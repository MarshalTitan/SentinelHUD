namespace SentinelHUD.Core;

public enum CurrencyAlignment { Left, Right }

public sealed record CurrencyDefinition(uint ItemId, string Name, uint IconId, uint? Cap, bool IsTomestone);
public sealed record CurrencyBalance(CurrencyDefinition Definition, uint? Amount, uint? Cap);

public static class CurrencyPolicy
{
    public const int MaximumSelected = 24;
    public const int RefreshMilliseconds = 500;
    // Tomestones 1/2/3 are the active perennial, uncapped and weekly currencies.
    // Type 4 is exchange-only; type 0 contains retired currencies.
    public static bool IsAvailable(uint iconId, bool isTomestone, uint tomestoneType)
        => iconId != 26154 && (!isTomestone || tomestoneType is >= 1 and <= 3);

    public static bool MatchesSearch(string name, string search)
        => name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);
    public static bool IsCurrency(uint id, uint uiCategory, uint filterGroup, uint stackSize)
        => id > 0 && uiCategory == 63 && filterGroup == 16 && stackSize > 1;

    public static List<uint> NormalizeSelection(IEnumerable<uint>? items)
    {
        var result = new List<uint>();
        if (items is null) return [1];
        foreach (var id in items)
            if (id > 0 && !result.Contains(id))
            {
                result.Add(id);
                if (result.Count == MaximumSelected) break;
            }
        return result;
    }

    public static string Format(uint? amount, uint? cap, bool showCap, HudNumberFormat format)
    {
        if (amount is null) return "--";
        var current = HudFormatting.Number(amount.Value, format);
        return showCap && cap is > 1 ? current + " / " + HudFormatting.Number(cap.Value, format) : current;
    }

}

public static class ArcTextPolicy
{
    public static string HpCurrent(uint current, HudModuleConfiguration config, bool visible = true)
        => visible ? HudFormatting.Number(current, config.NumberFormat) : string.Empty;

    public static string HpMaximum(uint maximum, HudModuleConfiguration config, bool visible = true)
        => visible && config.ArcValueFormat == ArcValueFormat.CurrentAndTotal
            ? HudFormatting.Number(maximum, config.NumberFormat) : string.Empty;

    public static string Values(uint current, uint maximum, HudModuleConfiguration config,
        bool showCurrent = true, bool showMaximum = true)
        => HudFormatting.HitPoints(current, maximum, showCurrent,
            showMaximum && config.ArcValueFormat == ArcValueFormat.CurrentAndTotal, false, config.NumberFormat);

    public static string Percentage(uint current, uint maximum, HudModuleConfiguration config, bool visible = true)
        => visible && config.ArcShowPercentages ? HudFormatting.Percentage(current, maximum) : string.Empty;
}
