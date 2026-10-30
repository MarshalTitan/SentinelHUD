using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SentinelHUD.Core;
using GameAction = Lumina.Excel.Sheets.Action;
using SdkVfxData = FFXIVClientStructs.FFXIV.Client.Graphics.Vfx.VfxData;
using SdkVfxResource = FFXIVClientStructs.FFXIV.Client.Graphics.Vfx.VfxResourceInstance;
using static SentinelHUD.Services.NativeOmenMemory;

namespace SentinelHUD.Services;

/// <summary>
/// Original, API-15-specific omen adapter. Only native factory results are considered; the matching
/// VFX-data destruction hook removes leases before native memory is released. No renderer is added.
/// Native ABI descriptors are interoperability facts, documented in ENCOUNTER_VISUALS.md.
/// </summary>
public sealed unsafe class NativeOmenColourService : IDisposable
{
    private const string OmenFactorySignature = "E8 ?? ?? ?? ?? 48 89 84 FB ?? ?? ?? ?? 48 85 C0 74 53";
    private const string VfxDataDestroySignature = "E8 ?? ?? ?? ?? 4D 89 A4 DE ?? ?? ?? ??";
    private readonly IGameInteropProvider interop;
    private readonly IDalamudPluginInterface plugins;
    private readonly IObjectTable objects;
    private readonly IDataManager data;
    private readonly object gate = new();
    private readonly OmenLifetimeRegistry<TrackedOmen> tracked = new(512);
    private readonly List<nint> retired = new(512);
    private readonly HashSet<nint> destroyedDuringFactory = new();
    private readonly Dictionary<uint, uint> omenIds = new();
    private Hook<CreateNativeOmen>? createHook;
    private Hook<DestroyVfxData>? destroyHook;
    private NativeOmenStyle style;
    private OmenCastTiming[] casts = [];
    private long nextCastPoll;
    private long nextConflictPoll;
    private long nextGeneration;
    private int activeFactories;
    private bool conflict;
    private bool requestedBefore;
    private bool failed;
    private bool disposed;

    private delegate nint CreateNativeOmen(uint omenId, nint p2, nint p3, float p4,
        int p5, int p6, float p7, int p8, byte hostility, byte p10);
    private delegate void DestroyVfxData(nint dataHandle);

    public NativeOmenColourService(IGameInteropProvider interop, IDalamudPluginInterface plugins,
        IObjectTable objects, IDataManager data)
    { this.interop = interop; this.plugins = plugins; this.objects = objects; this.data = data; }

    public string State { get; private set; } = "Off";
    public int TrackedCount { get { lock (gate) return tracked.Count; } }

    public void Update(NativeOmenColourConfiguration config, EncounterPaletteConfiguration palette,
        bool active, bool territoryChanged)
    {
        if (disposed) return;
        var requested = active && config.Enabled;
        if (requested && !requestedBefore) failed = false;
        requestedBefore = requested;
        var now = Environment.TickCount64;
        if (now >= nextConflictPoll)
        {
            conflict = plugins.InstalledPlugins.Any(p => p.IsLoaded
                && p.InternalName.Equals("Chroma", StringComparison.OrdinalIgnoreCase));
            nextConflictPoll = now + 2000;
        }
        if (!requested || conflict || territoryChanged)
        {
            Stop();
            State = !requested ? config.Enabled ? "Paused; HUD disabled or character unavailable" : "Off"
                : conflict ? "Paused; Chroma is loaded and may own native omen colours" : "Waiting for the new territory";
            return;
        }
        if (failed)
        { Stop(); State = "Not Available: native omen processing stopped; toggle off/on to retry"; return; }
        try
        {
            EnsureHooks();
            var timings = casts;
            if (config.CastWarningTransition && now >= nextCastPoll)
            { timings = ReadCastTimings(now); nextCastPoll = now + 100; }
            if (!config.CastWarningTransition) timings = [];
            lock (gate)
            {
                casts = timings;
                var nextStyle = NativeOmenStyle.From(config, palette, true);
                var colourChanged = nextStyle != style;
                style = nextStyle;
                foreach (var (handle, entry) in tracked.Active)
                {
                    // Static tints are applied at creation/config changes; only warning casts animate.
                    if (!colourChanged && (!style.Transition || !entry.Hostile)) continue;
                    if (!TryGetInstance(handle, out var instance) || instance != entry.Instance
                        || !entry.Lease.Owns(Read(instance))) { retired.Add(handle); continue; }
                    if (!entry.Hostile && !style.FriendlyEnabled)
                    { Write(instance, entry.Lease.Original); retired.Add(handle); continue; }
                    Apply(entry);
                }
                foreach (var handle in retired) tracked.Forget(handle);
                retired.Clear();
            }
            // Destruction is observed before creation can begin registering leases.
            if (!destroyHook!.IsEnabled) destroyHook.Enable();
            if (!createHook!.IsEnabled) createHook.Enable();
            State = TrackedCount == 0 ? "Ready; waiting for new native omens" : $"Active: {TrackedCount} native omen(s)";
        }
        catch (Exception exception)
        {
            failed = true; Stop();
            State = $"Not Available: native omen interop could not initialize ({exception.GetType().Name}). Toggle off/on to retry.";
        }
    }

