namespace SentinelHUD.Core;

public readonly record struct ShieldBarLayout(
    float HealthEnd,
    float ShieldOverlayStart,
    float ShieldExtensionEnd)
{
    public bool HasOverlay => ShieldOverlayStart < HealthEnd;
    public bool HasExtension => ShieldExtensionEnd > HealthEnd;
}

public static class ShieldBarPolicy
{
    public static ShieldBarLayout Calculate(float healthFraction, float shieldFraction)
    {
        var health = ClampFraction(healthFraction);
        var shield = ClampFraction(shieldFraction);
        var extension = Math.Min(shield, 1f - health);
        var overflow = Math.Max(0f, shield - extension);
        return new ShieldBarLayout(
            health,
            Math.Max(0f, health - overflow),
            Math.Min(1f, health + extension));
    }

    private static float ClampFraction(float value)
        => float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 0f;
}
