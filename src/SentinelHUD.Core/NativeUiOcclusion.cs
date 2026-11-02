using System.Numerics;

namespace SentinelHUD.Core;

/// <summary>A union of native window rectangles. Never infers occlusion from world objects.</summary>
public sealed class NativeUiOcclusionMask
{
    private readonly List<ScreenRectangle> blockers = new(32);
    public IReadOnlyList<ScreenRectangle> Blockers => blockers;
    public void Clear() => blockers.Clear();

    public void Add(ScreenRectangle rectangle)
    {
        if (!Valid(rectangle)) return;
        foreach (var old in blockers) if (Contains(old, rectangle)) return;
        for (var i = blockers.Count - 1; i >= 0; i--)
            if (Contains(rectangle, blockers[i])) blockers.RemoveAt(i);
        blockers.Add(rectangle);
    }

    public bool Contains(Vector2 point)
    {
        foreach (var r in blockers)
            if (point.X >= r.Left && point.X < r.Right && point.Y >= r.Top && point.Y < r.Bottom) return true;
        return false;
    }

    public bool Intersects(ScreenRectangle rectangle)
    {
        foreach (var r in blockers) if (r.Intersects(rectangle)) return true;
        return false;
    }

    /// <summary>Disjoint visible rectangles, shared by graphics scissors and input windows.</summary>
    public void Clip(ScreenRectangle source, List<ScreenRectangle> destination, List<ScreenRectangle> scratch)
    {
        destination.Clear(); scratch.Clear();
        if (!Valid(source)) return;
        destination.Add(source);
        var current = destination; var next = scratch;
        foreach (var blocker in blockers)
        {
            next.Clear();
            foreach (var rectangle in current)
            {
                if (!rectangle.Intersects(blocker)) { next.Add(rectangle); continue; }
                var left = Math.Max(rectangle.Left, blocker.Left);
                var top = Math.Max(rectangle.Top, blocker.Top);
                var right = Math.Min(rectangle.Right, blocker.Right);
                var bottom = Math.Min(rectangle.Bottom, blocker.Bottom);
                AddIfArea(next, new(rectangle.Left, rectangle.Top, rectangle.Right, top));
                AddIfArea(next, new(rectangle.Left, bottom, rectangle.Right, rectangle.Bottom));
                AddIfArea(next, new(rectangle.Left, top, left, bottom));
                AddIfArea(next, new(right, top, rectangle.Right, bottom));
            }
            (current, next) = (next, current);
            if (current.Count == 0) break;
        }
        if (!ReferenceEquals(current, destination)) { destination.Clear(); destination.AddRange(current); }
        scratch.Clear();
    }

    public static bool Valid(ScreenRectangle r) => float.IsFinite(r.Left) && float.IsFinite(r.Top)
        && float.IsFinite(r.Right) && float.IsFinite(r.Bottom) && r.Right > r.Left && r.Bottom > r.Top;
    private static bool Contains(ScreenRectangle a, ScreenRectangle b)
        => a.Left <= b.Left && a.Top <= b.Top && a.Right >= b.Right && a.Bottom >= b.Bottom;
    private static void AddIfArea(List<ScreenRectangle> target, ScreenRectangle r)
    { if (r.Right > r.Left && r.Bottom > r.Top) target.Add(r); }
}

public static class NativeUiWindowPolicy
{
    // A collision node is not proof of a painted window: invisible helpers can own a viewport-sized
    // collision/root canvas. Generic addons require a verified visible Window component. Frameless
    // panels/popups use explicit names and bounded fallback rectangles only.
    public static bool IsWindow(string name, bool hasWindow, bool hasWindowCollision)
        => !Excluded(name) && (hasWindow || IsPopup(name) || hasWindowCollision && IsPanel(name));

    public static bool Excluded(string name)
    {
        return string.IsNullOrEmpty(name) || name[0] == '_'
            || name.StartsWith("ChatLog", StringComparison.Ordinal)
            || name.StartsWith("Fade", StringComparison.Ordinal)
            || name.StartsWith("ScreenInfo", StringComparison.Ordinal)
            || name.StartsWith("ScreenLog", StringComparison.Ordinal)
            || name is "NamePlate" or "FlyText" or "WideText" or "AreaText" or "ScreenFilter"
                or "ActionBar" or "ActionCross" or "NowLoading";
    }