    private void EnsureHooks()
    {
        if (createHook is not null && destroyHook is not null) return;
        if (sizeof(SdkVfxData) != 0x1E0 || sizeof(SdkVfxResource) < ColourOffset + sizeof(Vector4))
            throw new NotSupportedException("SDK VFX layout differs from the reviewed ABI");
        destroyHook ??= interop.HookFromSignature<DestroyVfxData>(VfxDataDestroySignature, OnDestroy);
        createHook ??= interop.HookFromSignature<CreateNativeOmen>(OmenFactorySignature, OnCreate);
    }

    private nint OnCreate(uint id, nint p2, nint p3, float p4, int p5, int p6, float p7,
        int p8, byte hostility, byte p10)
    {
        var observing = false;
        try
        {
            lock (gate)
            {
                observing = !disposed && style.CanRecolour(hostility);
                if (observing && activeFactories++ == 0) destroyedDuringFactory.Clear();
            }
        }
        catch { observing = false; }
        // Always invoke the game's factory exactly once, including unavailable/disabled paths.
        nint result;
        try { result = createHook!.Original(id, p2, p3, p4, p5, p6, p7, p8, hostility, p10); }
        catch
        {
            if (observing) lock (gate) if (--activeFactories == 0) destroyedDuringFactory.Clear();
            throw;
        }
        if (!observing) return result;
        try
        {
            lock (gate)
            {
                try
                {
                    // Concurrent destruction during the factory invalidates its result conservatively.
                    if (disposed || !style.Enabled || destroyedDuringFactory.Contains(result)
                        || !TryGetInstance(result, out var instance)) return result;
                    var current = Read(instance);
                    var previous = tracked.Find(result);
                    var original = previous is not null && previous.Instance == instance && previous.Lease.Owns(current)
                        ? previous.Lease.Original : current;
                    var generation = ++nextGeneration;
                    if (!tracked.RegisterPending(generation, result)) return result;
                    if (!SaneColour(original)) { tracked.Forget(result); return result; }
                    var entry = new TrackedOmen(instance, hostility == 1, id, original,
                        hostility == 1 ? OmenCastTiming.Unique(casts, id) : null);
                    if (tracked.Activate(generation, result, entry)) Apply(entry);
                }
                finally { if (--activeFactories == 0) destroyedDuringFactory.Clear(); }
            }
        }
        catch { FailCallbacks(); }
        return result;
    }

    private void OnDestroy(nint handle)
    {
        try
        {
            lock (gate)
            {
                var entry = tracked.Find(handle);
                tracked.Forget(handle); // Forget before any optional restoration can fail.
                if (activeFactories > 0)
                {
                    if (destroyedDuringFactory.Count < 4096) destroyedDuringFactory.Add(handle);
                    else { style = style with { Enabled = false }; failed = true; }
                }
                if (entry is not null && TryGetInstance(handle, out var instance)
                    && instance == entry.Instance && entry.Lease.Owns(Read(instance))) Write(instance, entry.Lease.Original);
            }
        }
        catch { /* Destruction must never be prevented by managed bookkeeping. */ }
        destroyHook!.Original(handle);
    }

