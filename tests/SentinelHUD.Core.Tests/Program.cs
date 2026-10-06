using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using SentinelHUD.Core;

var tests = new (string Name, Action Run)[]
{
    ("configuration defaults", TestDefaults),
    ("configuration migration and repair", TestMigration),
    ("version-one settings survive migration", TestVersionOneMigration),
    ("version-two settings survive current migration", TestVersionTwoMigration),
    ("version-three MP settings survive current migration", TestVersionThreeMigration),
    ("version-four settings survive current migration", TestVersionFourMigration),
    ("version-five update preserves customized settings", TestVersionFiveUpgradePersistence),
    ("version-six update preserves customized settings", TestVersionSixUpgradePersistence),
    ("version-seven marker style migration", TestVersionSevenMarkerStyleMigration),
    ("version-eight quest convenience migration", TestVersionEightQuestConvenienceMigration),
    ("version-nine targeting-me migration", TestVersionNineTargetingMeMigration),
    ("version-ten modern-theme migration", TestVersionTenModernThemeMigration),
    ("modern navigation state", TestModernNavigationState),
    ("Sentinel ecosystem registry and summary", TestSentinelEcosystemRegistry),
    ("modern minimum window geometry", TestModernMinimumWindowGeometry),
    ("configuration serialization", TestSerialization),
    ("HP formatting", TestHitPointFormatting),
    ("compact number formatting", TestCompactNumberFormatting),
    ("percentage formatting", TestPercentageFormatting),
    ("remaining cast-time formatting", TestRemainingCastTimeFormatting),
    ("distance formatting", TestDistanceFormatting),
    ("shield formatting", TestShieldFormatting),
    ("integrated shield bar layout", TestShieldBarLayout),
    ("layout position round trip", TestLayoutRoundTrip),
    ("layout boundary protection", TestLayoutBoundaries),
    ("horizontal editor resize", TestHorizontalEditorResize),
    ("two-axis editor resize", TestTwoAxisEditorResize),
    ("edit chrome lock-unlock stability", TestEditChromeStability),
    ("native target anchor bounds", TestNativeTargetAnchorBounds),
    ("native context-menu input suspension", TestContextMenuSuppression),
    ("native context-menu adjacent placement", TestContextMenuPlacement),
    ("module independence", TestModuleIndependence),
    ("module visibility conditions", TestModuleVisibilityConditions),
    ("self-highlight conditional modes", TestHighlightModes),
    ("self-highlight colours", TestHighlightColours),
    ("native self-highlight palette", TestNativeHighlightPalette),
    ("position-marker configuration", TestPositionMarkerConfiguration),
    ("danger geometry containment", TestDangerGeometryContainment),
    ("HP colour modes", TestHpColourModes),
    ("camera zoom policy", TestCameraZoomPolicy),
    ("camera zoom death persistence", TestCameraZoomDeathPersistence),
    ("quest reward selection policy", TestQuestRewardSelectionPolicy),
    ("cutscene skip confirmation policy", TestCutsceneSkipConfirmationPolicy),
    ("PvP threat classification and warning policy", TestPvPThreatPolicy),
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
    True(config.Player.ClickToTarget);
    True(config.Player.RightClickContextMenu);
    Equal(ModuleClickableArea.WholeModule, config.Player.ClickableArea);
    True(config.Target.ShowDistance);
    True(config.FocusTarget.ShowCastBar);
    True(config.Player.ShowCastName);
    True(config.Player.ShowCastBar);
    True(config.Player.ShowCastPercentage);
    False(config.Player.ShowCastRemainingTime);
    True(config.FocusTarget.TargetOfFocus.Show);
    True(config.FocusTarget.TargetOfFocus.ClickToTarget);
    True(config.TargetOfTarget.Enabled);
    Equal(SelfHighlightMode.Off, config.SelfHighlight.Mode);
    Equal(HighlightColourPreset.Yellow, config.SelfHighlight.ColourPreset);
    False(config.PlayerPositionMarker.Enabled);
    Equal(SelfHighlightMode.Off, config.PlayerPositionMarker.Mode);
    Equal(PlayerPositionMarkerStyle.CameraFacing, config.PlayerPositionMarker.Style);
    Equal(HighlightColourPreset.White, config.PlayerPositionMarker.ColourPreset);
    True(config.PlayerPositionMarker.DangerDetectionEnabled);
    Equal(DangerColourPreset.Red, config.PlayerPositionMarker.DangerColourPreset);
    False(config.EncounterAwareness.Enabled);
    True(config.EncounterAwareness.NativeDetectionEnabled);
    False(config.EncounterAwareness.SplatoonIntegrationEnabled);
    False(config.Convenience.PreventAfkDisconnect);
    False(config.Convenience.SkipDialogue);
    False(config.Convenience.SkipCutscenes);
    Equal(QuestRewardSelectionMode.Manual, config.Convenience.QuestRewardSelection);
    True(config.TargetingMeCounter.Enabled);
    True(config.TargetingMeCounter.HideWhenZero);
    False(config.TargetingMeCounter.ShowJobs);
    False(config.TargetingMeCounter.ShowTargeterDetails);
    Equal(PvPThreatVisibility.FrontlineOnly, config.TargetingMeCounter.PvPVisibility);
    False(config.TargetingMeCounter.BorderEnabled);
    Near(64f, config.TargetingMeCounter.NumberSize);
    Near(0.01f, PlayerPositionMarkerPolicy.MinimumRadius);
    Equal(ShieldDisplayMode.BarAndText, config.Player.ShieldDisplay);
    Equal(MpDisplayMode.BarAndText, config.Player.MpDisplay);
    Equal(MpDisplayMode.Off, config.Target.MpDisplay);
    Equal(MpDisplayMode.Off, config.FocusTarget.MpDisplay);
    Equal(HpColourMode.StaticRoleBased, config.Appearance.PlayerHpColourMode);
    Equal(HpColourMode.StaticRoleBased, config.Appearance.TargetHpColourMode);
    Equal(ConfigurationWindowTheme.SentinelModern, config.Appearance.ConfigurationTheme);
    Equal(320f, config.Target.Width);
    Equal(20f, config.Target.BarHeight);
    Equal(new Vector4(0.88f, 0.20f, 0.18f, 1f), config.Appearance.HostileHealth.ToVector4());
    False(config.Camera.Enabled);
    Equal(CameraZoomPolicy.DefaultExtendedMaximum, config.Camera.MaximumZoomDistance);
}

