using System.Numerics;
using System.Text.Json;
using SentinelHUD.Core;

var tests = new (string Name, Action Run)[]
{
    ("configuration defaults", TestDefaults),
    ("configuration migration and repair", TestMigration),
    ("version-one settings survive migration", TestVersionOneMigration),
    ("version-two settings survive schema-four migration", TestVersionTwoMigration),
    ("version-three MP settings migrate to schema four", TestVersionThreeMigration),
    ("configuration serialization", TestSerialization),
    ("HP formatting", TestHitPointFormatting),
    ("compact number formatting", TestCompactNumberFormatting),
    ("percentage formatting", TestPercentageFormatting),
    ("distance formatting", TestDistanceFormatting),
    ("shield formatting", TestShieldFormatting),
    ("integrated shield bar layout", TestShieldBarLayout),
    ("layout position round trip", TestLayoutRoundTrip),
    ("layout boundary protection", TestLayoutBoundaries),
    ("edit chrome lock-unlock stability", TestEditChromeStability),
    ("module independence", TestModuleIndependence),
    ("module visibility conditions", TestModuleVisibilityConditions),
    ("self-highlight conditional modes", TestHighlightModes),
    ("self-highlight colours", TestHighlightColours),
    ("native self-highlight palette", TestNativeHighlightPalette),
    ("position-marker configuration", TestPositionMarkerConfiguration),
    ("HP colour modes", TestHpColourModes),
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
    Equal(SelfHighlightMode.Off, config.PlayerPositionMarker.Mode);
    Equal(HighlightColourPreset.White, config.PlayerPositionMarker.ColourPreset);
    Near(0.01f, PlayerPositionMarkerPolicy.MinimumRadius);
    Equal(ShieldDisplayMode.BarAndText, config.Player.ShieldDisplay);
    Equal(MpDisplayMode.BarAndText, config.Player.MpDisplay);
    Equal(MpDisplayMode.Off, config.Target.MpDisplay);
    Equal(MpDisplayMode.Off, config.FocusTarget.MpDisplay);
    Equal(HpColourMode.StaticRoleBased, config.Appearance.PlayerHpColourMode);
    Equal(HpColourMode.StaticRoleBased, config.Appearance.TargetHpColourMode);
    Equal(320f, config.Target.Width);
    Equal(20f, config.Target.BarHeight);
    Equal(new Vector4(0.88f, 0.20f, 0.18f, 1f), config.Appearance.HostileHealth.ToVector4());
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
    source.FocusTarget.Width = 410f;
    source.FocusTarget.BarHeight = 13f;
    source.FocusTarget.Visibility = ModuleVisibilityCondition.CombatOrDuty;
    source.FocusTarget.NumberFormat = HudNumberFormat.Compact;
    source.SelfHighlight.Mode = SelfHighlightMode.DutyOnly;
    source.SelfHighlight.ColourPreset = HighlightColourPreset.Custom;
    source.SelfHighlight.CustomColour.Set(new Vector4(0.1f, 0.2f, 0.3f, 0.9f));
    source.PlayerPositionMarker.Mode = SelfHighlightMode.Always;
    source.PlayerPositionMarker.Radius = 0.27f;
    source.PlayerPositionMarker.BorderThickness = 0.75f;
    source.PlayerPositionMarker.ColourPreset = HighlightColourPreset.Green;
    source.Camera.Enabled = true;
    source.Camera.MaximumZoomDistance = 42f;
    source.Target.ShieldDisplay = ShieldDisplayMode.BarOnly;
    source.Player.MpDisplay = MpDisplayMode.TextOnly;
    source.Target.MpDisplay = MpDisplayMode.BarAndText;
    source.FocusTarget.MpDisplay = MpDisplayMode.BarOnly;
    source.Target.NativeHpOverlay.Mode = NativeTargetOverlayMode.CombatOnly;
    source.Target.NativeHpOverlay.OffsetY = 17f;
    source.Appearance.HostileHealth.Set(new Vector4(0.7f, 0.1f, 0.2f, 1f));
    source.Appearance.Mp.Set(new Vector4(0.15f, 0.35f, 0.75f, 1f));
    source.Appearance.PlayerHpColourMode = HpColourMode.HealthStateGradient;
    source.Appearance.TargetHpColourMode = HpColourMode.HealthStateGradient;

    var json = JsonSerializer.Serialize(source);
    var restored = JsonSerializer.Deserialize<HudConfigurationData>(json);
    NotNull(restored);
    HudConfigurationMigrator.Normalize(restored!);
    False(restored!.Target.ShowDistance);
    Near(0.413f, restored.Target.Layout.AnchorX);
    Near(1.22f, restored.FocusTarget.Scale);
    Near(410f, restored.FocusTarget.Width);
    Near(13f, restored.FocusTarget.BarHeight);
    Equal(ModuleVisibilityCondition.CombatOrDuty, restored.FocusTarget.Visibility);
    Equal(HudNumberFormat.Compact, restored.FocusTarget.NumberFormat);
    Equal(SelfHighlightMode.DutyOnly, restored.SelfHighlight.Mode);
    Near(0.3f, restored.SelfHighlight.CustomColour.Blue);
    Equal(SelfHighlightMode.Always, restored.PlayerPositionMarker.Mode);
    True(restored.PlayerPositionMarker.Enabled);
    Near(0.27f, restored.PlayerPositionMarker.Radius);
    Near(0.75f, restored.PlayerPositionMarker.BorderThickness);
    Equal(HighlightColourPreset.Green, restored.PlayerPositionMarker.ColourPreset);
    True(restored.Camera.Enabled);
    Near(42f, restored.Camera.MaximumZoomDistance);
    Equal(ShieldDisplayMode.BarOnly, restored.Target.ShieldDisplay);
    Equal(MpDisplayMode.TextOnly, restored.Player.MpDisplay);
    Equal(MpDisplayMode.BarAndText, restored.Target.MpDisplay);
    Equal(MpDisplayMode.BarOnly, restored.FocusTarget.MpDisplay);
    Equal(NativeTargetOverlayMode.CombatOnly, restored.Target.NativeHpOverlay.Mode);
    Near(17f, restored.Target.NativeHpOverlay.OffsetY);
    Near(0.7f, restored.Appearance.HostileHealth.Red);
    Near(0.75f, restored.Appearance.Mp.Blue);
    Equal(HpColourMode.HealthStateGradient, restored.Appearance.PlayerHpColourMode);
    Equal(HpColourMode.HealthStateGradient, restored.Appearance.TargetHpColourMode);
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

static void TestVersionTwoMigration()
{
    var source = new HudConfigurationData
    {
        Version = 2,
        PlayerPositionMarker = new PlayerPositionMarkerConfiguration
        {
            Enabled = true,
            Radius = 0.04f,
            ColourPreset = HighlightColourPreset.Blue,
        },
    };
    source.Player.Layout.AnchorX = 0.37f;
    source.Target.Scale = 1.14f;
    source.Target.ShowShield = false;
    source.FocusTarget.ShowShield = true;

    HudConfigurationMigrator.Normalize(source);

    Equal(HudConfigurationData.CurrentVersion, source.Version);
    Equal(SelfHighlightMode.Always, source.PlayerPositionMarker.Mode);
    True(source.PlayerPositionMarker.Enabled);
    Near(0.04f, source.PlayerPositionMarker.Radius);
    Equal(HighlightColourPreset.Blue, source.PlayerPositionMarker.ColourPreset);
    Near(0.37f, source.Player.Layout.AnchorX);
    Near(1.14f, source.Target.Scale);
    Equal(320f, source.Target.Width);
    Equal(ShieldDisplayMode.Off, source.Target.ShieldDisplay);
    Equal(ShieldDisplayMode.BarAndText, source.FocusTarget.ShieldDisplay);
}

static void TestVersionThreeMigration()
{
    var source = new HudConfigurationData
    {
        Version = 3,
    };
    source.Player.ShowMp = false;
    source.Player.MpDisplay = MpDisplayMode.BarAndText;
    source.Player.Layout.AnchorX = 0.42f;
    source.Player.Layout.AnchorY = 0.61f;

    HudConfigurationMigrator.Normalize(source);

    Equal(HudConfigurationData.CurrentVersion, source.Version);
    Equal(MpDisplayMode.Off, source.Player.MpDisplay);
    False(source.Player.ShowMp);
    Equal(MpDisplayMode.Off, source.Target.MpDisplay);
    Equal(MpDisplayMode.Off, source.FocusTarget.MpDisplay);
    Near(0.42f, source.Player.Layout.AnchorX);
    Near(0.61f, source.Player.Layout.AnchorY);
}

static void TestHitPointFormatting()
{
    Equal("7,428,113 / 12,500,000 — 59.4%", HudFormatting.HitPoints(7_428_113, 12_500_000, true, true, true));
    Equal("7,428,113", HudFormatting.HitPoints(7_428_113, 12_500_000, true, false, false));
    Equal("Max 12,500,000", HudFormatting.HitPoints(7_428_113, 12_500_000, false, true, false));
    Equal("59.4%", HudFormatting.HitPoints(7_428_113, 12_500_000, false, false, true));
    Equal(string.Empty, HudFormatting.HitPoints(1, 2, false, false, false));
}

static void TestCompactNumberFormatting()
{
    Equal("295.9k / 295.9k", HudFormatting.HitPoints(
        295_921, 295_921, true, true, false, HudNumberFormat.Compact));
    Equal("12.48m / 18.7m", HudFormatting.HitPoints(
        12_480_000, 18_700_000, true, true, false, HudNumberFormat.Compact));
    Equal("73.7%", HudFormatting.NativeTargetHitPoints(
        112_878, 153_100, NativeTargetHpFormat.Percentage, HudNumberFormat.Full));
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

static void TestShieldBarLayout()
{
    var full = ShieldBarPolicy.Calculate(1f, 0.12f);
    Near(1f, full.HealthEnd);
    Near(0.88f, full.ShieldOverlayStart);
    Near(1f, full.ShieldExtensionEnd);
    True(full.HasOverlay);
    False(full.HasExtension);

    var belowFull = ShieldBarPolicy.Calculate(0.60f, 0.20f);
    Near(0.60f, belowFull.HealthEnd);
    Near(0.60f, belowFull.ShieldOverlayStart);
    Near(0.80f, belowFull.ShieldExtensionEnd);
    False(belowFull.HasOverlay);
    True(belowFull.HasExtension);

    var mixed = ShieldBarPolicy.Calculate(0.90f, 0.20f);
    Near(0.80f, mixed.ShieldOverlayStart);
    Near(1f, mixed.ShieldExtensionEnd);
    True(mixed.HasOverlay);
    True(mixed.HasExtension);
}

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

static void TestEditChromeStability()
{
    var layout = new ModuleLayoutConfiguration { AnchorX = 0.23f, AnchorY = 0.71f };
    var workPosition = new Vector2(80f, 40f);
    var workSize = new Vector2(1920f, 1080f);
    var contentSize = new Vector2(320f, 128f);
    const float chromeHeight = 27f;

    for (var iteration = 0; iteration < 100; iteration++)
    {
        var expectedContentPosition = LayoutPolicy.ToPixelPosition(
            layout, workPosition, workSize, contentSize);

        var unlockedOuter = LayoutPolicy.ToOuterWindowPosition(expectedContentPosition, chromeHeight);
        var unlockedContent = LayoutPolicy.ToContentPosition(unlockedOuter, chromeHeight);
        var unlockedContentSize = LayoutPolicy.ToContentSize(
            contentSize + new Vector2(0f, chromeHeight), chromeHeight);
        layout = LayoutPolicy.ToNormalizedPosition(
            unlockedContent, workPosition, workSize, unlockedContentSize);

        var lockedOuter = LayoutPolicy.ToOuterWindowPosition(expectedContentPosition, 0f);
        var lockedContent = LayoutPolicy.ToContentPosition(lockedOuter, 0f);
        layout = LayoutPolicy.ToNormalizedPosition(
            lockedContent, workPosition, workSize, contentSize);

        Near(0.23f, layout.AnchorX);
        Near(0.71f, layout.AnchorY);
        Equal(expectedContentPosition, unlockedContent);
        Equal(expectedContentPosition, lockedContent);
        Equal(contentSize, unlockedContentSize);
    }
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

static void TestModuleVisibilityConditions()
{
    True(HudVisibilityPolicy.ShouldShowModule(
        ModuleVisibilityCondition.Always, true, false, false));
    False(HudVisibilityPolicy.ShouldShowModule(
        ModuleVisibilityCondition.Always, false, true, true));
    True(HudVisibilityPolicy.ShouldShowModule(
        ModuleVisibilityCondition.CombatOnly, true, true, false));
    True(HudVisibilityPolicy.ShouldShowModule(
        ModuleVisibilityCondition.DutyOnly, true, false, true));
    True(HudVisibilityPolicy.ShouldShowModule(
        ModuleVisibilityCondition.CombatOrDuty, true, true, false));
    True(HudVisibilityPolicy.ShouldShowModule(
        ModuleVisibilityCondition.CombatOrDuty, true, false, true));
    False(HudVisibilityPolicy.ShouldShowModule(
        ModuleVisibilityCondition.CombatOrDuty, true, false, false));
    True(HudVisibilityPolicy.ShouldShowNativeTargetOverlay(
        NativeTargetOverlayMode.CombatOnly, true, true));
    False(HudVisibilityPolicy.ShouldShowNativeTargetOverlay(
        NativeTargetOverlayMode.CombatOnly, true, false));
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

static void TestHpColourModes()
{
    var staticColour = new Vector4(0.88f, 0.20f, 0.18f, 0.73f);
    Equal(staticColour, HpColourPolicy.Resolve(
        HpColourMode.StaticRoleBased, 0.95f, staticColour));
    Equal(HpColourPolicy.HighHealth, HpColourPolicy.Resolve(
        HpColourMode.HealthStateGradient, 1f, staticColour));
    Equal(HpColourPolicy.HighHealth, HpColourPolicy.Resolve(
        HpColourMode.HealthStateGradient, 0.80f, staticColour));
    Equal(HpColourPolicy.MidHealth, HpColourPolicy.Resolve(
        HpColourMode.HealthStateGradient, 0.55f, staticColour));
    Equal(HpColourPolicy.LowHealth, HpColourPolicy.Resolve(
        HpColourMode.HealthStateGradient, 0.35f, staticColour));
    Equal(HpColourPolicy.LowHealth, HpColourPolicy.Resolve(
        HpColourMode.HealthStateGradient, float.NaN, staticColour));

    var transitioning = HpColourPolicy.Resolve(
        HpColourMode.HealthStateGradient, 0.675f, staticColour);
    Equal(Vector4.Lerp(HpColourPolicy.MidHealth, HpColourPolicy.HighHealth, 0.5f), transitioning);
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
