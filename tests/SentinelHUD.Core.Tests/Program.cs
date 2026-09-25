using System.Numerics;
using System.Text.Json;
using SentinelHUD.Core;

var tests = new (string Name, Action Run)[]
{
    ("configuration defaults", TestDefaults),
    ("configuration migration and repair", TestMigration),
    ("version-one settings survive migration", TestVersionOneMigration),
    ("configuration serialization", TestSerialization),
    ("HP formatting", TestHitPointFormatting),
    ("percentage formatting", TestPercentageFormatting),
    ("distance formatting", TestDistanceFormatting),
    ("shield formatting", TestShieldFormatting),
    ("layout position round trip", TestLayoutRoundTrip),
    ("layout boundary protection", TestLayoutBoundaries),
    ("module independence", TestModuleIndependence),
    ("self-highlight conditional modes", TestHighlightModes),
    ("self-highlight colours", TestHighlightColours),
    ("native self-highlight palette", TestNativeHighlightPalette),
    ("position-marker configuration", TestPositionMarkerConfiguration),
    ("camera zoom policy", TestCameraZoomPolicy),
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"FAIL: {test.Name}: {exception.Message}");
    }
}

foreach (var failure in failures)
    Console.Error.WriteLine(failure);

Console.WriteLine($"{tests.Length - failures.Count}/{tests.Length} test groups passed.");
return failures.Count == 0 ? 0 : 1;

static void TestDefaults()
{
    var config = HudConfigurationMigrator.Normalize(new HudConfigurationData());
    Equal(HudConfigurationData.CurrentVersion, config.Version);
    True(config.Enabled);
    True(config.Locked);
    True(config.Player.Enabled);
    True(config.Target.ShowDistance);
    True(config.FocusTarget.ShowCastBar);
    True(config.TargetOfTarget.Enabled);
    Equal(SelfHighlightMode.Off, config.SelfHighlight.Mode);
    Equal(HighlightColourPreset.Yellow, config.SelfHighlight.ColourPreset);
    False(config.PlayerPositionMarker.Enabled);
    Equal(HighlightColourPreset.White, config.PlayerPositionMarker.ColourPreset);
    False(config.Camera.Enabled);
    Equal(CameraZoomPolicy.DefaultExtendedMaximum, config.Camera.MaximumZoomDistance);
}

static void TestMigration()
{
    var config = new HudConfigurationData
    {
        Version = 0,
        GlobalScale = float.NaN,
        GlobalOpacity = 5f,
        Player = null!,
        Target = new TargetModuleConfiguration
        {
            Scale = 9f,
            Opacity = -2f,
            Layout = new ModuleLayoutConfiguration { AnchorX = 4f, AnchorY = float.PositiveInfinity },
        },
        SelfHighlight = new SelfHighlightConfiguration
        {
            Intensity = -1f,
            CustomColour = null!,
        },
        PlayerPositionMarker = new PlayerPositionMarkerConfiguration
        {
            Radius = float.PositiveInfinity,
            Opacity = -4f,
            CustomColour = null!,
        },
        Camera = new ExtendedCameraZoomConfiguration
        {
            MaximumZoomDistance = 500f,
        },
    };

    HudConfigurationMigrator.Normalize(config);
    Equal(HudConfigurationData.CurrentVersion, config.Version);
    NotNull(config.Player);
    Equal(1f, config.GlobalScale);
    Equal(1f, config.GlobalOpacity);
    Equal(1.8f, config.Target.Scale);
    Equal(0.15f, config.Target.Opacity);
    Equal(1f, config.Target.Layout.AnchorX);
    Equal(HudConfigurationDefaults.CreateTarget().Layout.AnchorY, config.Target.Layout.AnchorY);
    Equal(0.15f, config.SelfHighlight.Intensity);
    NotNull(config.SelfHighlight.CustomColour);
    Equal(PlayerPositionMarkerPolicy.DefaultRadius, config.PlayerPositionMarker.Radius);
    Equal(0.1f, config.PlayerPositionMarker.Opacity);
    NotNull(config.PlayerPositionMarker.CustomColour);
    Equal(CameraZoomPolicy.MaximumSupported, config.Camera.MaximumZoomDistance);
}

