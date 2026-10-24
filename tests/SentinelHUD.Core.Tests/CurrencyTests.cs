using System.Text.Json;
using System.Text.Json.Nodes;
using SentinelHUD.Core;

internal static class CurrencyTests
{
    private static void Check(bool value) { if (!value) throw new InvalidOperationException("Currency/arc preference assertion failed."); }
    public static void Migration()
    {
        var source = HudConfigurationMigrator.Normalize(new HudConfigurationData());
        source.Version = 13;
        source.Player.BarStyle = HudBarStyle.Arc; source.Player.ArcHeight = 387f;
        source.Player.Layout.AnchorX = 0.39f; source.Player.ShowMaximumHp = false;
        source.Target.ArcOrientation = ArcOrientation.Left; source.Target.ClickableArea = ModuleClickableArea.HeaderOrName;
        source.PlayerCast.Enabled = true; source.PlayerCast.Layout.AnchorY = 0.69f;
        source.Camera.Enabled = true; source.Convenience.SkipDialogue = true;
        source.Appearance.ModernWindow.Minimized = true;
        var before = JsonSerializer.SerializeToNode(source)!.AsObject();
        Strip(before); before["Version"] = 13;
        var restored = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(before.ToJsonString())!);
        Check(restored.Version == 14 && !restored.CurrencyTracker.Enabled);
        Check(restored.Player.ArcValueFormat == ArcValueFormat.CurrentAndTotal && restored.Player.ArcShowPercentages);
        var after = JsonSerializer.SerializeToNode(restored)!.AsObject();
        Strip(before); Strip(after); Check(JsonNode.DeepEquals(before, after));
        static void Strip(JsonObject json)
        {
            json.Remove("Version"); json.Remove("CurrencyTracker");
            foreach (var name in new[] { "Player", "Target", "FocusTarget", "TargetOfTarget", "TargetingMeCounter", "PlayerCast", "TargetCast" })
            { json[name]!.AsObject().Remove("ArcValueFormat"); json[name]!.AsObject().Remove("ArcShowPercentages"); }
        }
    }

    public static void Persistence()
    {
        var source = new HudConfigurationData();
        source.CurrencyTracker.Enabled = true; source.CurrencyTracker.SelectedCurrencies = [1, 25, 28, 25, 0, 26807];
        source.CurrencyTracker.Layout.AnchorY = 0.91f; source.CurrencyTracker.Width = 533f;
        source.CurrencyTracker.Orientation = CurrencyOrientation.Vertical; source.CurrencyTracker.ShowNames = true;
        source.CurrencyTracker.Alignment = CurrencyAlignment.Right;
        source.CurrencyTracker.ShowCaps = false; source.CurrencyTracker.ShowIcons = false;
        source.Player.ArcValueFormat = ArcValueFormat.CurrentOnly; source.Target.ArcShowPercentages = false;
        var restored = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(JsonSerializer.Serialize(source))!);
        Check(restored.CurrencyTracker.SelectedCurrencies.SequenceEqual(new uint[] { 1, 25, 28, 26807 }));
        Check(restored.CurrencyTracker.Width == 533f && restored.CurrencyTracker.Layout.AnchorY == 0.91f);
        Check(restored.CurrencyTracker.Orientation == CurrencyOrientation.Vertical && restored.CurrencyTracker.ShowNames);
        Check(restored.CurrencyTracker.Alignment == CurrencyAlignment.Right);
        Check(!restored.CurrencyTracker.ShowCaps && !restored.CurrencyTracker.ShowIcons);
        Check(restored.Player.ArcValueFormat == ArcValueFormat.CurrentOnly && !restored.Target.ArcShowPercentages);
        var once = JsonSerializer.Serialize(restored); HudConfigurationMigrator.Normalize(restored);
        Check(once == JsonSerializer.Serialize(restored));
        Check(CurrencyPolicy.NormalizeSelection(Enumerable.Range(1, 100).Select(i => (uint)i)).Count == 24);
        Check(CurrencyPolicy.NormalizeSelection(Array.Empty<uint>()).Count == 0);
        Check(CurrencyPolicy.IsCurrency(1, 63, 16, 999999999));
        Check(!CurrencyPolicy.IsCurrency(123, 1, 16, 999));
        Check(!CurrencyPolicy.IsCurrency(123, 63, 1, 999));
        var navigation = new SentinelHudModernNavigationState(); navigation.SelectPrimary(SentinelHudModernNavigationState.HudId);
        navigation.SelectContent(SentinelHudModernContentPage.CurrencyTracker);
        Check(navigation.ContentPage == SentinelHudModernContentPage.CurrencyTracker);
    }

    public static void Formatting()
    {
        Check(CurrencyPolicy.Format(1989, 2000, true, HudNumberFormat.Full) == "1,989 / 2,000");
        Check(CurrencyPolicy.Format(1989, 2000, false, HudNumberFormat.Full) == "1,989");
        Check(CurrencyPolicy.Format(123456789, null, true, HudNumberFormat.Full) == "123,456,789");
        Check(CurrencyPolicy.Format(0, 20000, true, HudNumberFormat.Full) == "0 / 20,000");
        Check(CurrencyPolicy.Format(null, 2000, true, HudNumberFormat.Full) == "--");
        Check(CurrencyPolicy.Format(1989, 2000, true, HudNumberFormat.Compact) == "2k / 2k");
    }

    public static void CatalogAndSearch()
    {
        foreach (var activeType in new uint[] { 1, 2, 3 }) Check(CurrencyPolicy.IsAvailable(65000, true, activeType));
        Check(!CurrencyPolicy.IsAvailable(65000, true, 0));
        Check(!CurrencyPolicy.IsAvailable(65000, true, 4));
        Check(CurrencyPolicy.IsAvailable(65000, false, 0));
        Check(!CurrencyPolicy.IsAvailable(26154, false, 0));
        Check(CurrencyPolicy.MatchesSearch("Allagan Tomestone of Mnemonics", "  mNeMoNiCs "));
        Check(CurrencyPolicy.MatchesSearch("Wolf Marks", " "));
        Check(!CurrencyPolicy.MatchesSearch("Wolf Marks", "Gil"));
    }

    public static void NarrowRightLayout()
    {
        var old = HudConfigurationMigrator.Normalize(new HudConfigurationData());
        old.CurrencyTracker.Enabled = true; old.CurrencyTracker.Width = 231f;
        old.CurrencyTracker.SelectedCurrencies = [25, 1, 28]; old.Target.ArcHeight = 369f;
        var before = JsonSerializer.SerializeToNode(old)!.AsObject();
        before["CurrencyTracker"]!.AsObject().Remove("Alignment");
        var upgraded = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(before.ToJsonString())!);
        Check(upgraded.Version == 14 && upgraded.CurrencyTracker.Alignment == CurrencyAlignment.Left);
        var after = JsonSerializer.SerializeToNode(upgraded)!.AsObject();
        after["CurrencyTracker"]!.AsObject().Remove("Alignment");
        Check(JsonNode.DeepEquals(before, after));
        var source = new HudConfigurationData();
        source.CurrencyTracker.Width = 64f; source.CurrencyTracker.Layout.AnchorX = 1f;
        source.CurrencyTracker.Alignment = CurrencyAlignment.Right;
        source.Player.Width = 64f;
        var restored = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(JsonSerializer.Serialize(source))!);
        Check(restored.CurrencyTracker.Width == 64f && restored.Player.Width == 180f);
        Check(restored.CurrencyTracker.Alignment == CurrencyAlignment.Right);
        var work = new System.Numerics.Vector2(1920f, 1080f);
        var size = new System.Numerics.Vector2(64f, 100f);
        var position = LayoutPolicy.ToPixelPosition(restored.CurrencyTracker.Layout, System.Numerics.Vector2.Zero, work, size);
        Check(position.X + size.X == work.X);
        var resized = LayoutPolicy.ResizeFromEdges(180f, 20f, 4, EditorResizeEdges.Left,
            new System.Numerics.Vector2(500f, 0f), new System.Numerics.Vector2(1740f, 700f),
            new System.Numerics.Vector2(180f, 100f), System.Numerics.Vector2.Zero, work, HudSizingPolicy.CurrencyMinimumWidth);
        Check(resized.Width == 64f && resized.Position.X + resized.Width == work.X);
        restored.CurrencyTracker.Alignment = (CurrencyAlignment)999;
        Check(HudConfigurationMigrator.Normalize(restored).CurrencyTracker.Alignment == CurrencyAlignment.Left);
    }

    public static void ArcModes()
    {
        var config = new PlayerModuleConfiguration();
        Check(ArcTextPolicy.Values(205960, 205960, config) == "205,960 / 205,960");
        Check(ArcTextPolicy.Values(8000, 10000, config) == "8,000 / 10,000");
        config.ArcValueFormat = ArcValueFormat.CurrentOnly;
        Check(ArcTextPolicy.Values(205960, 205960, config) == "205,960");
        Check(ArcTextPolicy.Values(8000, 10000, config) == "8,000");
        Check(ArcTextPolicy.Percentage(8000, 10000, config) == "80.0%");
        config.ArcShowPercentages = false; Check(ArcTextPolicy.Percentage(8000, 10000, config) == "");
        config.ArcShowPercentages = true; Check(ArcTextPolicy.Percentage(8000, 10000, config, false) == "");
        config.NumberFormat = HudNumberFormat.Compact; Check(ArcTextPolicy.Values(205960, 205960, config) == "206k");
        Check(ArcTextPolicy.Values(205960, 205960, config, false, false) == "");
    }
}
