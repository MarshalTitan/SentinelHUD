using System.Text.Json;
using System.Text.Json.Nodes;
using SentinelHUD.Core;

internal static class ArcRefinementTests
{
    private static void Check(bool value) { if (!value) throw new InvalidOperationException("Arc text/cast refinement assertion failed."); }

    public static void Preferences()
    {
        var old = HudConfigurationMigrator.Normalize(new HudConfigurationData());
        old.Player.BarStyle = HudBarStyle.Arc; old.Player.ArcHeight = 371f;
        old.Target.Layout.AnchorX = 0.67f; old.PlayerCast.Enabled = true; old.TargetCast.Enabled = true;
        old.TargetCast.Layout.AnchorY = 0.8f; old.CurrencyTracker.Alignment = CurrencyAlignment.Right;
        old.CurrencyTracker.SelectedCurrencies = [25, 1, 28];
        var before = JsonSerializer.SerializeToNode(old)!.AsObject(); Strip(before);
        var restored = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(before.ToJsonString())!);
        Check(restored.Version == HudConfigurationData.CurrentVersion && restored.Player.ArcTextScale == 1f && restored.Target.ArcTextScale == 1f);
        var after = JsonSerializer.SerializeToNode(restored)!.AsObject(); Strip(after);
        Check(JsonNode.DeepEquals(before, after));
        restored.Player.ArcTextScale = 1.7f; restored.Target.ArcTextScale = 0.6f;
        var roundTrip = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(JsonSerializer.Serialize(restored))!);
        Check(roundTrip.Player.ArcTextScale == 1.7f && roundTrip.Target.ArcTextScale == 0.6f);
        Check(roundTrip.Player.ArcHeight == 371f && roundTrip.TargetCast.Layout.AnchorY == 0.8f);
        roundTrip.Player.ArcTextScale = float.NaN; roundTrip.Target.ArcTextScale = 20f;
        HudConfigurationMigrator.Normalize(roundTrip);
        Check(roundTrip.Player.ArcTextScale == 1f && roundTrip.Target.ArcTextScale == 2f);
        Check(ArcTextPolicy.HpCurrent(205960, restored.Player) == "205,960");
        Check(ArcTextPolicy.HpMaximum(250000, restored.Player) == "250,000");
        Check(ArcTextPolicy.HpCurrent(205960, restored.Player, false) == "");
        Check(ArcTextPolicy.HpMaximum(250000, restored.Player, false) == "");
        restored.Player.ArcValueFormat = ArcValueFormat.CurrentOnly;
        Check(ArcTextPolicy.HpMaximum(250000, restored.Player) == "");
        Check(ArcTextPolicy.Values(8000, 10000, restored.Player) == "8,000");
        restored.Player.ArcValueFormat = ArcValueFormat.CurrentAndTotal;
        Check(ArcTextPolicy.Values(8000, 10000, restored.Player) == "8,000 / 10,000");
        static void Strip(JsonObject json)
        {
            foreach (var name in new[] { "Player", "Target", "FocusTarget", "TargetOfTarget", "PlayerCast", "TargetCast", "TargetingMeCounter", "CurrencyTracker" })
                json[name]!.AsObject().Remove("ArcTextScale");
        }
    }

    public static void CastRouting()
    {
        var player = CastSnapshot.Create(10, true, 120, 1, 0.75f, 2.5f);
        var stale = CastSnapshot.Create(10, true, 999, 1, 2f, 8f);
        var target = CastSnapshot.ForTarget(10, player, 10, stale);
        Check(target == player && Math.Abs(target.Progress - 0.3f) < 0.001f);
        Check(HudFormatting.RemainingCastTime(target.Current, target.Total) == "1.75s");
        var pc = HudConfigurationDefaults.CreateCast(false); var tc = HudConfigurationDefaults.CreateCast(true);
        pc.Enabled = true; tc.Enabled = true;
        Check(CastPresentationPolicy.ShowSeparate(pc, true, player.IsCasting, true, false, false));
        Check(CastPresentationPolicy.ShowSeparate(tc, true, target.IsCasting, true, false, false));
        // Targeting another player, enemy or casting NPC must use that actor, never the player's cast.
        foreach (var actorId in new ulong[] { 20, 30, 40 })
        {
            var other = CastSnapshot.Create(actorId, true, 321, 1, 1.5f, 3f);
            Check(CastSnapshot.ForTarget(10, player, actorId, other) == other);
            Check(!CastSnapshot.ForTarget(10, player, actorId, default).IsCasting);
        }
        var stopped = CastSnapshot.Create(10, false, 120, 1, 1f, 2.5f);
        Check(!CastSnapshot.ForTarget(10, stopped, 10, stale).IsCasting);
        Check(!CastSnapshot.ForTarget(10, player, 20, stale).IsCasting);
        Check(!CastSnapshot.ForTarget(10, player, 0, player).IsCasting);
        Check(!CastSnapshot.Create(20, true, 321, 1, float.NaN, 3f).IsCasting);
        Check(!CastSnapshot.Create(20, true, 321, 1, 1f, 0f).IsCasting);
        Check(!CastSnapshot.Create(20, true, 0, 1, 1f, 3f).IsCasting);
        Check(CastSnapshot.Create(20, true, 321, 1, 100f, 3f).Progress == 1f);
        Check(CastSnapshot.Create(20, true, 321, 1, -1f, 3f).Progress == 0f);
        tc.Enabled = false;
        Check(!CastPresentationPolicy.ShowSeparate(tc, true, target.IsCasting, true, false, false));
        Check(CastPresentationPolicy.ShowSeparate(pc, true, player.IsCasting, true, false, false));
    }
}