static void TestSerialization()
{
    var source = new HudConfigurationData();
    source.Target.ShowDistance = false;
    source.Target.Layout.AnchorX = 0.413f;
    source.FocusTarget.Scale = 1.22f;
    source.SelfHighlight.Mode = SelfHighlightMode.DutyOnly;
    source.SelfHighlight.ColourPreset = HighlightColourPreset.Custom;
    source.SelfHighlight.CustomColour.Set(new Vector4(0.1f, 0.2f, 0.3f, 0.9f));
    source.PlayerPositionMarker.Enabled = true;
    source.PlayerPositionMarker.Radius = 0.27f;
    source.PlayerPositionMarker.ColourPreset = HighlightColourPreset.Green;
    source.Camera.Enabled = true;
    source.Camera.MaximumZoomDistance = 42f;

    var json = JsonSerializer.Serialize(source);
    var restored = JsonSerializer.Deserialize<HudConfigurationData>(json);
    NotNull(restored);
    HudConfigurationMigrator.Normalize(restored!);
    False(restored!.Target.ShowDistance);
    Near(0.413f, restored.Target.Layout.AnchorX);
    Near(1.22f, restored.FocusTarget.Scale);
    Equal(SelfHighlightMode.DutyOnly, restored.SelfHighlight.Mode);
    Near(0.3f, restored.SelfHighlight.CustomColour.Blue);
    True(restored.PlayerPositionMarker.Enabled);
    Near(0.27f, restored.PlayerPositionMarker.Radius);
    Equal(HighlightColourPreset.Green, restored.PlayerPositionMarker.ColourPreset);
    True(restored.Camera.Enabled);
    Near(42f, restored.Camera.MaximumZoomDistance);
}

static void TestVersionOneMigration()
{
    var source = new HudConfigurationData
    {
        Version = 1,
        Locked = false,
    };
    source.Player.Layout.AnchorX = 0.31f;
    source.Player.Layout.AnchorY = 0.67f;
    source.Target.ShowDistance = false;
    source.FocusTarget.Scale = 1.31f;

    HudConfigurationMigrator.Normalize(source);

    Equal(HudConfigurationData.CurrentVersion, source.Version);
    False(source.Locked);
    Near(0.31f, source.Player.Layout.AnchorX);
    Near(0.67f, source.Player.Layout.AnchorY);
    False(source.Target.ShowDistance);
    Near(1.31f, source.FocusTarget.Scale);
    NotNull(source.PlayerPositionMarker);
    NotNull(source.Camera);
}

static void TestHitPointFormatting()
{
    Equal("7,428,113 / 12,500,000 — 59.4%", HudFormatting.HitPoints(7_428_113, 12_500_000, true, true, true));
    Equal("7,428,113", HudFormatting.HitPoints(7_428_113, 12_500_000, true, false, false));
    Equal("Max 12,500,000", HudFormatting.HitPoints(7_428_113, 12_500_000, false, true, false));
    Equal("59.4%", HudFormatting.HitPoints(7_428_113, 12_500_000, false, false, true));
    Equal(string.Empty, HudFormatting.HitPoints(1, 2, false, false, false));
}

static void TestPercentageFormatting()
{
    Equal("50.0%", HudFormatting.Percentage(50, 100));
    Equal("--", HudFormatting.Percentage(0, 0));
    Equal("100.0%", HudFormatting.CastPercentage(2f, 2f));
    Equal("--", HudFormatting.CastPercentage(2f, 0f));
}

static void TestDistanceFormatting()
{
    Equal("18.7 yalms", HudFormatting.Distance(18.65f));
    Equal("0.0 yalms", HudFormatting.Distance(-4f));
    Equal("--", HudFormatting.Distance(float.NaN));
}

static void TestShieldFormatting()
    => Equal("2,500 (20%)", HudFormatting.Shield(12_500, 20));

static void TestLayoutRoundTrip()
{
    var source = new ModuleLayoutConfiguration { AnchorX = 0.63f, AnchorY = 0.41f };
    var workPosition = new Vector2(100f, 50f);
    var workSize = new Vector2(1800f, 1000f);
    var windowSize = new Vector2(320f, 140f);
    var pixel = LayoutPolicy.ToPixelPosition(source, workPosition, workSize, windowSize);
    var restored = LayoutPolicy.ToNormalizedPosition(pixel, workPosition, workSize, windowSize);
    Near(source.AnchorX, restored.AnchorX);
    Near(source.AnchorY, restored.AnchorY);
}

