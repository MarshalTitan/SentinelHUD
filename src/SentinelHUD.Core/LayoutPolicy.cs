using System.Numerics;

namespace SentinelHUD.Core;

public readonly record struct HorizontalResizeResult(
    float Width,
    Vector2 Position,
    ModuleLayoutConfiguration Layout);

[Flags]
public enum EditorResizeEdges
{
    None = 0,
    Left = 1,
    Right = 2,
    Top = 4,
    Bottom = 8,
}

public readonly record struct EditorResizeResult(
    float Width,
    float BarHeight,
    Vector2 Position,
    Vector2 ApproximateSize,
    ModuleLayoutConfiguration Layout);

public static class LayoutPolicy
{
    public static Vector2 ToOuterWindowPosition(Vector2 contentPosition, float editChromeHeight)
        => new(contentPosition.X, contentPosition.Y - SanitizeChromeHeight(editChromeHeight));

    public static Vector2 ToContentPosition(Vector2 outerWindowPosition, float editChromeHeight)
        => new(outerWindowPosition.X, outerWindowPosition.Y + SanitizeChromeHeight(editChromeHeight));

    public static Vector2 ToContentSize(Vector2 outerWindowSize, float editChromeHeight)
    {
        var chrome = SanitizeChromeHeight(editChromeHeight);
        return new Vector2(outerWindowSize.X, Math.Max(1f, outerWindowSize.Y - chrome));
    }

