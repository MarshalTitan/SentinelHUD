using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

/// <summary>Reads only selected balances at a bounded cadence; no native pointer is retained.</summary>
public sealed class CurrencyDataService(IDataManager data)
{
    private readonly Dictionary<uint, CurrencyDefinition> definitions = new();
    private readonly List<CurrencyDefinition> catalog = new();
    private long nextRefresh;
    private long nextCatalogAttempt;
    public IReadOnlyList<CurrencyDefinition> Catalog { get { EnsureCatalog(); return catalog; } }
    public IReadOnlyList<CurrencyBalance> Snapshot { get; private set; } = Array.Empty<CurrencyBalance>();
    public string State { get; private set; } = "Waiting for currency data.";

    private void EnsureCatalog()
    {
        if (catalog.Count > 0 || Environment.TickCount64 < nextCatalogAttempt) return;
        nextCatalogAttempt = Environment.TickCount64 + 10000;
        try
        {
            var tomes = new HashSet<uint>();
            foreach (var row in data.GetExcelSheet<TomestonesItem>())
                if (row.Item.RowId != 0) tomes.Add(row.Item.RowId);
            var loaded = new List<CurrencyDefinition>();
            foreach (var item in data.GetExcelSheet<Item>())
            {
                if (!CurrencyPolicy.IsCurrency(item.RowId, item.ItemUICategory.RowId, item.FilterGroup, item.StackSize)
                    && !tomes.Contains(item.RowId)) continue;
                var name = item.Name.ToString();
                if (string.IsNullOrWhiteSpace(name) || item.Icon == 0) continue;
                loaded.Add(new CurrencyDefinition(item.RowId, name, item.Icon,
                    item.RowId != 1 && item.StackSize > 1 ? item.StackSize : null, tomes.Contains(item.RowId)));
            }
            loaded.Sort(static (a, b) => a.ItemId.CompareTo(b.ItemId));
            foreach (var entry in loaded) { definitions[entry.ItemId] = entry; catalog.Add(entry); }
            State = $"{catalog.Count} currencies available.";
        }
        catch { State = "Currency data is unavailable; retrying shortly."; }
    }

    public unsafe void Update(CurrencyTrackerConfiguration config, bool active, bool playerAvailable)
    {
        if (!active || !config.Enabled)
        {
            Snapshot = Array.Empty<CurrencyBalance>(); nextRefresh = 0; return;
        }
        if (Environment.TickCount64 < nextRefresh) return;
        nextRefresh = Environment.TickCount64 + CurrencyPolicy.RefreshMilliseconds;
        EnsureCatalog();
        var result = new List<CurrencyBalance>(config.SelectedCurrencies.Count);
        var inventory = playerAvailable ? InventoryManager.Instance() : null;
        foreach (var id in config.SelectedCurrencies)
        {
            if (!definitions.TryGetValue(id, out var definition)) continue;
            uint? amount = null;
            var cap = definition.Cap;
            if (inventory != null)
            {
                try
                {
                    if (id == 1) amount = inventory->GetGil();
                    else if (id is >= 20 and <= 22)
                    {
                        var company = (byte)(id - 19);
                        amount = inventory->GetCompanySeals(company);
                        var maximum = inventory->GetMaxCompanySeals(company);
                        cap = maximum > 0 ? maximum : null;
                    }
                    else if (id == 25) amount = inventory->GetWolfMarks();
                    else if (id == 29) amount = inventory->GetGoldSaucerCoin();
                    else if (definition.IsTomestone) amount = inventory->GetTomestoneCount(id);
                    else
                    {
                        var count = inventory->GetInventoryItemCount(id);
                        if (count >= 0) amount = (uint)count;
                    }
                }
                catch { amount = null; }
            }
            result.Add(new CurrencyBalance(definition, amount, cap));
        }
        Snapshot = result.ToArray();
        State = inventory != null ? $"{result.Count} selected currencies; balances refresh twice per second."
            : "Waiting for character inventory; unavailable balances show --.";
    }
}
