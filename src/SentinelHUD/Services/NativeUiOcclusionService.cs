using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SentinelHUD.Core;
using NativeBounds = FFXIVClientStructs.FFXIV.Common.Math.Bounds;

namespace SentinelHUD.Services;

/// <summary>Read-only native window discovery. Only IDs/names survive a frame, never addon pointers.</summary>
public sealed unsafe class NativeUiOcclusionService : IDisposable
{
    private readonly IAddonLifecycle lifecycle;
    private readonly List<Candidate> candidates = new(64);
    private readonly List<NativeUiBlocker> windows = new(32);
    private int dirty = 1;
    private long nextDiscovery;
    private bool enabledBefore;
    public NativeUiOcclusionMask Mask { get; } = new();
    public IReadOnlyList<NativeUiBlocker> Windows => windows;
    public string State { get; private set; } = "Waiting for native UI";
    public int CandidateCount => candidates.Count;
    public int DiscoveryCount { get; private set; }

    public NativeUiOcclusionService(IAddonLifecycle lifecycle)
    {
        this.lifecycle = lifecycle;
        lifecycle.RegisterListener(AddonEvent.PostSetup, Invalidate);
        lifecycle.RegisterListener(AddonEvent.PreFinalize, Invalidate);
    }

    private void Invalidate(AddonEvent _, AddonArgs __) => Interlocked.Exchange(ref dirty, 1);

    public void Update(bool enabled, Vector2 viewportOrigin, Vector2 viewportSize)
    {
        Mask.Clear(); windows.Clear();
        if (!enabled) { State = "Off"; enabledBefore = false; return; }
        if (!enabledBefore) Interlocked.Exchange(ref dirty, 1);
        enabledBefore = true;
        try
        {
            var stage = AtkStage.Instance();
            if (stage is null || stage->RaptureAtkUnitManager is null)
            { candidates.Clear(); Interlocked.Exchange(ref dirty, 1); State = "Native UI unavailable"; return; }
            var manager = &stage->RaptureAtkUnitManager->AtkUnitManager;
            var now = Environment.TickCount64;
            if (Interlocked.Exchange(ref dirty, 0) != 0 || now >= nextDiscovery)
            {
                Discover(manager); nextDiscovery = now + 1000;
            }
            var viewport = ScreenRectangle.FromPositionSize(viewportOrigin, viewportSize);
            foreach (var candidate in candidates)
            {
                var addon = manager->GetAddonById(candidate.Id);
                if (addon is null || !addon->IsReady || !addon->IsVisible || addon->Alpha == 0
                    || addon->RootNode is null || !addon->RootNode->IsVisible()) continue;
                NativeBounds bounds = default;
                // The native window collision bounds account for UI scale/position. Root is used by
                // this game helper only for popups that do not provide a window collision node.
                addon->GetWindowBounds(&bounds);
                var r = new ScreenRectangle(viewportOrigin.X + bounds.Pos1.X, viewportOrigin.Y + bounds.Pos1.Y,
                    viewportOrigin.X + bounds.Pos2.X, viewportOrigin.Y + bounds.Pos2.Y);
                if (!NativeUiOcclusionMask.Valid(r) || !r.Intersects(viewport)) continue;
                r = new(Math.Max(r.Left, viewport.Left), Math.Max(r.Top, viewport.Top),
                    Math.Min(r.Right, viewport.Right), Math.Min(r.Bottom, viewport.Bottom));
                windows.Add(new(candidate.Name, r)); Mask.Add(r);
            }
            State = "Active; native window rectangles";
        }
        catch (Exception exception)
        {
            // Do not hide arbitrary gameplay regions if native metadata is temporarily unavailable.
            Mask.Clear(); windows.Clear(); candidates.Clear(); nextDiscovery = Environment.TickCount64 + 1000;
            State = $"Native UI unavailable ({exception.GetType().Name})";
        }
    }

    private void Discover(AtkUnitManager* manager)
    {
        candidates.Clear(); DiscoveryCount++;
        var list = &manager->AllLoadedUnitsList;
        for (var i = 0; i < Math.Min((int)list->Count, 256); i++)
        {
            var addon = list->Entries[i].Value;
            if (addon is null) continue;
            var name = addon->NameString;
            if (NativeUiWindowPolicy.IsWindow(name, addon->WindowNode is not null, addon->WindowCollisionNode is not null))
                candidates.Add(new(addon->Id, name));
        }
    }

    public void Dispose()
    {
        lifecycle.UnregisterListener(AddonEvent.PostSetup, Invalidate);
        lifecycle.UnregisterListener(AddonEvent.PreFinalize, Invalidate);
        candidates.Clear(); windows.Clear(); Mask.Clear();
    }
    private readonly record struct Candidate(ushort Id, string Name);
}

public readonly record struct NativeUiBlocker(string Name, ScreenRectangle Bounds);
