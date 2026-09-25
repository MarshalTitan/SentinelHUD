using System.Numerics;

namespace SentinelHUD.Core;

public static class HpColourPolicy
{
    public static readonly Vector4 HighHealth = new(0.20f, 0.78f, 0.34f, 1f);
    public static readonly Vector4 MidHealth = new(1.00f, 0.82f, 0.12f, 1f);
    public static readonly Vector4 LowHealth = new(0.90f, 0.18f, 0.16f, 1f);

    public const float LowThreshold = 0.35f;
    public const float MidThreshold = 0.55f;
    public const float HighThreshold = 0.80f;

    public static Vector4 Resolve(HpColourMode mode, float healthFraction, Vector4 staticColour)
    {
        if (mode == HpColourMode.StaticRoleBased)
            return staticColour;

        var fraction = float.IsFinite(healthFraction) ? Math.Clamp(healthFraction, 0f, 1f) : 0f;
        if (fraction <= LowThreshold)
            return LowHealth;
        if (fraction < MidThreshold)
            return Vector4.Lerp(LowHealth, MidHealth,
                (fraction - LowThreshold) / (MidThreshold - LowThreshold));
        if (fraction < HighThreshold)
            return Vector4.Lerp(MidHealth, HighHealth,
                (fraction - MidThreshold) / (HighThreshold - MidThreshold));
        return HighHealth;
    }
}
