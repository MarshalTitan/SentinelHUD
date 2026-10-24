using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using SentinelHUD.Core;

namespace SentinelHUD.UI;

public sealed class CurrencyTrackerRenderer(ITextureProvider? textures)
{
    private readonly Dictionary<uint, ISharedImmediateTexture> icons = new();
    public int RowCount { get; private set; }
    public int EntryCount { get; private set; }

    public void Draw(CurrencyTrackerConfiguration config, IReadOnlyList<CurrencyBalance> balances,
        float scale, float opacity, bool editing)
    {
        RowCount = 1; EntryCount = 0;
        var width = ImGui.GetContentRegionAvail().X;
        var iconSize = Math.Max(ImGui.GetTextLineHeight(), config.BarHeight * scale);
        var startX = ImGui.GetCursorPosX();
        var used = 0f;
        var gap = 16f * scale;
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
        try
        {
            foreach (var balance in balances)
            {
                var value = CurrencyPolicy.Format(balance.Amount, balance.Definition.ItemId == 1 ? null : balance.Cap,
                    config.ShowCaps, config.NumberFormat);
                var text = config.ShowNames ? balance.Definition.Name + "  " + value : value;
                var iconWidth = config.ShowIcons ? iconSize + ImGui.GetStyle().ItemSpacing.X : 0f;
                var desired = Math.Min(width, ImGui.CalcTextSize(text).X + iconWidth);
                if (EntryCount > 0)
                {
                    if (config.Orientation == CurrencyOrientation.Horizontal && used + gap + desired <= width)
                    { ImGui.SameLine(startX + used + gap); used += gap; }
                    else { used = 0f; RowCount++; }
                }
                ImGui.BeginGroup();
                if (config.ShowIcons)
                {
                    if (textures != null)
                    {
                        if (!icons.TryGetValue(balance.Definition.IconId, out var shared))
                            icons[balance.Definition.IconId] = shared = textures.GetFromGameIcon(new GameIconLookup(balance.Definition.IconId));
                        var wrap = shared.GetWrapOrEmpty();
                        ImGui.Image(wrap.Handle, new Vector2(iconSize));
                    }
                    else ImGui.Dummy(new Vector2(iconSize));
                    ImGui.SameLine();
                }
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + Math.Max(16f, desired - iconWidth));
                try { ImGui.TextUnformatted(text); }
                finally { ImGui.PopTextWrapPos(); }
                ImGui.EndGroup();
                used += ImGui.GetItemRectSize().X;
                EntryCount++;
            }
            if (EntryCount == 0 && editing) ImGui.TextWrapped(config.SelectedCurrencies.Count == 0
                ? "Currency Tracker: select currencies in settings." : "Currency Tracker: waiting for selected currency data.");
        }
        finally { ImGui.PopStyleVar(); }
    }
}
