using System.Globalization;

namespace SentinelHUD.Core;

public static class HudFormatting
{
    public static string HitPoints(
        uint current,
        uint maximum,
        bool showCurrent,
        bool showMaximum,
        bool showPercentage,
        HudNumberFormat numberFormat = HudNumberFormat.Full)
    {
        var numberPart = (showCurrent, showMaximum) switch
        {
            (true, true) => $"{Number(current, numberFormat)} / {Number(maximum, numberFormat)}",
            (true, false) => Number(current, numberFormat),
            (false, true) => $"Max {Number(maximum, numberFormat)}",
            _ => string.Empty,
        };

        if (!showPercentage)
            return numberPart;

        var percentage = Percentage(current, maximum);
        return numberPart.Length == 0 ? percentage : $"{numberPart} — {percentage}";
    }

    public static string NativeTargetHitPoints(
        uint current,
        uint maximum,
        NativeTargetHpFormat format,
        HudNumberFormat numberFormat)
        => format switch
        {
            NativeTargetHpFormat.CurrentAndMaximum => HitPoints(current, maximum, true, true, false, numberFormat),
            NativeTargetHpFormat.Percentage => Percentage(current, maximum),
            _ => HitPoints(current, maximum, true, true, true, numberFormat),
        };

    public static string Number(uint value, HudNumberFormat format)
    {
        if (format == HudNumberFormat.Full)
            return value.ToString("N0", CultureInfo.InvariantCulture);
        if (value >= 1_000_000)
            return string.Create(CultureInfo.InvariantCulture, $"{value / 1_000_000d:0.##}m");
        if (value >= 1_000)
            return string.Create(CultureInfo.InvariantCulture, $"{value / 1_000d:0.#}k");
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string Percentage(uint current, uint maximum)
        => maximum == 0
            ? "--"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{Math.Clamp((double)current / maximum * 100d, 0d, 999.9d):0.0}%");

    public static string CastPercentage(float current, float total)
        => total <= 0f || !float.IsFinite(current) || !float.IsFinite(total)
            ? "--"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{Math.Clamp(current / total * 100f, 0f, 100f):0.0}%");

    public static string Distance(float yalms)
        => !float.IsFinite(yalms)
            ? "--"
            : string.Create(CultureInfo.InvariantCulture, $"{Math.Max(0f, yalms):0.0} yalms");

    public static string Shield(uint maximumHp, byte shieldPercentage)
    {
        var amount = (ulong)Math.Round(maximumHp * (shieldPercentage / 100d), MidpointRounding.AwayFromZero);
        return string.Create(CultureInfo.InvariantCulture, $"{amount:N0} ({shieldPercentage}%)");
    }
}
