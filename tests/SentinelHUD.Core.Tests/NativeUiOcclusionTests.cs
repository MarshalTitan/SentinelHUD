using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using SentinelHUD.Core;

internal static class NativeUiOcclusionTests
{
    private static void Check(bool condition) { if (!condition) throw new InvalidOperationException("Native UI occlusion assertion failed"); }
    public static void Geometry()
    {
        var mask = new NativeUiOcclusionMask();
        var pieces = new List<ScreenRectangle>(); var scratch = new List<ScreenRectangle>();
        var source = new ScreenRectangle(10f, 10f, 110f, 110f);
        mask.Clip(source, pieces, scratch); Check(pieces.SequenceEqual(new[] { source }));
        mask.Add(new(40f, 40f, 70f, 80f)); mask.Clip(source, pieces, scratch);
        Check(pieces.Count == 4 && pieces.Sum(r => r.Width * r.Height) == 8800f);
        Check(mask.Contains(new Vector2(50f, 50f)) && !mask.Contains(new Vector2(70f, 50f)));
        Check(!mask.Intersects(new(70f, 40f, 90f, 80f)));
        mask.Add(new(35f, 50f, 90f, 90f));
        mask.Add(new(float.NaN, 0f, 1f, 1f)); mask.Add(new(9f, 9f, 8f, 8f));
        Check(mask.Blockers.Count == 2);
        VerifyCoverage(mask, source, pieces, scratch);
        mask.Add(new(-10f, -10f, 200f, 200f)); mask.Clip(source, pieces, scratch);
        Check(mask.Blockers.Count == 1 && pieces.Count == 0);
        mask.Clear(); mask.Add(new(110f, 20f, 150f, 50f)); mask.Clip(source, pieces, scratch);
        Check(pieces.SequenceEqual(new[] { source }));
        var random = new Random(778);
        for (var trial = 0; trial < 60; trial++)
        {
            mask.Clear();
            for (var i = 0; i < 12; i++)
            {
                var x = random.Next(-20, 130); var y = random.Next(-20, 130);
                mask.Add(new(x, y, x + random.Next(1, 50), y + random.Next(1, 50)));
            }
            VerifyCoverage(mask, source, pieces, scratch);
        }
    }
    private static void VerifyCoverage(NativeUiOcclusionMask mask, ScreenRectangle source,
        List<ScreenRectangle> pieces, List<ScreenRectangle> scratch)
    {
        mask.Clip(source, pieces, scratch);
        for (var i = 0; i < pieces.Count; i++)
        {
            Check(NativeUiOcclusionMask.Valid(pieces[i]) && !mask.Intersects(pieces[i]));
            for (var j = i + 1; j < pieces.Count; j++) Check(!pieces[i].Intersects(pieces[j]));
        }
        for (var x = 10.5f; x < 110f; x += 3f)
        for (var y = 10.5f; y < 110f; y += 3f)
        {
            var n = pieces.Count(r => x >= r.Left && x < r.Right && y >= r.Top && y < r.Bottom);
            Check(n == (mask.Contains(new(x, y)) ? 0 : 1));
        }
    }
    public static void Windows()
    {
        foreach (var name in new[] { "InventoryBuddy", "Inventory", "Character", "Achievement", "AreaMap", "ContentsFinder", "Currency", "Journal", "NewNativeWindow" })
            Check(NativeUiWindowPolicy.IsWindow(name, true, false));
        foreach (var name in new[] { "ContextMenu", "ContextSub", "SelectString", "ItemDetail", "ActionDetail" })
            Check(NativeUiWindowPolicy.IsWindow(name, false, false));
        foreach (var name in new[] { "_ParameterWidget", "_TargetInfo", "_ActionBar", "ChatLogPanel_0", "NamePlate", "FlyText", "ScreenLog", "" })
            Check(!NativeUiWindowPolicy.IsWindow(name, true, true));
        Check(!NativeUiWindowPolicy.IsWindow("ActorInWorld", false, false));
    }
    public static void Persistence()
    {
        var config = HudConfigurationMigrator.Normalize(new HudConfigurationData());
        config.Version = 15; config.Player.BarStyle = HudBarStyle.Arc; config.Player.ArcTextScale = 1.4f;
        config.CurrencyTracker.Alignment = CurrencyAlignment.Right; config.TargetCast.Layout.AnchorY = 0.61f;
        config.EncounterAwareness.NativeColours.Enabled = true; config.EncounterAwareness.Palette.Enabled = true;
        var before = JsonSerializer.SerializeToNode(config)!.AsObject(); before.Remove("RespectNativeWindows");
        var next = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(before.ToJsonString())!);
        Check(next.Version == HudConfigurationData.CurrentVersion && next.RespectNativeWindows);
        var after = JsonSerializer.SerializeToNode(next)!.AsObject();
        before.Remove("Version"); after.Remove("Version"); after.Remove("RespectNativeWindows");
        Check(JsonNode.DeepEquals(before, after));
        next.RespectNativeWindows = false;
        var saved = JsonSerializer.Serialize(next);
        var restored = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(saved)!);
        Check(!restored.RespectNativeWindows && saved == JsonSerializer.Serialize(restored));
    }
}