    public static Vector2 ToPixelPosition(
        ModuleLayoutConfiguration layout,
        Vector2 workPosition,
        Vector2 workSize,
        Vector2 windowSize)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!IsUsableWorkArea(workPosition, workSize))
            return Vector2.Zero;

        var safeWindow = SanitizeWindowSize(windowSize, workSize);
        var travel = Vector2.Max(workSize - safeWindow, Vector2.Zero);
        var anchor = new Vector2(
            ClampFinite(layout.AnchorX, 0f, 1f, 0f),
            ClampFinite(layout.AnchorY, 0f, 1f, 0f));
        return workPosition + (anchor * travel);
    }

    public static ModuleLayoutConfiguration ToNormalizedPosition(
        Vector2 pixelPosition,
        Vector2 workPosition,
        Vector2 workSize,
        Vector2 windowSize)
    {
        if (!IsUsableWorkArea(workPosition, workSize) || !IsFinite(pixelPosition))
            return new ModuleLayoutConfiguration();

        var safeWindow = SanitizeWindowSize(windowSize, workSize);
        var travel = Vector2.Max(workSize - safeWindow, Vector2.One);
        var relative = Vector2.Clamp(pixelPosition - workPosition, Vector2.Zero, travel);
        return new ModuleLayoutConfiguration
        {
            AnchorX = Math.Clamp(relative.X / travel.X, 0f, 1f),
            AnchorY = Math.Clamp(relative.Y / travel.Y, 0f, 1f),
        };
    }

    public static Vector2 KeepReachable(
        Vector2 pixelPosition,
        Vector2 workPosition,
        Vector2 workSize,
        Vector2 windowSize)
    {
        if (!IsUsableWorkArea(workPosition, workSize))
            return IsFinite(pixelPosition) ? pixelPosition : Vector2.Zero;

        var safeWindow = SanitizeWindowSize(windowSize, workSize);
        var maximum = workPosition + workSize - safeWindow;
        return Vector2.Clamp(IsFinite(pixelPosition) ? pixelPosition : workPosition, workPosition, maximum);
    }

    public static HorizontalResizeResult ResizeFromRightEdge(
        float startingWidth,
        float deltaX,
        Vector2 startingPosition,
        Vector2 startingSize,
        Vector2 workPosition,
        Vector2 workSize)
    {
        var safeStart = float.IsFinite(startingWidth) ? startingWidth : HudSizingPolicy.MinimumWidth;
        var safeDelta = float.IsFinite(deltaX) ? deltaX : 0f;
        var width = Math.Clamp(safeStart + safeDelta,
            HudSizingPolicy.MinimumWidth, HudSizingPolicy.MaximumWidth);
        var height = IsFinite(startingSize) && startingSize.Y > 0f ? startingSize.Y : 1f;
        var resizedSize = new Vector2(width, height);
        var position = KeepReachable(startingPosition, workPosition, workSize, resizedSize);
        var layout = ToNormalizedPosition(position, workPosition, workSize, resizedSize);
        return new HorizontalResizeResult(width, position, layout);
    }

    public static EditorResizeResult ResizeFromEdges(
        float startingWidth,
        float startingBarHeight,
        int renderedBarCount,
        EditorResizeEdges edges,
        Vector2 delta,
        Vector2 startingPosition,
        Vector2 startingSize,
        Vector2 workPosition,
        Vector2 workSize)
    {
        var safeStartingWidth = float.IsFinite(startingWidth)
            ? Math.Clamp(startingWidth, HudSizingPolicy.MinimumWidth, HudSizingPolicy.MaximumWidth)
            : HudSizingPolicy.MinimumWidth;
        var safeStartingBarHeight = float.IsFinite(startingBarHeight)
            ? Math.Clamp(startingBarHeight, HudSizingPolicy.MinimumBarHeight, HudSizingPolicy.MaximumBarHeight)
            : HudSizingPolicy.DefaultBarHeight;
        var width = safeStartingWidth;
        var barHeight = safeStartingBarHeight;
        var safeDelta = IsFinite(delta) ? delta : Vector2.Zero;
        var position = IsFinite(startingPosition) ? startingPosition : workPosition;
        var originalSize = IsFinite(startingSize) && startingSize.X > 0f && startingSize.Y > 0f
            ? startingSize
            : new Vector2(width, 1f);

        if (edges.HasFlag(EditorResizeEdges.Left))
        {
            var next = Math.Clamp(safeStartingWidth - safeDelta.X,
                HudSizingPolicy.MinimumWidth, HudSizingPolicy.MaximumWidth);
            position.X += safeStartingWidth - next;
            width = next;
        }
        else if (edges.HasFlag(EditorResizeEdges.Right))
        {
            width = Math.Clamp(safeStartingWidth + safeDelta.X,
                HudSizingPolicy.MinimumWidth, HudSizingPolicy.MaximumWidth);
        }

        var meterCount = Math.Max(1, renderedBarCount);
        if (edges.HasFlag(EditorResizeEdges.Top))
        {
            var next = Math.Clamp(safeStartingBarHeight - (safeDelta.Y / meterCount),
                HudSizingPolicy.MinimumBarHeight, HudSizingPolicy.MaximumBarHeight);
            position.Y += (safeStartingBarHeight - next) * meterCount;
            barHeight = next;
        }
        else if (edges.HasFlag(EditorResizeEdges.Bottom))
        {
            barHeight = Math.Clamp(safeStartingBarHeight + (safeDelta.Y / meterCount),
                HudSizingPolicy.MinimumBarHeight, HudSizingPolicy.MaximumBarHeight);
        }

        var heightDelta = (barHeight - safeStartingBarHeight) * meterCount;
        var approximateSize = new Vector2(width, Math.Max(1f, originalSize.Y + heightDelta));
        position = KeepReachable(position, workPosition, workSize, approximateSize);
        var layout = ToNormalizedPosition(position, workPosition, workSize, approximateSize);
        return new EditorResizeResult(width, barHeight, position, approximateSize, layout);
    }

    public static bool IsFinite(Vector2 value)
        => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static bool IsUsableWorkArea(Vector2 position, Vector2 size)
        => IsFinite(position) && IsFinite(size) && size.X > 0f && size.Y > 0f;

    private static Vector2 SanitizeWindowSize(Vector2 size, Vector2 workSize)
    {
        if (!IsFinite(size) || size.X <= 0f || size.Y <= 0f)
            size = new Vector2(240f, 110f);
        return Vector2.Min(size, workSize);
    }

    private static float ClampFinite(float value, float minimum, float maximum, float fallback)
        => float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;

    private static float SanitizeChromeHeight(float value)
        => float.IsFinite(value) ? Math.Max(0f, value) : 0f;
}
