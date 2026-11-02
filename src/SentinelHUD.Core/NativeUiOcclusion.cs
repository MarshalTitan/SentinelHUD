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
    // These are persistent native HUD surfaces, not menus. Window metadata handles ordinary addons
    // without a fixed inventory/map whitelist; popups without a window component are explicit.
    public static bool IsWindow(string name, bool hasWindow, bool hasWindowCollision)
    {
        if (string.IsNullOrEmpty(name) || name[0] == '_' || name.StartsWith("ChatLog", StringComparison.Ordinal)) return false;
        if (name is "NamePlate" or "FlyText" or "ScreenLog" or "ScreenInfo" or "WideText"
            or "ActionBar" or "ActionCross" or "NowLoading" or "FadeMiddle" or "FadeBack") return false;
        return hasWindow || hasWindowCollision || name is "ContextMenu" or "ContextSub" or "ContextIconMenu"
            or "Tooltip" or "ItemDetail" or "ActionDetail" or "SelectString" or "SelectIconString" or "SelectYesno";
    }
}
