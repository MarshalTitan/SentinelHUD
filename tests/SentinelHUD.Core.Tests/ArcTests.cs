using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using SentinelHUD.Core;

internal static class ArcTests
{
    private static void Check(bool value) { if (!value) throw new InvalidOperationException("Arc/cast assertion failed."); }
    private static void Near(float expected, float value) => Check(Math.Abs(expected - value) < 0.01f);

    public static void Migration()
    {
        var original = HudConfigurationMigrator.Normalize(new HudConfigurationData());
        original.Player.Layout.AnchorX = 0.38f;
        original.Player.Width = 477f;
        original.Player.BarHeight = 31f;
        original.Player.ShowCastRemainingTime = true;
        original.Target.ClickableArea = ModuleClickableArea.HeaderOrName;
        original.Target.MpDisplay = MpDisplayMode.TextOnly;
        original.FocusTarget.TargetOfFocus.ClickToTarget = false;
        original.Camera.Enabled = true;
        original.Appearance.ConfigurationTheme = ConfigurationWindowTheme.Classic;
        original.Appearance.ModernWindow.Minimized = true;
        original.Appearance.PlayerHpColourMode = HpColourMode.HealthStateGradient;
        original.Version = 12;
        var before = JsonSerializer.SerializeToNode(original)!.AsObject();
        StripNew(before);
        before["Version"] = 12;
        var loaded = JsonSerializer.Deserialize<HudConfigurationData>(before.ToJsonString())!;
        HudConfigurationMigrator.Normalize(loaded);
        Check(loaded.Version == HudConfigurationData.CurrentVersion);
        Check(loaded.Player.BarStyle == HudBarStyle.Standard && loaded.Target.BarStyle == HudBarStyle.Standard);
        Check(!loaded.PlayerCast.Enabled && !loaded.TargetCast.Enabled);
        var after = JsonSerializer.SerializeToNode(loaded)!.AsObject();
        StripNew(before); StripNew(after);
        Check(JsonNode.DeepEquals(before, after));
    }

    private static void StripNew(JsonObject json)
    {
        json.Remove("Version"); json.Remove("PlayerCast"); json.Remove("TargetCast");
        foreach (var name in new[] { "Player", "Target", "FocusTarget", "TargetOfTarget", "TargetingMeCounter" })
        {
            var child = json[name]!.AsObject();
            child.Remove("BarStyle"); child.Remove("ArcHeight"); child.Remove("ArcOrientation");
        }
    }