static void TestModernNavigationState()
{
    var state = new SentinelHudModernNavigationState();
    Equal(SentinelHudModernPrimaryPage.General, state.PrimaryPage);
    Equal(SentinelHudModernNavigationState.GeneralId, state.PrimaryPageId);
    Equal(SentinelHudModernContentPage.General, state.ContentPage);
    False(state.HasSecondaryNavigation);

    True(state.SelectPrimary(SentinelHudModernNavigationState.HudId));
    Equal(SentinelHudModernContentPage.Player, state.ContentPage);
    True(state.HasSecondaryNavigation);
    True(state.SelectContent(SentinelHudModernContentPage.Target));
    Equal(SentinelHudModernContentPage.Target, state.ContentPage);

    True(state.SelectPrimary(SentinelHudModernNavigationState.AwarenessId));
    True(state.SelectContent(SentinelHudModernContentPage.EncounterAwareness));
    Equal(SentinelHudModernContentPage.EncounterAwareness, state.ContentPage);

    True(state.SelectPrimary(SentinelHudModernNavigationState.SystemsId));
    True(state.SelectContent(SentinelHudModernContentPage.Convenience));
    Equal(SentinelHudModernContentPage.Convenience, state.ContentPage);

    True(state.SelectPrimary(SentinelHudModernNavigationState.HudId));
    Equal(SentinelHudModernContentPage.Target, state.ContentPage);
    False(state.SelectPrimary(SentinelHudModernNavigationState.HudId));
    False(state.SelectContent(SentinelHudModernContentPage.Target));
    True(state.SelectPrimary(SentinelHudModernNavigationState.PluginsId));
    Equal(SentinelHudModernPrimaryPage.Plugins, state.PrimaryPage);
    Equal(SentinelHudModernContentPage.Plugins, state.ContentPage);
    False(state.HasSecondaryNavigation);
    Throws<ArgumentOutOfRangeException>(() => state.SelectPrimary("unknown"));
    Throws<ArgumentOutOfRangeException>(() => state.SelectContent(SentinelHudModernContentPage.General));
}

static void TestSentinelEcosystemRegistry()
{
    Equal(5, SentinelEcosystemRegistry.Plugins.Count);
    True(SentinelEcosystemRegistry.Plugins.Any(plugin => plugin.InternalName == "SRankSentinel"));
    True(SentinelEcosystemRegistry.Plugins.Any(plugin => plugin.InternalName == "PvPSentinel"));
    True(SentinelEcosystemRegistry.Plugins.All(plugin => plugin.InternalName != "SentinelHUD"));
    True(SentinelEcosystemRegistry.Plugins.All(plugin => plugin.InternalName != "SentinelCore"));

    var statuses = SentinelEcosystemRegistry.Resolve(
    [
        new SentinelPluginObservation("SRankSentinel", true, "0.7.49.0"),
        new SentinelPluginObservation("pvpsentinel", false, "0.3.2.0"),
        new SentinelPluginObservation("UnrelatedPlugin", true, "1.0.0"),
    ]);
    Equal(SentinelCompanionState.Enabled, statuses.Single(status => status.Definition.InternalName == "SRankSentinel").State);
    Equal("0.7.49.0", statuses.Single(status => status.Definition.InternalName == "SRankSentinel").Version);
    Equal(SentinelCompanionState.InstalledDisabled, statuses.Single(status => status.Definition.InternalName == "PvPSentinel").State);
    Equal(SentinelCompanionState.NotInstalled, statuses.Single(status => status.Definition.InternalName == "ClassySentinel").State);

    var summary = SentinelEcosystemRegistry.Summarize(statuses);
    Equal(5, summary.Total);
    Equal(2, summary.Installed);
    Equal(1, summary.Enabled);
    Equal(1, summary.Disabled);
    Equal("1 Sentinel plugin enabled · 1 installed but disabled · 3 not installed", summary.Description);

    var allEnabled = SentinelEcosystemRegistry.Resolve(
        SentinelEcosystemRegistry.Plugins.Select(plugin =>
            new SentinelPluginObservation(plugin.InternalName, true, "1.0.0")));
    Equal(
        "Your Sentinel ecosystem is ready. All known companion plugins are enabled.",
        SentinelEcosystemRegistry.Summarize(allEnabled).Description);
}