static void TestLayoutBoundaries()
{
    var clamped = LayoutPolicy.KeepReachable(
        new Vector2(4000f, -800f),
        new Vector2(100f, 50f),
        new Vector2(1800f, 1000f),
        new Vector2(320f, 140f));
    Equal(new Vector2(1580f, 50f), clamped);

    var oversized = LayoutPolicy.KeepReachable(
        new Vector2(900f, 600f),
        new Vector2(100f, 50f),
        new Vector2(800f, 600f),
        new Vector2(1200f, 900f));
    Equal(new Vector2(100f, 50f), oversized);
}

static void TestModuleIndependence()
{
    var config = new HudConfigurationData();
    config.Target.ShowDistance = false;
    config.FocusTarget.ShowDistance = true;
    config.Target.Enabled = false;
    True(config.Player.Enabled);
    False(config.Target.Enabled);
    False(config.Target.ShowDistance);
    True(config.FocusTarget.ShowDistance);
}

static void TestHighlightModes()
{
    False(SelfHighlightPolicy.ShouldRender(SelfHighlightMode.Off, true, true, true));
    True(SelfHighlightPolicy.ShouldRender(SelfHighlightMode.Always, true, false, false));
    False(SelfHighlightPolicy.ShouldRender(SelfHighlightMode.Always, false, true, true));
    True(SelfHighlightPolicy.ShouldRender(SelfHighlightMode.CombatOnly, true, true, false));
    False(SelfHighlightPolicy.ShouldRender(SelfHighlightMode.CombatOnly, true, false, true));
    True(SelfHighlightPolicy.ShouldRender(SelfHighlightMode.DutyOnly, true, false, true));
    False(SelfHighlightPolicy.ShouldRender(SelfHighlightMode.DutyOnly, true, true, false));
}

static void TestHighlightColours()
{
    var config = new SelfHighlightConfiguration
    {
        ColourPreset = HighlightColourPreset.Blue,
        Intensity = 0.7f,
    };
    var blue = SelfHighlightPolicy.ResolveColour(config);
    Near(0.2f, blue.X);
    Near(0.72f, blue.Y);
    Near(1f, blue.Z);
    Near(0.7f, blue.W);

    config.ColourPreset = HighlightColourPreset.Custom;
    config.CustomColour.Set(new Vector4(0.11f, 0.22f, 0.33f, 0.44f));
    var custom = SelfHighlightPolicy.ResolveColour(config);
    Near(0.11f, custom.X);
    Near(0.22f, custom.Y);
    Near(0.33f, custom.Z);
    Near(0.7f, custom.W);
}

static void TestNativeHighlightPalette()
{
    var config = new SelfHighlightConfiguration
    {
        ColourPreset = HighlightColourPreset.Green,
    };
    var exact = NativeHighlightPolicy.Resolve(config);
    Equal(NativeHighlightColour.Green, exact.Colour);
    True(exact.IsExact);

    config.ColourPreset = HighlightColourPreset.White;
    var white = NativeHighlightPolicy.Resolve(config);
    False(white.IsExact);
    Equal(NativeHighlightColour.Yellow, white.Colour);

    config.ColourPreset = HighlightColourPreset.Custom;
    config.CustomColour.Set(new Vector4(0.98f, 0.22f, 0.75f, 1f));
    var custom = NativeHighlightPolicy.Resolve(config);
    False(custom.IsExact);
    Equal(NativeHighlightColour.Magenta, custom.Colour);
}

static void TestPositionMarkerConfiguration()
{
    var config = new PlayerPositionMarkerConfiguration
    {
        ColourPreset = HighlightColourPreset.Blue,
        Opacity = 0.64f,
    };
    var colour = PlayerPositionMarkerPolicy.ResolveColour(config);
    Near(0.20f, colour.X);
    Near(0.72f, colour.Y);
    Near(1f, colour.Z);
    Near(0.64f, colour.W);
}

static void TestCameraZoomPolicy()
{
    Near(CameraZoomPolicy.StockMaximum, CameraZoomPolicy.NormalizeMaximum(3f));
    Near(42f, CameraZoomPolicy.NormalizeMaximum(42f));
    Near(CameraZoomPolicy.MaximumSupported, CameraZoomPolicy.NormalizeMaximum(500f));
    Near(CameraZoomPolicy.DefaultExtendedMaximum, CameraZoomPolicy.NormalizeMaximum(float.NaN));
}

static void True(bool value)
{
    if (!value)
        throw new InvalidOperationException("Expected true.");
}

static void False(bool value) => True(!value);

static void NotNull(object? value)
{
    if (value is null)
        throw new InvalidOperationException("Expected a non-null value.");
}

static void Near(float expected, float actual)
{
    if (MathF.Abs(expected - actual) > 0.0001f)
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
}