    public static void Persistence()
    {
        var config = new HudConfigurationData();
        config.Player.BarStyle = HudBarStyle.Arc;
        config.Target.BarStyle = HudBarStyle.Arc;
        config.Player.ArcHeight = 361f;
        config.Target.ArcOrientation = ArcOrientation.Left;
        config.PlayerCast.Enabled = true;
        config.PlayerCast.Layout.AnchorX = 0.59f;
        config.PlayerCast.Width = 432f;
        config.PlayerCast.BarHeight = 27f;
        config.PlayerCast.ShowCastRemainingTime = false;
        config.TargetCast.Enabled = true;
        config.TargetCast.Visibility = ModuleVisibilityCondition.CombatOnly;
        var restored = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(JsonSerializer.Serialize(config))!);
        Check(restored.Player.BarStyle == HudBarStyle.Arc && restored.Target.BarStyle == HudBarStyle.Arc);
        Check(restored.FocusTarget.BarStyle == HudBarStyle.Standard);
        Near(361f, restored.Player.ArcHeight);
        Check(restored.Target.ArcOrientation == ArcOrientation.Left);
        Near(0.59f, restored.PlayerCast.Layout.AnchorX);
        Near(432f, restored.PlayerCast.Width);
        Near(27f, restored.PlayerCast.BarHeight);
        Check(!restored.PlayerCast.ShowCastRemainingTime);
        Check(restored.TargetCast.Visibility == ModuleVisibilityCondition.CombatOnly);
        var once = JsonSerializer.Serialize(restored);
        HudConfigurationMigrator.Normalize(restored);
        Check(once == JsonSerializer.Serialize(restored));
        restored.Player.ArcHeight = float.NaN;
        restored.Target.ArcHeight = -1f;
        restored.Player.BarStyle = (HudBarStyle)99;
        restored.Player.ArcOrientation = (ArcOrientation)99;
        restored.PlayerCast = null!;
        restored.TargetCast.Layout = null!;
        HudConfigurationMigrator.Normalize(restored);
        Near(240f, restored.Player.ArcHeight);
        Near(ArcBarGeometry.MinimumHeight, restored.Target.ArcHeight);
        Check(restored.Player.BarStyle == HudBarStyle.Standard);
        Check(restored.Player.ArcOrientation == ArcOrientation.Auto);
        Check(!restored.PlayerCast.Enabled && restored.TargetCast.Layout is not null);
    }

    public static void Geometry()
    {
        Check(!ArcBarGeometry.IsRight(ArcOrientation.Auto, HudModuleKind.Player));
        Check(ArcBarGeometry.IsRight(ArcOrientation.Auto, HudModuleKind.Target));
        Check(ArcBarGeometry.IsRight(ArcOrientation.Right, HudModuleKind.Player));
        foreach (var width in new[] { 140f, 285f, 620f })
        foreach (var height in new[] { 100f, 240f, 600f })
        foreach (var thickness in new[] { 8f, 20f, 34f })
        {
            var left = new ArcBarGeometry(); var right = new ArcBarGeometry();
            left.Update(width, height, thickness, false); right.Update(width, height, thickness, true);
            Check(left.TextWidth >= width * 0.45f);
            for (var i = 0; i <= ArcBarGeometry.Segments; i++)
            {
                var a = left.Hp[i]; var b = right.Hp[i];
                Near(width, a.X + b.X); Near(a.Y, b.Y);
                Near(width, left.Mp[i].X + right.Mp[i].X);
                Check(a.X - left.Thickness / 2f >= 0f && a.X + left.Thickness / 2f <= width);
                Check(a.Y - left.Thickness / 2f >= 0f && a.Y + left.Thickness / 2f <= height);
                Check(left.Mp[i].X + left.Thickness / 4f < left.TextLeft);
                if (i > 0) Check(left.Hp[i].Y < left.Hp[i - 1].Y);
            }
            var revision = left.Revision;
            for (var i = 0; i < 1000; i++) left.Update(width, height, thickness, false);
            Check(revision == left.Revision);
        }
        var invalid = new ArcBarGeometry();
        invalid.Update(float.NaN, float.NaN, float.NaN, false);
        Check(float.IsFinite(invalid.Hp[0].X));
    }

    public static void FlatStrips()
    {
        foreach (var right in new[] { false, true })
        foreach (var width in new[] { 140f, 285f, 620f })
        foreach (var height in new[] { 100f, 240f, 600f })
        {
            var geometry = new ArcBarGeometry();
            geometry.Update(width, height, 20f, right);
            foreach (var mp in new[] { false, true })
            {
                var points = mp ? geometry.Mp : geometry.Hp;
                var thickness = geometry.Thickness * (mp ? 0.5f : 1f);
                for (var i = 0; i < ArcBarGeometry.Segments; i++)
                {
                    var start = Vector2.Lerp(points[i], points[i + 1], 0.17f);
                    var end = Vector2.Lerp(points[i], points[i + 1], 0.83f);
                    var quad = ArcStripGeometry.Section(start, end, thickness);
                    Near(quad.A.Y, quad.B.Y); Near(quad.C.Y, quad.D.Y);
                    Near(start.Y, quad.A.Y); Near(end.Y, quad.C.Y);
                    Near(thickness, quad.B.X - quad.A.X);
                    Near(thickness, quad.C.X - quad.D.X);
                    Check(quad.C.Y < quad.A.Y);
                    Check(quad.A.X >= 0f && quad.B.X <= width && quad.C.X <= width && quad.D.X >= 0f);
                }
                var top = ArcStripGeometry.Section(points[^2], points[^1], thickness);
                var bottom = ArcStripGeometry.Section(points[0], points[1], thickness);
                Near(points[^1].Y, top.C.Y); Near(points[^1].Y, top.D.Y);
                Near(points[0].Y, bottom.A.Y); Near(points[0].Y, bottom.B.Y);
            }
        }
    }

    public static void Resize()
    {
        var start = new Vector2(500f, 300f); var size = new Vector2(285f, 300f);
        var work = new Vector2(1920f, 1080f);
        var top = ArcBarGeometry.Resize(285f, 240f, 1.5f, EditorResizeEdges.Top,
            new Vector2(0f, -60f), start, size, Vector2.Zero, work);
        Near(280f, top.BarHeight); Near(240f, top.Position.Y);
        Near(start.Y + size.Y, top.Position.Y + top.ApproximateSize.Y);
        var corner = ArcBarGeometry.Resize(285f, 240f, 1f, EditorResizeEdges.Bottom | EditorResizeEdges.Right,
            new Vector2(30f, 90f), start, size, Vector2.Zero, work);
        Near(315f, corner.Width); Near(330f, corner.BarHeight);
        var bound = ArcBarGeometry.Resize(285f, 240f, 1f, EditorResizeEdges.Bottom,
            new Vector2(0f, 9999f), start, size, Vector2.Zero, work);
        Near(ArcBarGeometry.MaximumHeight, bound.BarHeight);
        Check(bound.Position.Y + bound.ApproximateSize.Y <= work.Y);
    }

    public static void Casts()
    {
        var parent = HudConfigurationDefaults.CreatePlayer();
        var cast = HudConfigurationDefaults.CreateCast(false);
        Check(CastPresentationPolicy.ShowAttached(parent, cast));
        parent.BarStyle = HudBarStyle.Arc;
        Check(!CastPresentationPolicy.ShowAttached(parent, cast));
        Check(!CastPresentationPolicy.ShowSeparate(cast, false, true, true, true, true));
        cast.Enabled = true;
        Check(CastPresentationPolicy.ShowSeparate(cast, false, false, false, false, false));
        Check(!CastPresentationPolicy.ShowSeparate(cast, true, false, true, true, true));
        Check(CastPresentationPolicy.ShowSeparate(cast, true, true, true, false, false));
        cast.Visibility = ModuleVisibilityCondition.CombatOnly;
        Check(!CastPresentationPolicy.ShowSeparate(cast, true, true, true, false, true));
        Check(CastPresentationPolicy.ShowSeparate(cast, true, true, true, true, false));
        parent.Enabled = false;
        Check(CastPresentationPolicy.ShowSeparate(cast, true, true, true, true, false));
        parent.BarStyle = HudBarStyle.Standard;
        Check(!CastPresentationPolicy.ShowAttached(parent, cast));
    }
}
