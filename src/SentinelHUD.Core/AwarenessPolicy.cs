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
    bool IsSupported,
    string DisplayName);

public static class NativeHighlightPolicy
{
    public static NativeHighlightSelection Resolve(SelfHighlightConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.ColourPreset switch
        {
            HighlightColourPreset.Yellow => new(NativeHighlightColour.Yellow, true, "Yellow"),
            HighlightColourPreset.Green => new(NativeHighlightColour.Green, true, "Green"),
            HighlightColourPreset.Blue => new(NativeHighlightColour.Blue, true, "Blue"),
            HighlightColourPreset.White => new(NativeHighlightColour.Yellow, false, "White"),
            HighlightColourPreset.Custom => new(NativeHighlightColour.Yellow, false, "Custom"),
            _ => new(NativeHighlightColour.Yellow, true, "Yellow"),
        };
    }
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
