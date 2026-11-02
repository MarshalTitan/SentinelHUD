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
    private readonly List<NativeUiRejectedWindow> rejected = new(32);
    private int dirty = 1;
    private long nextDiscovery;
    private bool enabledBefore;
    public NativeUiOcclusionMask Mask { get; } = new();
    public IReadOnlyList<NativeUiBlocker> Windows => windows;
    public IReadOnlyList<NativeUiRejectedWindow> Rejected => rejected;
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
        Mask.Clear(); windows.Clear(); rejected.Clear();
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
                if (NativeUiWindowPolicy.Excluded(candidate.Name))
                { rejected.Add(new(candidate.Name, NativeUiBoundsRejection.HudOrScreenHelper)); continue; }
                var frame = addon->WindowNode;
                var verified = frame is not null && VisibleThroughParents((AtkResNode*)frame)
                    && frame->Component is not null && frame->Component->GetComponentType() == ComponentType.Window;
                var namedFallback = NativeUiWindowPolicy.IsPanel(candidate.Name) || NativeUiWindowPolicy.IsPopup(candidate.Name);
                if (!verified && !namedFallback)
                { rejected.Add(new(candidate.Name, NativeUiBoundsRejection.NoWindowEvidence)); continue; }
                var collision = verified ? ((AtkComponentWindow*)frame->Component)->WindowCollisionNode : null;
                if (collision is null) collision = addon->WindowCollisionNode;
                var evidence = new NativeUiWindowEvidence(candidate.Name, verified,
                    verified ? ReadBounds((AtkResNode*)frame, viewportOrigin) : null,
                    ReadBounds((AtkResNode*)collision, viewportOrigin),
                    namedFallback ? ReadBounds(addon->RootNode, viewportOrigin) : null);
                if (!NativeUiWindowPolicy.TryBounds(evidence, viewport, out var r, out var source, out var reason))
                { rejected.Add(new(candidate.Name, reason)); continue; }
                windows.Add(new(candidate.Name, r, source)); Mask.Add(r);
            }
            State = "Active; verified windows and bounded panel/pop-up fallbacks";
        }
        catch (Exception exception)
        {
            // Do not hide arbitrary gameplay regions if native metadata is temporarily unavailable.
            Mask.Clear(); windows.Clear(); rejected.Clear(); candidates.Clear(); nextDiscovery = Environment.TickCount64 + 1000;
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
            // Keep possible helpers in diagnostics, but never admit their generic collision/root
            // rectangles to the mask without positive window evidence in TryBounds.
            if (addon->WindowNode is not null || addon->WindowCollisionNode is not null
                || NativeUiWindowPolicy.IsPanel(name) || NativeUiWindowPolicy.IsPopup(name))
                candidates.Add(new(addon->Id, name));
        }
    }

    public void Dispose()
    {
        lifecycle.UnregisterListener(AddonEvent.PostSetup, Invalidate);
        lifecycle.UnregisterListener(AddonEvent.PreFinalize, Invalidate);
        candidates.Clear(); windows.Clear(); rejected.Clear(); Mask.Clear();
    }

    private static ScreenRectangle? ReadBounds(AtkResNode* node, Vector2 origin)
    {
        if (node is null || node->Width == 0 || node->Height == 0) return null;
        NativeBounds bounds = default; node->GetBounds(&bounds);
        var rectangle = new ScreenRectangle(origin.X + bounds.Pos1.X, origin.Y + bounds.Pos1.Y,
            origin.X + bounds.Pos2.X, origin.Y + bounds.Pos2.Y);
        return NativeUiOcclusionMask.Valid(rectangle) ? rectangle : null;
    }

    private static bool VisibleThroughParents(AtkResNode* node)
    {
        for (var depth = 0; node is not null && depth < 64; depth++, node = node->ParentNode)
            if (!node->IsVisible()) return false;
        return node is null;
    }
    private readonly record struct Candidate(ushort Id, string Name);
}

public readonly record struct NativeUiBlocker(string Name, ScreenRectangle Bounds, NativeUiBoundsSource Source);
public readonly record struct NativeUiRejectedWindow(string Name, NativeUiBoundsRejection Reason);
