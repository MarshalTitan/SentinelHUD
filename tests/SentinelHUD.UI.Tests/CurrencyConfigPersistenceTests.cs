using Newtonsoft.Json;
using SentinelCore.Configuration;
using SentinelHUD.Core;
using HudConfig = SentinelHUD.Configuration;

internal static class CurrencyConfigPersistenceTests
{
    public static void Run()
    {
        var store = new DalamudJsonStore();
        using (var config = Open(store))
        {
            Check(config.Current.CurrencyTracker.SelectedCurrencies.SequenceEqual(new uint[] { 1 }),
                "New configuration must still default to Gil.");
            config.Update(c => c.CurrencyTracker.SelectedCurrencies = [28, 1, 25]);
            config.Update(c => c.CurrencyTracker.SelectedCurrencies =
                Array.FindAll(c.CurrencyTracker.SelectedCurrencies, id => id != 1));
        }

        using (var config = Open(store))
        {
            Check(config.Current.CurrencyTracker.SelectedCurrencies.SequenceEqual(new uint[] { 28, 25 }),
                "Disabled Gil reappeared or the selected order changed.");
            config.Update(c => c.CurrencyTracker.SelectedCurrencies =
                Array.FindAll(c.CurrencyTracker.SelectedCurrencies, id => id != 28));
        }

        using (var config = Open(store))
        {
            Check(config.Current.CurrencyTracker.SelectedCurrencies.SequenceEqual(new uint[] { 25 }),
                "A disabled non-Gil currency reappeared.");
            config.Update(c => c.CurrencyTracker.SelectedCurrencies = []);
        }

        using (var config = Open(store))
            Check(config.Current.CurrencyTracker.SelectedCurrencies.Length == 0, "Empty selection restored defaults.");

        store.LoadLegacyWithoutSelection();
        using var legacy = Open(store);
        Check(legacy.Current.CurrencyTracker.SelectedCurrencies.SequenceEqual(new uint[] { 1 }),
            "Old configs with no stored selection must keep the default.");
    }

    private static ConfigurationCoordinator<HudConfig> Open(DalamudJsonStore store) =>
        new(store, static () => new HudConfig(), static c => HudConfigurationMigrator.Normalize(c), saveAfterLoad: false);

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    // Dalamud saves object type metadata and its typed reader uses Json.NET's default
    // object creation behavior. Keep that behavior here so an initialized List<T> fails.
    private sealed class DalamudJsonStore : IConfigurationStore<HudConfig>
    {
        private string? json;

        public HudConfig? Load() => json is null ? null : JsonConvert.DeserializeObject<HudConfig>(json);

        public void Save(HudConfig config) =>
            json = JsonConvert.SerializeObject(config, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });

        public void LoadLegacyWithoutSelection() => json = """{"Version": 13, "CurrencyTracker": {}}""";
    }
}
