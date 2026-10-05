using System.Numerics;

namespace SentinelHUD.Core;

/// <summary>
/// Plugin-owned outer-window minimum around Core's already-resolved shell geometry.
/// The practical logical minimum keeps six rail destinations usable at supported UI scales.
/// </summary>
public static class SentinelHudModernWindowPolicy
{
    public static readonly Vector2 LogicalMinimum = new(720f, 560f);

    public static Vector2 MinimumSize(float scale, Vector2 resolvedShellMinimum)
    {
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (!float.IsFinite(resolvedShellMinimum.X)
            || !float.IsFinite(resolvedShellMinimum.Y)
            || resolvedShellMinimum.X < 0f
            || resolvedShellMinimum.Y < 0f)
            throw new ArgumentOutOfRangeException(nameof(resolvedShellMinimum));

        var practical = LogicalMinimum * scale;
        var shellWithOuterChrome = resolvedShellMinimum + (new Vector2(24f, 20f) * scale);
        return Vector2.Max(practical, shellWithOuterChrome);
    }
}