static void TestModernMinimumWindowGeometry()
{
    Equal(
        new Vector2(720f, 560f),
        SentinelHudModernWindowPolicy.MinimumSize(1f, new Vector2(580f, 408f)));
    Equal(
        new Vector2(900f, 700f),
        SentinelHudModernWindowPolicy.MinimumSize(1.25f, new Vector2(725f, 510f)));
    Equal(
        new Vector2(1080f, 840f),
        SentinelHudModernWindowPolicy.MinimumSize(1.5f, new Vector2(870f, 612f)));
    Equal(
        new Vector2(1200f, 800f),
        SentinelHudModernWindowPolicy.MinimumSize(1f, new Vector2(1200f, 800f)));
    Throws<ArgumentOutOfRangeException>(() =>
        SentinelHudModernWindowPolicy.MinimumSize(0f, new Vector2(580f, 408f)));
    Throws<ArgumentOutOfRangeException>(() =>
        SentinelHudModernWindowPolicy.MinimumSize(1f, new Vector2(float.NaN, 408f)));
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
    source.PlayerPositionMarker.Style = PlayerPositionMarkerStyle.GroundProjected;
    source.PlayerPositionMarker.Radius = 0.27f;
    source.PlayerPositionMarker.BorderThickness = 0.75f;
    source.PlayerPositionMarker.ColourPreset = HighlightColourPreset.Green;
    source.PlayerPositionMarker.DangerDetectionEnabled = false;
    source.PlayerPositionMarker.DangerColourPreset = DangerColourPreset.Custom;
    source.PlayerPositionMarker.CustomDangerColour.Set(new Vector4(0.7f, 0.2f, 0.9f, 1f));
    source.EncounterAwareness.Enabled = true;
    source.EncounterAwareness.NativeDetectionEnabled = false;
    source.EncounterAwareness.SplatoonIntegrationEnabled = true;
    source.EncounterAwareness.TreatUnclassifiedSplatoonGeometryAsDanger = true;
    source.Convenience.PreventAfkDisconnect = true;
    source.Convenience.SkipDialogue = true;
    source.Convenience.SkipCutscenes = true;
    source.Convenience.QuestRewardSelection = QuestRewardSelectionMode.CurrentJobReward;
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
    source.Player.ShowCastRemainingTime = true;
    source.FocusTarget.TargetOfFocus.ShowHpPercentage = false;
    source.FocusTarget.TargetOfFocus.ClickToTarget = false;
    source.FocusTarget.TargetOfFocus.RightClickContextMenu = false;
    source.Player.ClickToTarget = false;
    source.Player.RightClickContextMenu = false;
    source.Target.ClickableArea = ModuleClickableArea.HeaderOrName;
    source.TargetingMeCounter.PvPVisibility = PvPThreatVisibility.AllPvPDuties;
    source.TargetingMeCounter.HideWhenZero = false;
    source.TargetingMeCounter.ShowJobs = true;
    source.TargetingMeCounter.ShowTargeterDetails = true;
    source.TargetingMeCounter.Scale = 1.31f;
    source.TargetingMeCounter.Opacity = 0.63f;
    source.TargetingMeCounter.Width = 377f;
    source.TargetingMeCounter.NumberSize = 91f;
    source.TargetingMeCounter.Layout.AnchorX = 0.284f;
    source.TargetingMeCounter.HighColour.Set(new Vector4(0.7f, 0.2f, 0.8f, 1f));

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
    Equal(PlayerPositionMarkerStyle.GroundProjected, restored.PlayerPositionMarker.Style);
    True(restored.PlayerPositionMarker.Enabled);
    Near(0.27f, restored.PlayerPositionMarker.Radius);
    Near(0.75f, restored.PlayerPositionMarker.BorderThickness);
    Equal(HighlightColourPreset.Green, restored.PlayerPositionMarker.ColourPreset);
    False(restored.PlayerPositionMarker.DangerDetectionEnabled);
    Equal(DangerColourPreset.Custom, restored.PlayerPositionMarker.DangerColourPreset);
    Near(0.9f, restored.PlayerPositionMarker.CustomDangerColour.Blue);
    True(restored.EncounterAwareness.Enabled);
    False(restored.EncounterAwareness.NativeDetectionEnabled);
    True(restored.EncounterAwareness.SplatoonIntegrationEnabled);
    True(restored.EncounterAwareness.TreatUnclassifiedSplatoonGeometryAsDanger);
    True(restored.Convenience.PreventAfkDisconnect);
    True(restored.Convenience.SkipDialogue);
    True(restored.Convenience.SkipCutscenes);
    Equal(QuestRewardSelectionMode.CurrentJobReward, restored.Convenience.QuestRewardSelection);
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
    True(restored.Player.ShowCastRemainingTime);
    False(restored.FocusTarget.TargetOfFocus.ShowHpPercentage);
    False(restored.FocusTarget.TargetOfFocus.ClickToTarget);
    False(restored.FocusTarget.TargetOfFocus.RightClickContextMenu);
    False(restored.Player.ClickToTarget);
    False(restored.Player.RightClickContextMenu);
    Equal(ModuleClickableArea.HeaderOrName, restored.Target.ClickableArea);
    Equal(PvPThreatVisibility.AllPvPDuties, restored.TargetingMeCounter.PvPVisibility);
    False(restored.TargetingMeCounter.HideWhenZero);
    True(restored.TargetingMeCounter.ShowJobs);
    True(restored.TargetingMeCounter.ShowTargeterDetails);
    Near(1.31f, restored.TargetingMeCounter.Scale);
    Near(0.63f, restored.TargetingMeCounter.Opacity);
    Near(377f, restored.TargetingMeCounter.Width);
    Near(91f, restored.TargetingMeCounter.NumberSize);
    Near(0.284f, restored.TargetingMeCounter.Layout.AnchorX);
    Near(0.8f, restored.TargetingMeCounter.HighColour.Blue);
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

static void TestVersionFourMigration()
{
    var source = new HudConfigurationData
    {
        Version = 4,
    };
    source.Player.Layout.AnchorX = 0.27f;
    source.Player.Layout.AnchorY = 0.74f;
    source.Player.MpDisplay = MpDisplayMode.TextOnly;
    source.FocusTarget.TargetOfFocus = null!;

    HudConfigurationMigrator.Normalize(source);

    Equal(HudConfigurationData.CurrentVersion, source.Version);
    Near(0.27f, source.Player.Layout.AnchorX);
    Near(0.74f, source.Player.Layout.AnchorY);
    Equal(MpDisplayMode.TextOnly, source.Player.MpDisplay);
    NotNull(source.FocusTarget.TargetOfFocus);
    True(source.FocusTarget.TargetOfFocus.Show);
    True(source.FocusTarget.TargetOfFocus.ClickToTarget);
}

static void TestVersionFiveUpgradePersistence()
{
    var versionA = new HudConfigurationData
    {
        Version = 5,
        Enabled = false,
        Locked = false,
        GlobalScale = 1.17f,
        GlobalOpacity = 0.73f,
    };
    versionA.Player.Layout.AnchorX = 0.123f;
    versionA.Player.Layout.AnchorY = 0.654f;
    versionA.Player.Width = 337f;
    versionA.Player.Scale = 1.23f;
    versionA.Player.BarHeight = 14f;
    versionA.Player.ShowMaximumHp = false;
    versionA.Player.MpDisplay = MpDisplayMode.TextOnly;
    versionA.Target.Layout.AnchorX = 0.812f;
    versionA.Target.Width = 463f;
    versionA.Target.ShowDistance = false;
    versionA.Target.NativeHpOverlay.Mode = NativeTargetOverlayMode.CombatOnly;
    versionA.Target.NativeHpOverlay.OffsetX = 21f;
    versionA.FocusTarget.Enabled = false;
    versionA.FocusTarget.Visibility = ModuleVisibilityCondition.DutyOnly;
    versionA.TargetOfTarget.Width = 222f;
    versionA.SelfHighlight.Mode = SelfHighlightMode.CombatOnly;
    versionA.SelfHighlight.ColourPreset = HighlightColourPreset.Green;
    versionA.PlayerPositionMarker.Mode = SelfHighlightMode.DutyOnly;
    versionA.PlayerPositionMarker.Radius = 0.03f;
    versionA.PlayerPositionMarker.Opacity = 0.44f;
    versionA.PlayerPositionMarker.ColourPreset = HighlightColourPreset.Blue;
    versionA.Camera.Enabled = true;
    versionA.Camera.MaximumZoomDistance = 47f;
    versionA.Appearance.HostileHealth.Set(new Vector4(0.61f, 0.07f, 0.12f, 1f));

    var document = JsonNode.Parse(JsonSerializer.Serialize(versionA))!.AsObject();
    foreach (var moduleName in new[] { "Player", "Target", "FocusTarget", "TargetOfTarget" })
    {
        var module = document[moduleName]!.AsObject();
        module.Remove("ClickToTarget");
        module.Remove("ClickableArea");
    }

    var versionB = JsonSerializer.Deserialize<HudConfigurationData>(document.ToJsonString());
    NotNull(versionB);
    HudConfigurationMigrator.Normalize(versionB!);

    Equal(HudConfigurationData.CurrentVersion, versionB!.Version);
    False(versionB.Enabled);
    False(versionB.Locked);
    Near(1.17f, versionB.GlobalScale);
    Near(0.73f, versionB.GlobalOpacity);
    Near(0.123f, versionB.Player.Layout.AnchorX);
    Near(0.654f, versionB.Player.Layout.AnchorY);
    Near(337f, versionB.Player.Width);
    Near(1.23f, versionB.Player.Scale);
    Near(14f, versionB.Player.BarHeight);
    False(versionB.Player.ShowMaximumHp);
    Equal(MpDisplayMode.TextOnly, versionB.Player.MpDisplay);
    Near(0.812f, versionB.Target.Layout.AnchorX);
    Near(463f, versionB.Target.Width);
    False(versionB.Target.ShowDistance);
    Equal(NativeTargetOverlayMode.CombatOnly, versionB.Target.NativeHpOverlay.Mode);
    Near(21f, versionB.Target.NativeHpOverlay.OffsetX);
    False(versionB.FocusTarget.Enabled);
    Equal(ModuleVisibilityCondition.DutyOnly, versionB.FocusTarget.Visibility);
    Near(222f, versionB.TargetOfTarget.Width);
    Equal(SelfHighlightMode.CombatOnly, versionB.SelfHighlight.Mode);
    Equal(HighlightColourPreset.Green, versionB.SelfHighlight.ColourPreset);
    Equal(SelfHighlightMode.DutyOnly, versionB.PlayerPositionMarker.Mode);
    Near(0.03f, versionB.PlayerPositionMarker.Radius);
    Near(0.44f, versionB.PlayerPositionMarker.Opacity);
    Equal(HighlightColourPreset.Blue, versionB.PlayerPositionMarker.ColourPreset);
    True(versionB.Camera.Enabled);
    Near(47f, versionB.Camera.MaximumZoomDistance);
    Near(0.61f, versionB.Appearance.HostileHealth.Red);
    True(versionB.Player.ClickToTarget);
    True(versionB.Target.ClickToTarget);
    True(versionB.FocusTarget.ClickToTarget);
    True(versionB.TargetOfTarget.ClickToTarget);
    Equal(ModuleClickableArea.WholeModule, versionB.Player.ClickableArea);
}

static void TestVersionSixUpgradePersistence()
{
    var versionA = new HudConfigurationData
    {
        Version = 6,
        Locked = false,
        GlobalScale = 1.21f,
    };
    versionA.Player.Layout.AnchorX = 0.19f;
    versionA.Player.Layout.AnchorY = 0.77f;
    versionA.Player.Width = 401f;
    versionA.Player.BarHeight = 13f;
    versionA.Player.ClickToTarget = false;
    versionA.Target.Layout.AnchorX = 0.61f;
    versionA.Target.Width = 517f;
    versionA.FocusTarget.TargetOfFocus.ClickToTarget = false;
    versionA.SelfHighlight.ColourPreset = HighlightColourPreset.Custom;
    versionA.SelfHighlight.CustomColour.Set(new Vector4(0.07f, 0.14f, 0.91f, 1f));
    versionA.PlayerPositionMarker.Radius = 0.02f;
    versionA.Camera.MaximumZoomDistance = 53f;

    var document = JsonNode.Parse(JsonSerializer.Serialize(versionA))!.AsObject();
    document.Remove("EncounterAwareness");
    document.Remove("Convenience");
    var marker = document["PlayerPositionMarker"]!.AsObject();
    marker.Remove("DangerDetectionEnabled");
    marker.Remove("DangerColourPreset");
    marker.Remove("CustomDangerColour");
    foreach (var moduleName in new[] { "Player", "Target", "FocusTarget", "TargetOfTarget" })
        document[moduleName]!.AsObject().Remove("RightClickContextMenu");
    document["FocusTarget"]!["TargetOfFocus"]!.AsObject().Remove("RightClickContextMenu");

    var versionB = JsonSerializer.Deserialize<HudConfigurationData>(document.ToJsonString());
    NotNull(versionB);
    HudConfigurationMigrator.Normalize(versionB!);

    Equal(HudConfigurationData.CurrentVersion, versionB!.Version);
    False(versionB.Locked);
    Near(1.21f, versionB.GlobalScale);
    Near(0.19f, versionB.Player.Layout.AnchorX);
    Near(0.77f, versionB.Player.Layout.AnchorY);
    Near(401f, versionB.Player.Width);
    Near(13f, versionB.Player.BarHeight);
    False(versionB.Player.ClickToTarget);
    Near(0.61f, versionB.Target.Layout.AnchorX);
    Near(517f, versionB.Target.Width);
    False(versionB.FocusTarget.TargetOfFocus.ClickToTarget);
    Equal(HighlightColourPreset.Custom, versionB.SelfHighlight.ColourPreset);
    Near(0.91f, versionB.SelfHighlight.CustomColour.Blue);
    Near(0.02f, versionB.PlayerPositionMarker.Radius);
    Near(53f, versionB.Camera.MaximumZoomDistance);
    True(versionB.Player.RightClickContextMenu);
    True(versionB.Target.RightClickContextMenu);
    True(versionB.FocusTarget.RightClickContextMenu);
    True(versionB.TargetOfTarget.RightClickContextMenu);
    True(versionB.FocusTarget.TargetOfFocus.RightClickContextMenu);
    False(versionB.EncounterAwareness.Enabled);
    True(versionB.EncounterAwareness.NativeDetectionEnabled);
    False(versionB.EncounterAwareness.SplatoonIntegrationEnabled);
    False(versionB.Convenience.PreventAfkDisconnect);
    True(versionB.PlayerPositionMarker.DangerDetectionEnabled);
    Equal(DangerColourPreset.Red, versionB.PlayerPositionMarker.DangerColourPreset);
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

static void TestRemainingCastTimeFormatting()
{
    Equal("0.45s", HudFormatting.RemainingCastTime(1.55f, 2f));
    Equal("0.00s", HudFormatting.RemainingCastTime(3f, 2f));
    Equal("--", HudFormatting.RemainingCastTime(1f, 0f));
    Equal("--", HudFormatting.RemainingCastTime(float.NaN, 2f));
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

static void TestHorizontalEditorResize()
{
    var workPosition = new Vector2(100f, 50f);
    var workSize = new Vector2(1600f, 900f);
    var origin = new Vector2(380f, 410f);
    var result = LayoutPolicy.ResizeFromRightEdge(
        320f, 100f, origin, new Vector2(320f, 120f), workPosition, workSize);
    Near(420f, result.Width);
    Equal(origin, result.Position);
    Equal(origin, LayoutPolicy.ToPixelPosition(
        result.Layout, workPosition, workSize, new Vector2(result.Width, 120f)));

    var minimum = LayoutPolicy.ResizeFromRightEdge(
        320f, -1000f, origin, new Vector2(320f, 120f), workPosition, workSize);
    Near(HudSizingPolicy.MinimumWidth, minimum.Width);
    var maximum = LayoutPolicy.ResizeFromRightEdge(
        320f, 1000f, origin, new Vector2(320f, 120f), workPosition, workSize);
    Near(HudSizingPolicy.MaximumWidth, maximum.Width);
}

static void TestTwoAxisEditorResize()
{
    var workPosition = new Vector2(100f, 50f);
    var workSize = new Vector2(1600f, 900f);
    var origin = new Vector2(380f, 410f);
    var originalSize = new Vector2(320f, 140f);

    var corner = LayoutPolicy.ResizeFromEdges(
        320f, 20f, 3, EditorResizeEdges.Right | EditorResizeEdges.Bottom,
        new Vector2(80f, 12f), origin, originalSize, workPosition, workSize);
    Near(400f, corner.Width);
    Near(24f, corner.BarHeight);
    Equal(origin, corner.Position);
    Near(152f, corner.ApproximateSize.Y);

    var topLeft = LayoutPolicy.ResizeFromEdges(
        320f, 20f, 2, EditorResizeEdges.Left | EditorResizeEdges.Top,
        new Vector2(50f, -8f), origin, originalSize, workPosition, workSize);
    Near(270f, topLeft.Width);
    Near(24f, topLeft.BarHeight);
    Equal(new Vector2(430f, 402f), topLeft.Position);
    Near(148f, topLeft.ApproximateSize.Y);

    var clamped = LayoutPolicy.ResizeFromEdges(
        320f, 20f, 3, EditorResizeEdges.Right | EditorResizeEdges.Bottom,
        new Vector2(10_000f, 10_000f), origin, originalSize, workPosition, workSize);
    Near(HudSizingPolicy.MaximumWidth, clamped.Width);
    Near(HudSizingPolicy.MaximumBarHeight, clamped.BarHeight);
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

static void TestNativeTargetAnchorBounds()
{
    True(NativeTargetAnchorPolicy.TryCreate(
        new Vector2(500f, 120f),
        new Vector2(820f, 144f),
        3f,
        4f,
        out var anchor));
    Equal(new Vector2(503f, 148f), anchor.Position);
    Equal(new Vector2(320f, 24f), anchor.Size);

    False(NativeTargetAnchorPolicy.TryCreate(
        new Vector2(500f, 120f),
        new Vector2(520f, 144f),
        0f,
        4f,
        out _));
    False(NativeTargetAnchorPolicy.TryCreate(
        new Vector2(float.NaN, 120f),
        new Vector2(820f, 144f),
        0f,
        4f,
        out _));
}

static void TestContextMenuSuppression()
{
    var policy = new ContextMenuSuppressionPolicy(openGraceMilliseconds: 100);

    False(policy.Update(nativeMenuVisible: false, nowMilliseconds: 0));
    policy.NotifyOpenRequested(nowMilliseconds: 10);
    True(policy.Update(nativeMenuVisible: false, nowMilliseconds: 10));
    True(policy.Update(nativeMenuVisible: true, nowMilliseconds: 20));
    False(policy.Update(nativeMenuVisible: false, nowMilliseconds: 30));

    // A stock native context menu receives the same priority even without a Sentinel request.
    True(policy.Update(nativeMenuVisible: true, nowMilliseconds: 40));
    False(policy.Update(nativeMenuVisible: false, nowMilliseconds: 50));

    // A failed/delayed open cannot leave the HUD permanently suppressed.
    policy.NotifyOpenRequested(nowMilliseconds: 100);
    True(policy.Update(nativeMenuVisible: false, nowMilliseconds: 199));
    False(policy.Update(nativeMenuVisible: false, nowMilliseconds: 200));
}

static void TestContextMenuPlacement()
{
    var viewport = new ScreenRectangle(0f, 0f, 1920f, 1080f);
    var origin = new ScreenRectangle(100f, 300f, 360f, 400f);
    var size = new Vector2(220f, 300f);
    var right = ContextMenuPlacementPolicy.Place(origin, size, viewport);
    Equal(ContextMenuPlacementSide.Right, right.Side);
    Near(368f, right.Position.X);
    False(right.Bounds(size).Intersects(origin));

    origin = new ScreenRectangle(1_700f, 300f, 1_900f, 400f);
    var left = ContextMenuPlacementPolicy.Place(origin, size, viewport);
    Equal(ContextMenuPlacementSide.Left, left.Side);
    Near(1_472f, left.Position.X);
    False(left.Bounds(size).Intersects(origin));

    var compactViewport = new ScreenRectangle(0f, 0f, 800f, 600f);
    origin = new ScreenRectangle(250f, 80f, 550f, 200f);
    var below = ContextMenuPlacementPolicy.Place(origin, new Vector2(240f, 120f), compactViewport);
    Equal(ContextMenuPlacementSide.Below, below.Side);
    Near(208f, below.Position.Y);
    False(below.Bounds(new Vector2(240f, 120f)).Intersects(origin));

    origin = new ScreenRectangle(250f, 400f, 550f, 590f);
    var above = ContextMenuPlacementPolicy.Place(origin, new Vector2(240f, 180f), compactViewport);
    Equal(ContextMenuPlacementSide.Above, above.Side);
    Near(212f, above.Position.Y);
    False(above.Bounds(new Vector2(240f, 180f)).Intersects(origin));
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
    True(exact.IsSupported);

    config.ColourPreset = HighlightColourPreset.White;
    var white = NativeHighlightPolicy.Resolve(config);
    False(white.IsSupported);
    Equal("White", white.DisplayName);

    config.ColourPreset = HighlightColourPreset.Custom;
    config.CustomColour.Set(new Vector4(0.98f, 0.22f, 0.75f, 1f));
    var custom = NativeHighlightPolicy.Resolve(config);
    True(custom.IsSupported);
    Equal(NativeHighlightColour.Magenta, custom.Colour);
    Equal("Magenta", custom.DisplayName);
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

    config.DangerDetectionEnabled = true;
    config.DangerColourPreset = DangerColourPreset.Red;
    var danger = PlayerPositionMarkerPolicy.ResolveColour(config, true);
    Near(1f, danger.X);
    Near(0.12f, danger.Y);
    Near(0.08f, danger.Z);
    Near(0.64f, danger.W);

    config.DangerColourPreset = DangerColourPreset.Custom;
    config.CustomDangerColour.Set(new Vector4(0.5f, 0.1f, 0.8f, 1f));
    danger = PlayerPositionMarkerPolicy.ResolveColour(config, true);
    Near(0.5f, danger.X);
    Near(0.1f, danger.Y);
    Near(0.8f, danger.Z);

    Near(1f, PlayerPositionMarkerPolicy.ResolveCameraFacingRadius(0.01f));
    Near(1.3084745f, PlayerPositionMarkerPolicy.ResolveCameraFacingRadius(0.02f));
    Near(6.244068f, PlayerPositionMarkerPolicy.ResolveCameraFacingRadius(0.18f));
    Near(19.2f, PlayerPositionMarkerPolicy.ResolveCameraFacingRadius(0.60f));
    False(PlayerPositionMarkerPolicy.UsesTerrainProjection(PlayerPositionMarkerStyle.CameraFacing));
    True(PlayerPositionMarkerPolicy.UsesTerrainProjection(PlayerPositionMarkerStyle.GroundProjected));
}

static void TestVersionSevenMarkerStyleMigration()
{
    var versionA = new HudConfigurationData
    {
        Version = 7,
        PlayerPositionMarker = new PlayerPositionMarkerConfiguration
        {
            Mode = SelfHighlightMode.CombatOnly,
            Style = PlayerPositionMarkerStyle.GroundProjected,
            Radius = 0.04f,
            Opacity = 0.61f,
            ColourPreset = HighlightColourPreset.Green,
            ShowBorder = false,
            DangerDetectionEnabled = true,
            DangerColourPreset = DangerColourPreset.Orange,
        },
    };

    var document = JsonNode.Parse(JsonSerializer.Serialize(versionA))!.AsObject();
    document["PlayerPositionMarker"]!.AsObject().Remove("Style");
    var source = JsonSerializer.Deserialize<HudConfigurationData>(document.ToJsonString());
    NotNull(source);

    HudConfigurationMigrator.Normalize(source!);

    Equal(HudConfigurationData.CurrentVersion, source!.Version);
    Equal(PlayerPositionMarkerStyle.CameraFacing, source.PlayerPositionMarker.Style);
    Equal(SelfHighlightMode.CombatOnly, source.PlayerPositionMarker.Mode);
    Near(0.04f, source.PlayerPositionMarker.Radius);
    Near(0.61f, source.PlayerPositionMarker.Opacity);
    Equal(HighlightColourPreset.Green, source.PlayerPositionMarker.ColourPreset);
    False(source.PlayerPositionMarker.ShowBorder);
    True(source.PlayerPositionMarker.DangerDetectionEnabled);
    Equal(DangerColourPreset.Orange, source.PlayerPositionMarker.DangerColourPreset);
}

static void TestVersionEightQuestConvenienceMigration()
{
    var versionA = new HudConfigurationData
    {
        Version = 8,
        Locked = false,
    };
    versionA.Convenience.PreventAfkDisconnect = true;
    versionA.Player.Layout.AnchorX = 0.314f;
    versionA.Target.Width = 471f;

    var document = JsonNode.Parse(JsonSerializer.Serialize(versionA))!.AsObject();
    var convenience = document["Convenience"]!.AsObject();
    convenience.Remove("SkipDialogue");
    convenience.Remove("SkipCutscenes");
    convenience.Remove("QuestRewardSelection");
    var source = JsonSerializer.Deserialize<HudConfigurationData>(document.ToJsonString());
    NotNull(source);

    HudConfigurationMigrator.Normalize(source!);

    Equal(HudConfigurationData.CurrentVersion, source!.Version);
    False(source.Locked);
    True(source.Convenience.PreventAfkDisconnect);
    False(source.Convenience.SkipDialogue);
    False(source.Convenience.SkipCutscenes);
    Equal(QuestRewardSelectionMode.Manual, source.Convenience.QuestRewardSelection);
    Near(0.314f, source.Player.Layout.AnchorX);
    Near(471f, source.Target.Width);
}

static void TestVersionNineTargetingMeMigration()
{
    var versionA = new HudConfigurationData
    {
        Version = 9,
        Locked = false,
    };
    versionA.Player.Layout.AnchorX = 0.219f;
    versionA.Target.Width = 438f;
    versionA.SelfHighlight.Mode = SelfHighlightMode.DutyOnly;
    versionA.Convenience.SkipDialogue = true;

    var document = JsonNode.Parse(JsonSerializer.Serialize(versionA))!.AsObject();
    document.Remove("TargetingMeCounter");
    var source = JsonSerializer.Deserialize<HudConfigurationData>(document.ToJsonString());
    NotNull(source);

    HudConfigurationMigrator.Normalize(source!);

    Equal(HudConfigurationData.CurrentVersion, source!.Version);
    False(source.Locked);
    Near(0.219f, source.Player.Layout.AnchorX);
    Near(438f, source.Target.Width);
    Equal(SelfHighlightMode.DutyOnly, source.SelfHighlight.Mode);
    True(source.Convenience.SkipDialogue);
    True(source.TargetingMeCounter.Enabled);
    Equal(PvPThreatVisibility.FrontlineOnly, source.TargetingMeCounter.PvPVisibility);
    True(source.TargetingMeCounter.HideWhenZero);
    False(source.TargetingMeCounter.ShowJobs);
    False(source.TargetingMeCounter.ShowTargeterDetails);
    False(source.TargetingMeCounter.BorderEnabled);
    Near(0.45f, source.TargetingMeCounter.Layout.AnchorX);
    Near(0.24f, source.TargetingMeCounter.Layout.AnchorY);
}

static void TestVersionTenModernThemeMigration()
{
    var versionA = new HudConfigurationData
    {
        Version = 10,
        Locked = false,
    };
    versionA.Player.Layout.AnchorX = 0.271f;
    versionA.Target.Width = 492f;
    versionA.Convenience.SkipCutscenes = true;
    versionA.SelfHighlight.Mode = SelfHighlightMode.Always;

    var document = JsonNode.Parse(JsonSerializer.Serialize(versionA))!.AsObject();
    document["Appearance"]!.AsObject().Remove("ConfigurationTheme");
    var source = JsonSerializer.Deserialize<HudConfigurationData>(document.ToJsonString());
    NotNull(source);

    HudConfigurationMigrator.Normalize(source!);

    Equal(HudConfigurationData.CurrentVersion, source!.Version);
    Equal(ConfigurationWindowTheme.SentinelModern, source.Appearance.ConfigurationTheme);
    False(source.Locked);
    Near(0.271f, source.Player.Layout.AnchorX);
    Near(492f, source.Target.Width);
    True(source.Convenience.SkipCutscenes);
    Equal(SelfHighlightMode.Always, source.SelfHighlight.Mode);
}

static void TestDangerGeometryContainment()
{
    var circle = DangerArea.Circle(Vector3.Zero, 5f, "test", "circle");
    True(circle.Contains(new Vector3(3f, 0f, 4f)));
    False(circle.Contains(new Vector3(5.1f, 0f, 0f)));
    False(circle.Contains(new Vector3(0f, 6f, 0f)));

    var donut = DangerArea.Donut(Vector3.Zero, 2f, 6f, "test", "donut");
    False(donut.Contains(Vector3.Zero));
    True(donut.Contains(new Vector3(0f, 0f, 4f)));
    False(donut.Contains(new Vector3(0f, 0f, 7f)));

    var rectangle = DangerArea.Rectangle(Vector3.Zero, 8f, 2f, 0f, "test", "rect");
    True(rectangle.Contains(new Vector3(1.9f, 0f, 7.9f)));
    False(rectangle.Contains(new Vector3(2.1f, 0f, 4f)));
    False(rectangle.Contains(new Vector3(0f, 0f, -1f)));

    var cone = DangerArea.Cone(Vector3.Zero, 10f, 0f, MathF.PI / 4f, "test", "cone");
    True(cone.Contains(new Vector3(3f, 0f, 5f)));
    False(cone.Contains(new Vector3(8f, 0f, 1f)));

    var line = DangerArea.Line(Vector3.Zero, new Vector3(0f, 0f, 10f), 1f, "test", "line");
    True(line.Contains(new Vector3(0.8f, 0f, 5f)));
    False(line.Contains(new Vector3(1.2f, 0f, 5f)));

    var cross = DangerArea.Cross(Vector3.Zero, 6f, 1f, 0f, "test", "cross");
    True(cross.Contains(new Vector3(5f, 0f, 0.5f)));
    True(cross.Contains(new Vector3(0.5f, 0f, 5f)));
    False(cross.Contains(new Vector3(3f, 0f, 3f)));
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

static void TestCameraZoomDeathPersistence()
{
    var policy = new CameraZoomPersistencePolicy();

    False(policy.Update(false, 44f, 60f, 0).ShouldRestore);
    var deathReset = policy.Update(true, 6f, 60f, 100);
    True(deathReset.ShouldRestore);
    Near(44f, deathReset.Zoom);

    False(policy.Update(true, 44f, 60f, 200).ShouldRestore);
    False(policy.Update(true, 6f, 60f,
        100 + CameraZoomPersistencePolicy.DeathRestoreWindowMilliseconds + 1).ShouldRestore);

    var respawnReset = policy.Update(false, 6f, 60f, 4_000);
    True(respawnReset.ShouldRestore);
    Near(44f, respawnReset.Zoom);
    False(policy.Update(false, 44f, 60f, 4_001).ShouldRestore);

    policy.Reset();
    False(policy.Update(false, 70f, 100f, 5_000).ShouldRestore);
    var clamped = policy.Update(true, 6f, 50f, 5_100);
    True(clamped.ShouldRestore);
    Near(50f, clamped.Zoom);
}

static void TestQuestRewardSelectionPolicy()
{
    var candidates = new[]
    {
        new QuestRewardCandidate(0, 100, 1, "First", false, 0, 1, false, 20),
        new QuestRewardCandidate(1, 101, 1, "Shared role", true, 10, 700, false, 30),
        new QuestRewardCandidate(2, 102, 1, "Specific job", true, 20, 690, false, 40),
        new QuestRewardCandidate(3, 5826, 2, "Allagan Gold Piece", false, 0, 0, true, 2_500),
        new QuestRewardCandidate(4, 5827, 1, "Allagan Platinum Piece", false, 0, 0, true, 10_000),
    };

    Equal<QuestRewardDecision?>(null,
        QuestRewardSelectionPolicy.Select(QuestRewardSelectionMode.Manual, candidates));

    var first = QuestRewardSelectionPolicy.Select(QuestRewardSelectionMode.FirstReward, candidates);
    NotNull(first);
    Equal(0, first!.Value.Index);
    Equal(QuestRewardSelectionPolicy.FirstRewardReason, first.Value.Reason);

    var job = QuestRewardSelectionPolicy.Select(QuestRewardSelectionMode.CurrentJobReward, candidates);
    NotNull(job);
    Equal(2, job!.Value.Index);
    Equal(QuestRewardSelectionPolicy.CurrentJobMatchReason, job.Value.Reason);

    var jobFallback = QuestRewardSelectionPolicy.Select(
        QuestRewardSelectionMode.CurrentJobReward,
        candidates.Where(candidate => !candidate.IsCurrentJobCompatible).ToArray());
    NotNull(jobFallback);
    Equal(0, jobFallback!.Value.Index);
    Equal(QuestRewardSelectionPolicy.NoJobMatchFallbackReason, jobFallback.Value.Reason);

    var allagan = QuestRewardSelectionPolicy.Select(QuestRewardSelectionMode.AllaganPiece, candidates);
    NotNull(allagan);
    Equal(4, allagan!.Value.Index);
    Equal(QuestRewardSelectionPolicy.AllaganPieceReason, allagan.Value.Reason);

    var allaganFallback = QuestRewardSelectionPolicy.Select(
        QuestRewardSelectionMode.AllaganPiece,
        candidates.Where(candidate => !candidate.IsAllaganPiece).ToArray());
    NotNull(allaganFallback);
    Equal(0, allaganFallback!.Value.Index);
    Equal(QuestRewardSelectionPolicy.NoAllaganPieceFallbackReason, allaganFallback.Value.Reason);

    var itemLevelTieBreak = QuestRewardSelectionPolicy.Select(
        QuestRewardSelectionMode.CurrentJobReward,
        new[]
        {
            new QuestRewardCandidate(0, 201, 1, "Lower", true, 12, 650, false, 0),
            new QuestRewardCandidate(1, 202, 1, "Higher", true, 12, 660, false, 0),
        });
    Equal(1, itemLevelTieBreak!.Value.Index);
}

static void TestCutsceneSkipConfirmationPolicy()
{
    True(CutsceneSkipPromptPolicy.TryMatch(
        CutsceneSkipPromptPolicy.SelectStringAddonName,
        42,
        42,
        out var selectStringKind));
    Equal(CutsceneSkipPromptKind.SelectString, selectStringKind);

    True(CutsceneSkipPromptPolicy.TryMatch(
        CutsceneSkipPromptPolicy.CutSceneSelectStringAddonName,
        43,
        43,
        out var cutSceneSelectStringKind));
    Equal(CutsceneSkipPromptKind.CutSceneSelectString, cutSceneSelectStringKind);

    False(CutsceneSkipPromptPolicy.TryMatch("SelectYesno", 42, 42, out _));
    False(CutsceneSkipPromptPolicy.TryMatch("SelectString", 41, 42, out _));
    False(CutsceneSkipPromptPolicy.TryMatch("SelectString", 0, 0, out _));
    False(CutsceneSkipPromptPolicy.TryMatch("SelectString", 42, ushort.MaxValue + 1u, out _));

    var pending = CutsceneSkipConfirmationState.Begin(1_000, 5_000);
    True(pending.Pending);
    False(pending.PromptObserved);

    // The request remains eligible while the dedicated modal is being created, even if the
    // game's cutscene condition has already fallen. An unrelated prompt is never passed here.
    Equal(
        CutsceneSkipConfirmationDecision.Wait,
        CutsceneSkipConfirmationPolicy.Evaluate(ref pending, 1_250, false, false));
    True(pending.Pending);
    False(pending.PromptObserved);

    Equal(
        CutsceneSkipConfirmationDecision.Wait,
        CutsceneSkipConfirmationPolicy.Evaluate(ref pending, 1_500, true, false));
    True(pending.PromptObserved);
    Equal(
        CutsceneSkipConfirmationDecision.Confirm,
        CutsceneSkipConfirmationPolicy.Evaluate(ref pending, 1_600, true, true));

    var dismissed = CutsceneSkipConfirmationState.Begin(2_000, 5_000);
    Equal(
        CutsceneSkipConfirmationDecision.Wait,
        CutsceneSkipConfirmationPolicy.Evaluate(ref dismissed, 2_100, true, false));
    Equal(
        CutsceneSkipConfirmationDecision.Dismissed,
        CutsceneSkipConfirmationPolicy.Evaluate(ref dismissed, 2_200, false, false));

    var timedOut = CutsceneSkipConfirmationState.Begin(3_000, 5_000);
    Equal(
        CutsceneSkipConfirmationDecision.Wait,
        CutsceneSkipConfirmationPolicy.Evaluate(ref timedOut, 7_999, false, false));
    Equal(
        CutsceneSkipConfirmationDecision.TimedOut,
        CutsceneSkipConfirmationPolicy.Evaluate(ref timedOut, 8_000, false, false));

    var idle = default(CutsceneSkipConfirmationState);
    Equal(
        CutsceneSkipConfirmationDecision.Wait,
        CutsceneSkipConfirmationPolicy.Evaluate(ref idle, 9_000, true, true));
}

static void TestPvPThreatPolicy()
{
    True(PvPThreatPolicy.IsValidFrontlineBattalion(0));
    True(PvPThreatPolicy.IsValidFrontlineBattalion(1));
    True(PvPThreatPolicy.IsValidFrontlineBattalion(2));
    False(PvPThreatPolicy.IsValidFrontlineBattalion(3));
    False(PvPThreatPolicy.IsValidFrontlineBattalion(byte.MaxValue));
    True(PvPThreatPolicy.IsBattalionEnemy(0, 1));
    True(PvPThreatPolicy.IsBattalionEnemy(0, 2));
    False(PvPThreatPolicy.IsBattalionEnemy(0, 0));
    False(PvPThreatPolicy.IsBattalionEnemy(byte.MaxValue, 1));

    True(PvPThreatPolicy.IsFallbackHostile(false, true, false, false, false));
    False(PvPThreatPolicy.IsFallbackHostile(false, true, true, false, false));
    False(PvPThreatPolicy.IsFallbackHostile(false, true, false, true, false));
    False(PvPThreatPolicy.IsFallbackHostile(false, true, false, false, true));
    False(PvPThreatPolicy.IsFallbackHostile(true, true, false, false, false));

    Equal(PvPThreatLevel.None, PvPThreatPolicy.EvaluateLevel(0, 0));
    Equal(PvPThreatLevel.Low, PvPThreatPolicy.EvaluateLevel(1, 1));
    Equal(PvPThreatLevel.Moderate, PvPThreatPolicy.EvaluateLevel(2, 2));
    Equal(PvPThreatLevel.High, PvPThreatPolicy.EvaluateLevel(4, 2));
    Equal(PvPThreatLevel.Extreme, PvPThreatPolicy.EvaluateLevel(6, 2));
    Equal(PvPThreatLevel.Extreme, PvPThreatPolicy.EvaluateLevel(0, 10));
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

static void Throws<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}