    public static bool IsPopup(string name) => name is "ContextMenu" or "ContextSub" or "ContextIconMenu"
        or "Tooltip" or "ItemDetail" or "ActionDetail" or "SelectString" or "SelectIconString" or "SelectYesno";

    public static bool IsPanel(string name) => name is "Inventory" or "InventoryLarge" or "InventoryExpansion"
        or "InventoryBuddy" or "InventoryBuddy2" or "InventoryRetainer" or "InventoryRetainerLarge"
        or "Character" or "CharacterStatus" or "CharacterProfile" or "CharacterInspect" or "ArmouryBoard"
        or "Achievement" or "AreaMap" or "Map" or "ContentsFinder" or "ContentsFinderConfirm"
        or "ContentsFinderReady" or "Currency" or "Journal" or "JournalDetail" or "QuestResult";

    public static bool TryBounds(NativeUiWindowEvidence evidence, ScreenRectangle viewport,
        out ScreenRectangle bounds, out NativeUiBoundsSource source, out NativeUiBoundsRejection rejection)
    {
        bounds = default; source = NativeUiBoundsSource.None; rejection = NativeUiBoundsRejection.NoWindowEvidence;
        if (Excluded(evidence.Name)) { rejection = NativeUiBoundsRejection.HudOrScreenHelper; return false; }
        ScreenRectangle? preferred = null;
        var frameCanvas = evidence.FrameBounds is { } candidateFrame && NativeUiOcclusionMask.Valid(candidateFrame)
            && IsCanvas(candidateFrame, viewport);
        if (evidence.VerifiedWindowComponent && evidence.FrameBounds is { } frame
            && NativeUiOcclusionMask.Valid(frame) && !frameCanvas)
        { preferred = frame; source = NativeUiBoundsSource.WindowFrame; }
        else if (evidence.VerifiedWindowComponent || IsPanel(evidence.Name) || IsPopup(evidence.Name))
        {
            var canvas = frameCanvas;
            if (evidence.CollisionBounds is { } collision && NativeUiOcclusionMask.Valid(collision))
            {
                if (IsCanvas(collision, viewport)) canvas = true;
                else { preferred = collision; source = NativeUiBoundsSource.WindowCollision; }
            }
            if (preferred is null && (IsPanel(evidence.Name) || IsPopup(evidence.Name))
                && evidence.RootBounds is { } root && NativeUiOcclusionMask.Valid(root))
            {
                if (IsCanvas(root, viewport)) canvas = true;
                else { preferred = root; source = NativeUiBoundsSource.NamedPanelOrPopupRoot; }
            }
            if (preferred is null && canvas)
            {
                // AreaMap has a genuine full-screen presentation. A generic window/helper canvas
                // is insufficient evidence for hiding the entire gameplay HUD.
                if (evidence.VerifiedWindowComponent && frameCanvas && (evidence.Name is "AreaMap" or "Map"))
                { preferred = evidence.FrameBounds; source = NativeUiBoundsSource.WindowFrame; }
                else { rejection = NativeUiBoundsRejection.UnsafeCanvasFallback; return false; }
            }
        }
        else return false;
        if (preferred is not { } selected) { rejection = NativeUiBoundsRejection.NoUsableWindowBounds; return false; }
        if (!selected.Intersects(viewport)) { rejection = NativeUiBoundsRejection.OutsideViewport; source = NativeUiBoundsSource.None; return false; }
        bounds = new(Math.Max(selected.Left, viewport.Left), Math.Max(selected.Top, viewport.Top),
            Math.Min(selected.Right, viewport.Right), Math.Min(selected.Bottom, viewport.Bottom));
        rejection = NativeUiBoundsRejection.None; return true;
    }

    private static bool IsCanvas(ScreenRectangle rectangle, ScreenRectangle viewport)
        => rectangle.Width >= viewport.Width * 0.95f && rectangle.Height >= viewport.Height * 0.95f;
}

public enum NativeUiBoundsSource { None, WindowFrame, WindowCollision, NamedPanelOrPopupRoot }
public enum NativeUiBoundsRejection { None, HudOrScreenHelper, NoWindowEvidence, NoUsableWindowBounds, UnsafeCanvasFallback, OutsideViewport }
public readonly record struct NativeUiWindowEvidence(string Name, bool VerifiedWindowComponent,
    ScreenRectangle? FrameBounds, ScreenRectangle? CollisionBounds, ScreenRectangle? RootBounds);
