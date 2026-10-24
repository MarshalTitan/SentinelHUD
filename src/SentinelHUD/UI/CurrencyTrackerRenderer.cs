using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using SentinelHUD.Core;

namespace SentinelHUD.UI;

public sealed class CurrencyTrackerRenderer(ITextureProvider? textures)
{
    private readonly Dictionary<uint, ISharedImmediateTexture> icons = new();
    private readonly List<DisplayEntry> entries = new(CurrencyPolicy.MaximumSelected);
    public int RowCount { get; private set; }
    public int EntryCount { get; private set; }

    public void Draw(CurrencyTrackerConfiguration config, IReadOnlyList<CurrencyBalance> balances,
        float scale, float opacity, bool editing)
    {
        RowCount = 1; EntryCount = 0; entries.Clear();
        var width = Math.Max(1f, ImGui.GetContentRegionAvail().X);
        var iconSize = Math.Min(width, Math.Max(ImGui.GetTextLineHeight(), config.BarHeight * scale));
        var iconWidth = config.ShowIcons ? iconSize + Math.Min(4f * scale, width * 0.1f) : 0f;
        var textWidth = Math.Max(1f, width - iconWidth);
        var start = ImGui.GetCursorPos();
        var gap = 16f * scale;
        var rowGap = ImGui.GetStyle().ItemSpacing.Y;
        var widest = 0f;
        foreach (var balance in balances)
        {
            var value = CurrencyPolicy.Format(balance.Amount, balance.Definition.ItemId == 1 ? null : balance.Cap,
                config.ShowCaps, config.NumberFormat);
            var text = config.ShowNames ? balance.Definition.Name + "  " + value : value;
            var size = ImGui.CalcTextSize(text, false, textWidth);
            size.X = Math.Min(textWidth, size.X);
            var desired = Math.Min(width, iconWidth + size.X);
            entries.Add(new DisplayEntry(balance.Definition.IconId, text, size, desired,
                Math.Max(config.ShowIcons ? iconSize : 0f, size.Y)));
            widest = Math.Max(widest, desired);
        }

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
        try
        {
            var y = start.Y;
            for (var first = 0; first < entries.Count;)
            {
                var last = first + 1;
                var rowWidth = entries[first].Width;
                var rowHeight = entries[first].Height;
                if (config.Orientation == CurrencyOrientation.Horizontal)
                    while (last < entries.Count && rowWidth + gap + entries[last].Width <= width)
                    {
                        rowWidth += gap + entries[last].Width;
                        rowHeight = Math.Max(rowHeight, entries[last].Height);
                        last++;
                    }
                var right = config.Alignment == CurrencyAlignment.Right;
                var blockWidth = config.Orientation == CurrencyOrientation.Vertical ? widest : rowWidth;
                var x = start.X + (right ? Math.Max(0f, width - blockWidth) : 0f);
                for (var i = first; i < last; i++)
                {
                    var entry = entries[i];
                    if (config.ShowIcons)
                    {
                        ImGui.SetCursorPos(new Vector2(x, y));
                        if (textures != null)
                        {
                            if (!icons.TryGetValue(entry.IconId, out var shared))
                                icons[entry.IconId] = shared = textures.GetFromGameIcon(new GameIconLookup(entry.IconId));
                            var wrap = shared.GetWrapOrEmpty();
                            ImGui.Image(wrap.Handle, new Vector2(iconSize));
                        }
                        else ImGui.Dummy(new Vector2(iconSize));
                    }
                    var textX = right && config.Orientation == CurrencyOrientation.Vertical
                        ? start.X + width - entry.TextSize.X : x + iconWidth;
                    ImGui.SetCursorPos(new Vector2(textX, y));
                    ImGui.PushTextWrapPos(start.X + width);
                    try { ImGui.TextUnformatted(entry.Text); }
                    finally { ImGui.PopTextWrapPos(); }
                    rowHeight = Math.Max(rowHeight, ImGui.GetItemRectSize().Y);
                    if (right && width >= 600f && scale > 1f && !config.ShowNames && !config.ShowIcons)
                        Console.WriteLine($"CURRENCY TRACE text={entry.Text}; width={width}; start={start}; measured={entry.TextSize}; rowWidth={rowWidth}; x={textX}; rect={ImGui.GetItemRectMin()} to {ImGui.GetItemRectMax()}; font={ImGui.GetFontSize()}");
                    x += entry.Width + gap;
                    EntryCount++;
                }
                y += rowHeight + rowGap;
                first = last;
                if (first < entries.Count) RowCount++;
            }
            if (entries.Count > 0)
            {
                ImGui.SetCursorPos(start);
                ImGui.Dummy(new Vector2(width, Math.Max(1f, y - start.Y - rowGap)));
            }
            else if (editing) ImGui.TextWrapped(config.SelectedCurrencies.Count == 0
                ? "Currency Tracker: select currencies in settings." : "Currency Tracker: waiting for selected currency data.");
        }
        finally { ImGui.PopStyleVar(); }
    }

    private readonly record struct DisplayEntry(uint IconId, string Text, Vector2 TextSize, float Width, float Height);
}
