using System.Numerics;

namespace SentinelHUD.Core;

public enum NativeHighlightColour
{
    Red,
    Green,
    Blue,
    Yellow,
    Orange,
    Magenta,
    Black,
}

public readonly record struct NativeHighlightSelection(
    NativeHighlightColour Colour,
    bool IsExact,
    string DisplayName);

public static class NativeHighlightPolicy
{
    private static readonly (NativeHighlightColour Colour, Vector3 Rgb, string Name)[] NativePalette =
    [
        (NativeHighlightColour.Red, new Vector3(1f, 0.20f, 0.16f), "Red"),
        (NativeHighlightColour.Green, new Vector3(0.24f, 1f, 0.38f), "Green"),
        (NativeHighlightColour.Blue, new Vector3(0.20f, 0.72f, 1f), "Blue"),
        (NativeHighlightColour.Yellow, new Vector3(1f, 0.82f, 0.18f), "Yellow"),
        (NativeHighlightColour.Orange, new Vector3(1f, 0.49f, 0.12f), "Orange"),
        (NativeHighlightColour.Magenta, new Vector3(1f, 0.24f, 0.78f), "Magenta"),
        (NativeHighlightColour.Black, new Vector3(0.05f, 0.05f, 0.05f), "Black"),
    ];

    public static NativeHighlightSelection Resolve(SelfHighlightConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.ColourPreset switch
        {
            HighlightColourPreset.Yellow => new(NativeHighlightColour.Yellow, true, "Yellow"),
            HighlightColourPreset.Green => new(NativeHighlightColour.Green, true, "Green"),
            HighlightColourPreset.Blue => new(NativeHighlightColour.Blue, true, "Blue"),
            HighlightColourPreset.White => new(NativeHighlightColour.Yellow, false, "Yellow"),
            HighlightColourPreset.Custom => Nearest(configuration.CustomColour.ToVector4().AsVector3()),
            _ => new(NativeHighlightColour.Yellow, true, "Yellow"),
        };
    }

    private static NativeHighlightSelection Nearest(Vector3 requested)
    {
        var best = NativePalette[0];
        var bestDistance = Vector3.DistanceSquared(requested, best.Rgb);
        for (var index = 1; index < NativePalette.Length; index++)
        {
            var candidate = NativePalette[index];
            var distance = Vector3.DistanceSquared(requested, candidate.Rgb);
            if (distance >= bestDistance)
                continue;
            best = candidate;
            bestDistance = distance;
        }

        return new NativeHighlightSelection(best.Colour, false, best.Name);
    }

    private static Vector3 AsVector3(this Vector4 colour) => new(colour.X, colour.Y, colour.Z);
}

public static class PlayerPositionMarkerPolicy
{
    public const float MinimumRadius = 0.01f;
    public const float MaximumRadius = 0.60f;
    public const float DefaultRadius = 0.18f;
    public const float DefaultOpacity = 0.88f;
    public const float MinimumBorderThickness = 0.5f;
    public const float MaximumBorderThickness = 2.5f;
    public const float DefaultBorderThickness = 1.25f;

    public static Vector4 ResolveColour(PlayerPositionMarkerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var colour = configuration.ColourPreset switch
        {
            HighlightColourPreset.Yellow => new Vector4(1f, 0.82f, 0.18f, 1f),
            HighlightColourPreset.Green => new Vector4(0.24f, 1f, 0.38f, 1f),
            HighlightColourPreset.Blue => new Vector4(0.20f, 0.72f, 1f, 1f),
            HighlightColourPreset.Custom => configuration.CustomColour.ToVector4(),
            _ => Vector4.One,
        };
        colour.W = Math.Clamp(configuration.Opacity, 0.1f, 1f);
        return colour;
    }
}

public static class HudVisibilityPolicy
{
    public static bool ShouldShowModule(
        ModuleVisibilityCondition condition,
        bool isLoggedIn,
        bool isInCombat,
        bool isInDuty)
        => isLoggedIn && condition switch
        {
            ModuleVisibilityCondition.Always => true,
            ModuleVisibilityCondition.CombatOnly => isInCombat,
            ModuleVisibilityCondition.DutyOnly => isInDuty,
            ModuleVisibilityCondition.CombatOrDuty => isInCombat || isInDuty,
            _ => false,
        };

    public static bool ShouldShowNativeTargetOverlay(
        NativeTargetOverlayMode mode,
        bool isLoggedIn,
        bool isInCombat)
        => isLoggedIn && mode switch
        {
            NativeTargetOverlayMode.Always => true,
            NativeTargetOverlayMode.CombatOnly => isInCombat,
            _ => false,
        };
}

public static class CameraZoomPolicy
{
    public const float StockMaximum = 20f;
    public const float DefaultExtendedMaximum = 30f;
    public const float MaximumSupported = 100f;

    public static float NormalizeMaximum(float value)
        => float.IsFinite(value)
            ? Math.Clamp(value, StockMaximum, MaximumSupported)
            : DefaultExtendedMaximum;
}
