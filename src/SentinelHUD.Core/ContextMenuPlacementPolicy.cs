using System.Numerics;

namespace SentinelHUD.Core;

public enum ContextMenuPlacementSide
{
    Right,
    Left,
    Below,
    Above,
    MinimumOverlap,
}

public readonly record struct ScreenRectangle(float Left, float Top, float Right, float Bottom)
{
    public float Width => Math.Max(0f, Right - Left);
    public float Height => Math.Max(0f, Bottom - Top);

    public static ScreenRectangle FromPositionSize(Vector2 position, Vector2 size)
        => new(position.X, position.Y, position.X + size.X, position.Y + size.Y);

    public bool Intersects(ScreenRectangle other)
        => Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;

    public float IntersectionArea(ScreenRectangle other)
    {
        var width = Math.Max(0f, Math.Min(Right, other.Right) - Math.Max(Left, other.Left));
        var height = Math.Max(0f, Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top));
        return width * height;
    }
}

public readonly record struct ContextMenuPlacement(Vector2 Position, ContextMenuPlacementSide Side)
{
    public ScreenRectangle Bounds(Vector2 size) => ScreenRectangle.FromPositionSize(Position, size);
}

/// <summary>
/// Positions the native context menu beside its originating Sentinel region without moving that region.
/// </summary>
public static class ContextMenuPlacementPolicy
{
    public const float Gap = 8f;
    public const float ViewportMargin = 4f;
    public static readonly Vector2 EstimatedMenuSize = new(250f, 360f);

    public static ContextMenuPlacement Place(
        ScreenRectangle origin,
        Vector2 menuSize,
        ScreenRectangle viewport)
    {
        menuSize = new Vector2(
            Math.Clamp(IsFinite(menuSize.X) ? menuSize.X : EstimatedMenuSize.X, 1f, viewport.Width),
            Math.Clamp(IsFinite(menuSize.Y) ? menuSize.Y : EstimatedMenuSize.Y, 1f, viewport.Height));

        var usable = new ScreenRectangle(
            viewport.Left + ViewportMargin,
            viewport.Top + ViewportMargin,
            viewport.Right - ViewportMargin,
            viewport.Bottom - ViewportMargin);

        var right = new ContextMenuPlacement(
            new Vector2(origin.Right + Gap, Clamp(origin.Top, usable.Top, usable.Bottom - menuSize.Y)),
            ContextMenuPlacementSide.Right);
        var left = new ContextMenuPlacement(
            new Vector2(origin.Left - Gap - menuSize.X, Clamp(origin.Top, usable.Top, usable.Bottom - menuSize.Y)),
            ContextMenuPlacementSide.Left);
        var below = new ContextMenuPlacement(
            new Vector2(Clamp(origin.Left, usable.Left, usable.Right - menuSize.X), origin.Bottom + Gap),
            ContextMenuPlacementSide.Below);
        var above = new ContextMenuPlacement(
            new Vector2(Clamp(origin.Left, usable.Left, usable.Right - menuSize.X), origin.Top - Gap - menuSize.Y),
            ContextMenuPlacementSide.Above);

        Span<ContextMenuPlacement> preferred = [right, left, below, above];
        foreach (var placement in preferred)
        {
            var bounds = placement.Bounds(menuSize);
            if (IsInside(bounds, usable) && !bounds.Intersects(origin))
                return placement;
        }

        // Very small viewports may not have a fully clear side. Keep the popup on-screen and choose
        // the candidate obscuring the least of the originating region.
        var best = new ContextMenuPlacement(
            ClampToViewport(right.Position, menuSize, usable),
            ContextMenuPlacementSide.MinimumOverlap);
        var bestArea = best.Bounds(menuSize).IntersectionArea(origin);
        foreach (var candidate in preferred[1..])
        {
            var clamped = new ContextMenuPlacement(
                ClampToViewport(candidate.Position, menuSize, usable),
                ContextMenuPlacementSide.MinimumOverlap);
            var area = clamped.Bounds(menuSize).IntersectionArea(origin);
            if (area < bestArea)
            {
                best = clamped;
                bestArea = area;
            }
        }

        return best;
    }

    private static Vector2 ClampToViewport(Vector2 position, Vector2 size, ScreenRectangle viewport)
        => new(
            Clamp(position.X, viewport.Left, viewport.Right - size.X),
            Clamp(position.Y, viewport.Top, viewport.Bottom - size.Y));

    private static bool IsInside(ScreenRectangle rectangle, ScreenRectangle viewport)
        => rectangle.Left >= viewport.Left
           && rectangle.Top >= viewport.Top
           && rectangle.Right <= viewport.Right
           && rectangle.Bottom <= viewport.Bottom;

    private static float Clamp(float value, float minimum, float maximum)
        => maximum < minimum ? minimum : Math.Clamp(value, minimum, maximum);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
