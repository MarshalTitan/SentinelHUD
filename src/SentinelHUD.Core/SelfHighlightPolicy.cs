using System.Numerics;

namespace SentinelHUD.Core;

public static class SelfHighlightPolicy
{
    public static bool ShouldRender(
        SelfHighlightMode mode,
        bool isLoggedIn,
        bool isInCombat,
        bool isInDuty)
        => isLoggedIn && mode switch
        {
            SelfHighlightMode.Always => true,
            SelfHighlightMode.CombatOnly => isInCombat,
            SelfHighlightMode.DutyOnly => isInDuty,
            _ => false,
        };

    public static Vector4 ResolveColour(SelfHighlightConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var colour = configuration.ColourPreset switch
        {
            HighlightColourPreset.Green => new Vector4(0.24f, 1f, 0.38f, 1f),
            HighlightColourPreset.Blue => new Vector4(0.20f, 0.72f, 1f, 1f),
            HighlightColourPreset.White => Vector4.One,
            HighlightColourPreset.Custom => configuration.CustomColour?.ToVector4() ?? Vector4.One,
            _ => new Vector4(1f, 0.82f, 0.18f, 1f),
        };
        colour.W = Math.Clamp(configuration.Intensity, 0.15f, 1f);
        return colour;
    }
}
