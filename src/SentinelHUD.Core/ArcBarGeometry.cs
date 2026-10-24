using System.Numerics;

namespace SentinelHUD.Core;

/// <summary>Cached, bounded vertical bows. Samples run from full-height bottom to top.</summary>
public sealed class ArcBarGeometry
{
    public const int Segments = 48;
    public const float MinimumHeight = 100f;
    public const float MaximumHeight = 600f;
    private readonly Vector2[] hp = new Vector2[Segments + 1];
    private readonly Vector2[] mp = new Vector2[Segments + 1];
    private (float Width, float Height, float Thickness, bool Right) key;
    public ReadOnlySpan<Vector2> Hp => hp;
    public ReadOnlySpan<Vector2> Mp => mp;
    public float Thickness { get; private set; }
    public float TextLeft { get; private set; }
    public float TextWidth { get; private set; }
    public int Revision { get; private set; }

    public static bool IsRight(ArcOrientation orientation, HudModuleKind kind)
        => orientation == ArcOrientation.Right || orientation == ArcOrientation.Auto && kind == HudModuleKind.Target;

    public void Update(float width, float height, float thickness, bool right)
    {
        width = float.IsFinite(width) ? Math.Max(80f, width) : 180f;
        height = float.IsFinite(height) ? Math.Max(40f, height) : 240f;
        thickness = float.IsFinite(thickness) ? Math.Clamp(thickness, 2f, width * 0.065f) : 8f;
        var next = (width, height, thickness, right);
        if (next == key) return;
        key = next;
        Thickness = thickness;
        var bow = width * 0.16f;
        var margin = thickness * 0.5f + 1f;
        var mpOffset = thickness + 5f;
        var curveWidth = margin + bow + mpOffset + thickness * 0.25f;
        TextWidth = width - curveWidth - 12f;
        TextLeft = right ? 0f : curveWidth + 12f;
        for (var i = 0; i <= Segments; i++)
        {
            var t = (float)i / Segments;
            var x = margin + bow * MathF.Pow(2f * t - 1f, 2f);
            var y = margin + (height - 2f * margin) * (1f - t);
            hp[i] = new Vector2(right ? width - x : x, y);
            mp[i] = new Vector2(right ? width - x - mpOffset : x + mpOffset, y);
        }
        Revision++;
    }

    public static EditorResizeResult Resize(float width, float height, float scale,
        EditorResizeEdges edges, Vector2 delta, Vector2 position, Vector2 size,
        Vector2 workPosition, Vector2 workSize)
    {
        scale = float.IsFinite(scale) ? Math.Clamp(scale, 0.1f, 4f) : 1f;
        var horizontal = LayoutPolicy.ResizeFromEdges(width, 20f, 1,
            edges & (EditorResizeEdges.Left | EditorResizeEdges.Right), delta,
            position, size, workPosition, workSize);
        var nextHeight = height;
        if (edges.HasFlag(EditorResizeEdges.Top)) nextHeight -= delta.Y / scale;
        else if (edges.HasFlag(EditorResizeEdges.Bottom)) nextHeight += delta.Y / scale;
        nextHeight = Math.Clamp(nextHeight, MinimumHeight, MaximumHeight);
        var change = (nextHeight - height) * scale;
        var nextPosition = horizontal.Position;
        if (edges.HasFlag(EditorResizeEdges.Top)) nextPosition.Y -= change;
        var nextSize = new Vector2(horizontal.Width, Math.Max(1f, size.Y + change));
        nextPosition = LayoutPolicy.KeepReachable(nextPosition, workPosition, workSize, nextSize);
        return new EditorResizeResult(horizontal.Width, nextHeight, nextPosition, nextSize,
            LayoutPolicy.ToNormalizedPosition(nextPosition, workPosition, workSize, nextSize));
    }
}

public static class CastPresentationPolicy
{
    public static bool ShowAttached(HudModuleConfiguration parent, CastModuleConfiguration separate)
        => parent.BarStyle == HudBarStyle.Standard && !separate.Enabled;

    public static bool ShowSeparate(CastModuleConfiguration cast, bool locked,
        bool isCasting, bool loggedIn, bool inCombat, bool inDuty)
        => cast.Enabled && (!locked || isCasting && HudVisibilityPolicy.ShouldShowModule(
            cast.Visibility, loggedIn, inCombat, inDuty));
}