    private void Apply(TrackedOmen entry)
    {
        if (entry.Timing is null && style.Transition) entry.Timing = OmenCastTiming.Unique(casts, entry.OmenId);
        float? progress = null;
        if (entry.Timing is { } timing)
            foreach (var current in casts)
                if (current.ActorId == timing.ActorId && current.ActionId == timing.ActionId
                    && Math.Abs(current.StartTick - timing.StartTick) < 300)
                { progress = current.Progress(Environment.TickCount64); break; }
        Write(entry.Instance, entry.Lease.Apply(style.Resolve(entry.Hostile, progress, entry.Lease.Original.W)));
    }

    private OmenCastTiming[] ReadCastTimings(long now)
    {
        var result = new List<OmenCastTiming>(32);
        foreach (var candidate in objects)
        {
            if (result.Count >= 128) break;
            if (candidate is not IBattleChara actor || candidate is not ICharacter character
                || !character.StatusFlags.HasFlag(StatusFlags.Hostile) || !actor.IsCasting) continue;
            if (!float.IsFinite(actor.TotalCastTime) || actor.TotalCastTime is <= 0f or > 600f
                || !float.IsFinite(actor.CurrentCastTime) || actor.CurrentCastTime < 0f || actor.CurrentCastTime > actor.TotalCastTime + 1f) continue;
            var actionId = actor.CastActionId;
            if (!omenIds.TryGetValue(actionId, out var omenId))
            {
                omenId = data.GetExcelSheet<GameAction>().TryGetRow(actionId, out var action) ? action.Omen.RowId : 0;
                omenIds[actionId] = omenId;
            }
            if (omenId != 0) result.Add(new OmenCastTiming(omenId, actor.GameObjectId, actionId,
                now - (long)(actor.CurrentCastTime * 1000f), actor.TotalCastTime));
        }
        return result.ToArray();
    }

    private void Stop()
    {
        bool destructionIdle;
        lock (gate)
        {
            style = style with { Enabled = false }; casts = [];
            foreach (var (handle, entry) in tracked.Active)
                if (TryGetInstance(handle, out var instance) && instance == entry.Instance && entry.Lease.Owns(Read(instance)))
                    Write(instance, entry.Lease.Original);
            tracked.Clear(); retired.Clear();
            destructionIdle = activeFactories == 0;
        }
        // Retain the destruction observer only while an observed factory is still unwinding.
        try { if (createHook is { IsEnabled: true }) createHook.Disable(); } catch { }
        try { if (destructionIdle && destroyHook is { IsEnabled: true }) destroyHook.Disable(); } catch { }
    }

    private void FailCallbacks()
    {
        lock (gate) style = style with { Enabled = false };
        failed = true; State = "Not Available: native omen callback stopped; toggle off/on to retry";
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { Stop(); }
        finally
        {
            try { createHook?.Dispose(); }
            finally { destroyHook?.Dispose(); }
        }
    }

    private static bool SaneColour(Vector4 value) => float.IsFinite(value.X) && float.IsFinite(value.Y)
        && float.IsFinite(value.Z) && float.IsFinite(value.W) && value.X is >= 0f and <= 64f
        && value.Y is >= 0f and <= 64f && value.Z is >= 0f and <= 64f && value.W is >= 0f and <= 64f
        && value.X + value.Y + value.Z > 0.0001f;

    private sealed class TrackedOmen(nint instance, bool hostile, uint omenId, Vector4 original, OmenCastTiming? timing)
    {
        public nint Instance { get; } = instance;
        public bool Hostile { get; } = hostile;
        public uint OmenId { get; } = omenId;
        public OmenTintLease Lease { get; } = new(original);
        public OmenCastTiming? Timing = timing;
    }
}
