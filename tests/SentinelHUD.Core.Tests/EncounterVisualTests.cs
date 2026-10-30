using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using SentinelHUD.Core;

internal static class EncounterVisualTests
{
    private static void Check(bool value) { if (!value) throw new InvalidOperationException("Encounter visual assertion failed"); }
    private static void Near(Vector4 a, Vector4 b) => Check(Vector4.Distance(a, b) < 0.0001f);

    public static void Migration()
    {
        var old = HudConfigurationMigrator.Normalize(new HudConfigurationData());
        old.Version = 14; old.Player.BarStyle = HudBarStyle.Arc; old.Player.ArcTextScale = 1.65f;
        old.Target.Layout.AnchorX = 0.62f; old.TargetCast.Enabled = true; old.TargetCast.Layout.AnchorY = 0.81f;
        old.EncounterAwareness.Enabled = true; old.EncounterAwareness.SplatoonIntegrationEnabled = true;
        old.EncounterAwareness.TreatUnclassifiedSplatoonGeometryAsDanger = true;
        old.PlayerPositionMarker.CustomDangerColour.Set(new Vector4(0.6f, 0.1f, 0.7f, 1f));
        old.CurrencyTracker.Alignment = CurrencyAlignment.Right; old.CurrencyTracker.SelectedCurrencies = [1, 25, 28];
        old.Camera.Enabled = true; old.Appearance.ModernWindow.Minimized = true;
        var before = JsonSerializer.SerializeToNode(old)!.AsObject(); Strip(before); before["Version"] = 14;
        var restored = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(before.ToJsonString())!);
        Check(restored.Version == 15 && !restored.EncounterAwareness.Palette.Enabled);
        Check(!restored.EncounterAwareness.NativeColours.Enabled && !restored.EncounterAwareness.Overlay.Enabled);
        Check(!restored.PlayerPositionMarker.UseEncounterPalette);
        var after = JsonSerializer.SerializeToNode(restored)!.AsObject(); Strip(before); Strip(after);
        Check(JsonNode.DeepEquals(before, after));
        restored.EncounterAwareness.Palette.Enabled = true; restored.PlayerPositionMarker.UseEncounterPalette = true;
        restored.EncounterAwareness.NativeColours.Hostile.Source = EncounterColourSource.Palette;
        restored.EncounterAwareness.NativeColours.Hostile.Role = EncounterColourRole.Target;
        restored.EncounterAwareness.NativeColours.Enabled = true; restored.EncounterAwareness.Overlay.Radius = 12f;
        var json = JsonSerializer.Serialize(restored);
        var again = HudConfigurationMigrator.Normalize(JsonSerializer.Deserialize<HudConfigurationData>(json)!);
        Check(json == JsonSerializer.Serialize(again));
        static void Strip(JsonObject json)
        {
            json.Remove("Version");
            var encounter = json["EncounterAwareness"]!.AsObject();
            encounter.Remove("Palette"); encounter.Remove("NativeColours"); encounter.Remove("Overlay");
            json["PlayerPositionMarker"]!.AsObject().Remove("UseEncounterPalette");
        }
    }

    public static void Palette()
    {
        var config = new EncounterAwarenessConfiguration();
        config.NativeColours.Hostile.Custom.Set(new Vector4(0.1f, 0.2f, 0.9f, 0.8f));
        var custom = config.NativeColours.Hostile.Custom.ToVector4();
        Near(custom, EncounterColourPolicy.Resolve(config.NativeColours.Hostile, config.Palette));
        config.Palette.Enabled = true;
        Near(custom, EncounterColourPolicy.Resolve(config.NativeColours.Hostile, config.Palette));
        config.NativeColours.Hostile.Source = EncounterColourSource.Palette;
        Near(config.Palette.Danger.ToVector4(), EncounterColourPolicy.Resolve(config.NativeColours.Hostile, config.Palette));
        config.NativeColours.Hostile.Role = EncounterColourRole.Objective;
        Near(config.Palette.Objective.ToVector4(), EncounterColourPolicy.Resolve(config.NativeColours.Hostile, config.Palette));
        config.Palette.Enabled = false;
        Near(custom, EncounterColourPolicy.Resolve(config.NativeColours.Hostile, config.Palette));
        var marker = new PlayerPositionMarkerConfiguration();
        var previous = PlayerPositionMarkerPolicy.ResolveColour(marker, true);
        config.Palette.Enabled = true;
        Near(previous, PlayerPositionMarkerPolicy.ResolveColour(marker, true, config.Palette));
        marker.UseEncounterPalette = true;
        var expected = config.Palette.Danger.ToVector4(); expected.W = marker.Opacity;
        Near(expected, PlayerPositionMarkerPolicy.ResolveColour(marker, true, config.Palette));
        expected = config.Palette.Safe.ToVector4(); expected.W = marker.Opacity;
        Near(expected, PlayerPositionMarkerPolicy.ResolveColour(marker, false, config.Palette));
    }

    public static void Transition()
    {
        var config = new NativeOmenColourConfiguration { Enabled = true, CastWarningTransition = true, Opacity = 0.5f, Intensity = 1.5f };
        config.Hostile.Custom.Set(EncounterColourPolicy.Preset(4));
        var style = NativeOmenStyle.From(config, new EncounterPaletteConfiguration(), true);
        Check(style.CanRecolour(1) && !style.CanRecolour(0) && !style.CanRecolour(2));
        var start = style.Hostile; start = new Vector4(new Vector3(start.X, start.Y, start.Z) * 1.5f, 0.25f);
        Near(start, style.Resolve(true, 0f, 0.5f));
        var middle = style.Midpoint; middle = new Vector4(new Vector3(middle.X, middle.Y, middle.Z) * 1.5f, 0.25f);
        Near(middle, style.Resolve(true, 0.65f, 0.5f));
        var end = style.Warning; end = new Vector4(new Vector3(end.X, end.Y, end.Z) * 1.5f, 0.25f);
        Near(end, style.Resolve(true, 1f, 0.5f));
        Near(start, style.Resolve(true, null, 0.5f));
        Near(start, style.Resolve(true, float.NaN, 0.5f));
        config.RecolourFriendly = true;
        Check(NativeOmenStyle.From(config, new(), true).CanRecolour(0));
        Check(!NativeOmenStyle.From(config, new(), false).CanRecolour(1));
    }

    public static void Ownership()
    {
        var original = new Vector4(1f, 1f, 1f, 0.7f); var lease = new OmenTintLease(original);
        Check(!lease.Owns(original)); var applied = lease.Apply(new Vector4(1f, 0f, 0f, 0.35f));
        Check(lease.Owns(applied)); Near(original, lease.Original);
        Check(!lease.Owns(new Vector4(0f, 1f, 0f, 1f))); // A different writer keeps ownership.
        lease.CaptureOriginal(new Vector4(1f, 1f, 1f, 0.2f));
        Check(lease.Original.W == 0.2f);
    }

    public static void Lifetimes()
    {
        var lifetime = new OmenLifetimeRegistry<OmenTintLease>(2);
        var a = new OmenTintLease(Vector4.One); var b = new OmenTintLease(Vector4.One);
        Check(lifetime.RegisterPending(1, 123)); lifetime.Forget(123);
        Check(!lifetime.Activate(1, 123, a)); // Destroyed while the factory was still returning.
        Check(lifetime.RegisterPending(2, 123));
        Check(!lifetime.Activate(1, 123, a)); // Old generation cannot touch a reused address.
        Check(lifetime.Activate(2, 123, b) && ReferenceEquals(lifetime.Find(123), b));
        Check(lifetime.RegisterPending(3, 234)); Check(!lifetime.RegisterPending(4, 345));
        Check(lifetime.Count == 1); lifetime.Clear();
        Check(lifetime.Count == 0 && lifetime.Find(123) is null && !lifetime.IsPending(3, 234));
        Check(!lifetime.RegisterPending(5, 0));
    }

    public static void CastLinks()
    {
        OmenCastTiming[] casts = [new(5, 10, 100, 1000, 4f), new(6, 20, 200, 1000, 3f)];
        var match = OmenCastTiming.Unique(casts, 5);
        Check(match is { ActorId: 10 } found && Math.Abs(found.Progress(3000) - 0.5f) < 0.0001f);
        Check(OmenCastTiming.Unique(casts, 7) is null);
        Check(OmenCastTiming.Unique([casts[0], casts[0] with { ActorId = 30 }], 5) is null);
        Check(casts[0].Progress(900) == 0f && casts[0].Progress(10000) == 1f);
    }

    public static void Validation()
    {
        var config = new EncounterAwarenessConfiguration();
        config.Palette.Danger = null!; config.NativeColours.Hostile.Custom = null!;
        config.NativeColours.Opacity = float.NaN; config.NativeColours.Intensity = 100f;
        config.Overlay.Radius = float.PositiveInfinity; config.Overlay.Thickness = -100f;
        config.Overlay.TargetRing.Source = (EncounterColourSource)90; config.Overlay.TargetRing.Role = (EncounterColourRole)99;
        EncounterColourPolicy.Normalize(config);
        Check(config.Palette.Danger is not null && config.NativeColours.Hostile.Custom is not null);
        Check(config.NativeColours.Opacity == 1f && config.NativeColours.Intensity == 2f);
        Check(config.Overlay.Radius == 3f && config.Overlay.Thickness == 0.5f);
        Check(config.Overlay.TargetRing.Source == EncounterColourSource.Custom && config.Overlay.TargetRing.Role == EncounterColourRole.Target);
        Check(EncounterColourPolicy.Pack(new Vector4(1f, 0f, 0f, 1f)) == 0xFF0000FF);
        var request = new EncounterOverlayRequest("demo", EncounterOverlayShape.Circle, Vector3.Zero, Vector3.One,
            3f, 2f, Vector4.One, "Marker", 123, true);
        Check(EncounterOverlayPolicy.Valid(request));
        Check(!EncounterOverlayPolicy.Valid(request with { ActorId = 0xE0000000 }));
        Check(!EncounterOverlayPolicy.Valid(request with { Start = new Vector3(float.NaN) }));
        Check(!EncounterOverlayPolicy.Valid(request with { Thickness = 100f }));
        Check(!EncounterOverlayPolicy.Valid(request with { Label = new string('x', 121) }));
        Check(!EncounterOverlayPolicy.Valid(request with { Shape = EncounterOverlayShape.Line }));
        Check(EncounterOverlayPolicy.IsOwnedNamespace("SentinelHUD.EncounterOverlay.test.PlayerCircle"));
        Check(!EncounterOverlayPolicy.IsOwnedNamespace("OtherPlugin.Danger") && !EncounterOverlayPolicy.IsOwnedNamespace(null));
    }
}
