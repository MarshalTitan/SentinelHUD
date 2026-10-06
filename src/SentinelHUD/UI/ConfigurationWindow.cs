using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using SentinelCore.Configuration;
using SentinelCore.Diagnostics;
using SentinelCore.UI;
using SentinelHUD.Core;
using SentinelHUD.Persistence;
using SentinelHUD.Services;

namespace SentinelHUD.UI;

public sealed class ConfigurationWindow : Window, IDisposable
{
    private static readonly string[] HighlightModes = ["Off", "Always", "Combat Only", "Duty Only"];
    private static readonly string[] HighlightColours = ["Yellow", "Green", "Blue", "White", "Custom"];
    private static readonly string[] DangerColours = ["Red", "Orange", "Yellow", "Custom"];
    private static readonly string[] MarkerStyles = ["Ground Projected", "Camera Facing"];
    private static readonly string[] ModuleNames = ["Player", "Target", "Focus Target", "Target of Target", "Targeting Me Counter"];
    private static readonly string[] ClickableAreas = ["Whole module", "Header / name only"];
    private static readonly string[] ModuleVisibilityModes = ["Always", "Combat Only", "Duty Only", "Combat or Duty"];
    private static readonly string[] ShieldModes = ["Off", "Text Only", "Bar Only", "Bar + Text"];
    private static readonly string[] MpModes = ["Off", "Text Only", "Bar Only", "Bar + Text"];
    private static readonly string[] HpColourModes = ["Static / Role-Based", "Health-State Gradient"];
    private static readonly string[] TextAlignments = ["Left", "Center", "Right"];
    private static readonly string[] NumberFormats = ["Full", "Compact"];
    private static readonly string[] NativeOverlayModes = ["Off", "Always", "Combat Only"];
    private static readonly string[] NativeOverlayFormats = ["Current / Maximum", "Percentage", "Current / Maximum + Percentage"];
    private static readonly string[] QuestRewardModes = ["Manual", "First Reward", "Current Job Reward", "Allagan Piece"];
    private static readonly string[] PvPThreatVisibilityModes = ["All PvP Duties", "Frontline Only"];
    private static readonly SentinelModernNavItem[] ModernPrimaryNavigation =
    [
        new(SentinelHudModernNavigationState.GeneralId, null, "General")
        {
            DrawIcon = static context => DrawModernNavigationIcon(FontAwesomeIcon.Cog, context),
        },
        new(SentinelHudModernNavigationState.HudId, null, "HUD Elements")
        {
            DrawIcon = static context => DrawModernNavigationIcon(FontAwesomeIcon.Desktop, context),
        },
        new(SentinelHudModernNavigationState.AwarenessId, null, "Awareness")
        {
            DrawIcon = static context => DrawModernNavigationIcon(FontAwesomeIcon.Eye, context),
        },
        new(SentinelHudModernNavigationState.SystemsId, null, "Systems")
        {
            DrawIcon = static context => DrawModernNavigationIcon(FontAwesomeIcon.Tools, context),
        },
        new(SentinelHudModernNavigationState.AppearanceId, null, "Appearance")
        {
            DrawIcon = static context => DrawModernNavigationIcon(FontAwesomeIcon.Palette, context),
        },
        new(SentinelHudModernNavigationState.DiagnosticsId, null, "Diagnostics")
        {
            DrawIcon = static context => DrawModernNavigationIcon(FontAwesomeIcon.Bug, context),
        },
    ];

    private static readonly Vector2 ClassicMinimumWindowSize = new(720f, 560f);
    private readonly ConfigurationCoordinator<Configuration> configuration;
    private readonly HudRenderer renderer;
    private readonly QuestConvenienceService questConvenience;
    private readonly DiagnosticBuffer diagnostics;
    private readonly ResilientConfigurationStore configurationStore;
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly SentinelModernStyleScope modernStyle = new();
    private readonly SentinelModernAppShellState modernShellState = new();
    private readonly SentinelHudModernNavigationState modernNavigation = new();
    private readonly Action<string> selectModernPrimaryPage;
    private readonly Action drawModernPage;
    private readonly Action drawModernSecondaryNavigation;
    private readonly Action drawModernActionDock;
    private readonly Action requestModernCollapse;
    private readonly Action requestModernClose;
    private readonly Action<SentinelModernIconDrawContext> drawModernPluginIcon;
    private readonly Action drawGlobalScaleControl;
    private readonly Action drawGlobalOpacityControl;
    private readonly ImGuiWindowFlags classicWindowFlags;
    private int selectedLayoutModule;
    private bool modernThemeActive;
    private bool modernCollapsed;
    private bool expandOnNextDraw;
    private IDisposable? activeClassicStyleScope;
    private bool disposed;

    public ConfigurationWindow(
        ConfigurationCoordinator<Configuration> configuration,
        HudRenderer renderer,
        QuestConvenienceService questConvenience,
        DiagnosticBuffer diagnostics,
        ResilientConfigurationStore configurationStore,
        IDalamudPluginInterface pluginInterface)
        : base("Sentinel HUD Configuration##SentinelHUD-Configuration")
    {
        this.configuration = configuration;
        this.renderer = renderer;
        this.questConvenience = questConvenience;
        this.diagnostics = diagnostics;
        this.configurationStore = configurationStore;
        this.pluginInterface = pluginInterface;
        selectModernPrimaryPage = SelectModernPrimaryPage;
        drawModernPage = DrawModernPage;
        drawModernSecondaryNavigation = DrawModernSecondaryNavigation;
        drawModernActionDock = DrawModernActionDock;
        requestModernCollapse = RequestModernCollapse;
        requestModernClose = RequestModernClose;
        drawModernPluginIcon = DrawModernPluginIcon;
        drawGlobalScaleControl = DrawGlobalScaleControl;
        drawGlobalOpacityControl = DrawGlobalOpacityControl;
        classicWindowFlags = Flags;
        Size = new Vector2(920f, 720f);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = ClassicMinimumWindowSize };
    }

    public override void PreDraw()
    {
        activeClassicStyleScope?.Dispose();
        activeClassicStyleScope = null;
        modernStyle.Pop();
        modernThemeActive = configuration.Current.Appearance.ConfigurationTheme
                            == ConfigurationWindowTheme.SentinelModern;
        if (expandOnNextDraw)
        {
            ImGui.SetNextWindowCollapsed(false, ImGuiCond.Always);
            expandOnNextDraw = false;
            modernCollapsed = false;
        }
        if (modernThemeActive)
        {
            var scale = ImGuiHelpers.GlobalScale;
            Flags = SentinelModernWindowChrome.UseCustomHeader(classicWindowFlags);
            modernStyle.PushAppShell(scale);
            var shellMinimum = SentinelModernAppLayout.MinimumWindowSize(
                scale,
                hasSecondarySidebar: true,
                hasActionDock: true);
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = SentinelHudModernWindowPolicy.MinimumSize(scale, shellMinimum),
            };
        }
        else
        {
            Flags = classicWindowFlags;
            activeClassicStyleScope = SentinelStyleScope.PushWindow();
            SizeConstraints = new WindowSizeConstraints { MinimumSize = ClassicMinimumWindowSize };
        }
    }

    public override void PostDraw()
    {
        modernStyle.Pop();
        activeClassicStyleScope?.Dispose();
        activeClassicStyleScope = null;
    }

    public override void Draw()
    {
        if (modernThemeActive)
        {
            DrawModernShell();
            return;
        }

        DrawClassicShell();
    }

    private void DrawClassicShell()
    {
        DrawSectionHeader("Sentinel HUD");
        ImGui.TextDisabled("Modular enhancements for the native FFXIV HUD");
        ImGui.SameLine();
        if (ImGui.SmallButton("Use Sentinel Modern theme"))
            Update(c => c.Appearance.ConfigurationTheme = ConfigurationWindowTheme.SentinelModern);
        ImGui.Spacing();
        if (!ImGui.BeginTabBar("SentinelHUD-SettingsTabs"))
            return;
        try
        {
            DrawTab("General", DrawGeneral);
            DrawTab("Player", DrawPlayer);
            DrawTab("Target", DrawTarget);
            DrawTab("Focus Target", DrawFocusTarget);
            DrawTab("Target-of-Target", DrawTargetOfTarget);
            DrawTab("Awareness", DrawAwareness);
            DrawTab("Encounter Awareness", DrawEncounterAwareness);
            DrawTab("Camera", DrawCamera);
            DrawTab("Questing / Convenience", DrawConvenience);
            DrawTab("Appearance", DrawAppearance);
            DrawTab("Layout", DrawLayout);
            DrawTab("Diagnostics", DrawDiagnostics);
        }
        finally
        {
            ImGui.EndTabBar();
        }
    }

    private void DrawModernShell()
    {
        var page = modernNavigation.ContentPage;
        var (title, _) = GetModernPageMetadata(page);
        var locked = configuration.Current.Locked;
        var options = new SentinelModernAppShellOptions(
            "SentinelHUD.Modern2",
            "Sentinel HUD",
            modernNavigation.PrimaryPageId)
        {
            DrawPluginIcon = drawModernPluginIcon,
            ContextLabel = title,
            Status = new SentinelModernStatusPillOptions(
                locked ? "HUD LOCKED" : "EDIT MODE",
                locked ? SentinelModernPillTone.Ready : SentinelModernPillTone.Warning)
            {
                Tooltip = locked
                    ? "Gameplay mode; configured actor interactions are active."
                    : "Visual editor active; targeting interactions are suspended.",
            },
            Scale = ImGuiHelpers.GlobalScale,
            DeltaTime = ImGui.GetIO().DeltaTime,
            ReducedMotion = pluginInterface.UiBuilder.ShouldUseReducedMotion,
            AmbientIntensity = 0.9f,
            SurfaceStyle = SentinelModernAppSurfaceStyle.Unified,
            EnableWindowDragging = true,
            RequestCollapse = requestModernCollapse,
            RequestClose = requestModernClose,
        };

        SentinelModernAppShell.Draw(
            options,
            modernShellState,
            ModernPrimaryNavigation,
            selectModernPrimaryPage,
            drawModernPage,
            modernNavigation.HasSecondaryNavigation ? drawModernSecondaryNavigation : null,
            page == SentinelHudModernContentPage.Appearance ? drawModernActionDock : null);
    }

    private void DrawModernPage()
    {
        var page = modernNavigation.ContentPage;
        var (title, description) = GetModernPageMetadata(page);
        SentinelModernUi.PageHeading(title, description);
        ImGui.Spacing();

        switch (page)
        {
            case SentinelHudModernContentPage.General: DrawModernGeneral(); break;
            case SentinelHudModernContentPage.Player: DrawPlayer(); break;
            case SentinelHudModernContentPage.Target: DrawTarget(); break;
            case SentinelHudModernContentPage.FocusTarget: DrawFocusTarget(); break;
            case SentinelHudModernContentPage.TargetOfTarget: DrawTargetOfTarget(); break;
            case SentinelHudModernContentPage.Layout: DrawLayout(); break;
            case SentinelHudModernContentPage.Awareness: DrawAwareness(); break;
            case SentinelHudModernContentPage.EncounterAwareness: DrawEncounterAwareness(); break;
            case SentinelHudModernContentPage.Camera: DrawCamera(); break;
            case SentinelHudModernContentPage.Convenience: DrawConvenience(); break;
            case SentinelHudModernContentPage.Appearance: DrawAppearance(); break;
            case SentinelHudModernContentPage.Diagnostics: DrawDiagnostics(); break;
        }
    }

    private void DrawModernSecondaryNavigation()
    {
        switch (modernNavigation.PrimaryPage)
        {
            case SentinelHudModernPrimaryPage.Hud:
                SentinelModernSecondaryNavigation.GroupLabel("HUD Elements");
                DrawModernSecondaryItem(SentinelHudModernContentPage.Player, "Player");
                DrawModernSecondaryItem(SentinelHudModernContentPage.Target, "Target");
                DrawModernSecondaryItem(SentinelHudModernContentPage.FocusTarget, "Focus Target");
                DrawModernSecondaryItem(SentinelHudModernContentPage.TargetOfTarget, "Target-of-Target");
                ImGui.Spacing();
                SentinelModernSecondaryNavigation.GroupLabel("Tools");
                DrawModernSecondaryItem(SentinelHudModernContentPage.Layout, "Layout");
                break;

            case SentinelHudModernPrimaryPage.Awareness:
                SentinelModernSecondaryNavigation.GroupLabel("Personal");
                DrawModernSecondaryItem(SentinelHudModernContentPage.Awareness, "Awareness");
                ImGui.Spacing();
                SentinelModernSecondaryNavigation.GroupLabel("Encounter");
                DrawModernSecondaryItem(
                    SentinelHudModernContentPage.EncounterAwareness,
                    "Encounter Awareness");
                break;

            case SentinelHudModernPrimaryPage.Systems:
                SentinelModernSecondaryNavigation.GroupLabel("Systems");
                DrawModernSecondaryItem(SentinelHudModernContentPage.Camera, "Camera");
                ImGui.Spacing();
                SentinelModernSecondaryNavigation.GroupLabel("Convenience");
                DrawModernSecondaryItem(
                    SentinelHudModernContentPage.Convenience,
                    "Questing / Convenience");
                break;
        }
    }

    private void DrawModernSecondaryItem(SentinelHudModernContentPage page, string label)
    {
        if (SentinelModernSecondaryNavigation.Item(
                $"SentinelHUD.Secondary.{page}",
                label,
                modernNavigation.ContentPage == page,
                modernShellState.Motion,
                ImGuiHelpers.GlobalScale))
            modernNavigation.SelectContent(page);
    }

    private void DrawModernActionDock()
    {
        SentinelModernActionDock.Status("Sentinel Modern 2 is active");
        ImGui.SameLine();
        if (SentinelModernActionDock.PrimaryButton(
                "SentinelHUD.UseClassic",
                "Use Classic Theme",
                new Vector2(190f * ImGuiHelpers.GlobalScale, 0f),
                ImGuiHelpers.GlobalScale))
            Update(c => c.Appearance.ConfigurationTheme = ConfigurationWindowTheme.Classic);
    }

    private void DrawModernGeneral()
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var card = SentinelModernGlassCard.Begin(
            "SentinelHUD.Modern2.General",
            new SentinelModernGlassCardOptions
            {
                Size = new Vector2(0f, 300f),
                Accent = SentinelModernPalette.Accent,
                AccentStrength = 0.12f,
                Elevated = true,
            },
            scale);
        if (!card.IsVisible)
            return;

        SentinelModernUi.SectionHeader("Global HUD");
        var enabled = configuration.Current.Enabled;
        if (SentinelModernSwitch.Draw(
                "SentinelHUD.Enabled",
                "Enable Sentinel HUD",
                ref enabled,
                modernShellState.Motion,
                scale))
            Update(c => c.Enabled = enabled);

        var locked = configuration.Current.Locked;
        if (SentinelModernSwitch.Draw(
                "SentinelHUD.Locked",
                "Lock HUD",
                ref locked,
                modernShellState.Motion,
                scale))
        {
            Update(c => c.Locked = locked);
            renderer.RequestRepositionAll();
        }

        SentinelModernSettingsRow.Draw(
            "SentinelHUD.GlobalScale",
            "Global scale",
            "Scales all Sentinel HUD modules without changing their saved anchors.",
            drawGlobalScaleControl,
            scale: scale);
        SentinelModernSettingsRow.Draw(
            "SentinelHUD.GlobalOpacity",
            "Global opacity",
            "Applies a shared opacity multiplier while retaining module appearance values.",
            drawGlobalOpacityControl,
            scale: scale);
        ImGui.TextWrapped(configuration.Current.Locked
            ? "Gameplay mode is active. Enabled actor regions use the configured targeting interactions; unrelated HUD space remains click-through."
            : "Visual editing is active. Drag modules to move them and use their edges or corners to resize them. Actor actions are suspended while editing.");
    }

    private void DrawGlobalScaleControl()
    {
        var value = configuration.Current.GlobalScale;
        if (!ImGui.SliderFloat("##GlobalScale", ref value, 0.65f, 1.75f, "%.2fx"))
            return;
        Update(c => c.GlobalScale = value);
        renderer.RequestRepositionAll();
    }

    private void DrawGlobalOpacityControl()
    {
        var value = configuration.Current.GlobalOpacity;
        if (ImGui.SliderFloat("##GlobalOpacity", ref value, 0.2f, 1f, "%.0f%%"))
            Update(c => c.GlobalOpacity = value);
    }

    private void SelectModernPrimaryPage(string id) => modernNavigation.SelectPrimary(id);

    public void OpenAndExpand()
    {
        IsOpen = true;
        expandOnNextDraw = true;
    }

    public void ToggleFromCommand()
    {
        if (!IsOpen || modernCollapsed)
        {
            OpenAndExpand();
            return;
        }

        IsOpen = false;
    }

    private void RequestModernCollapse()
    {
        modernCollapsed = true;
        ImGui.SetWindowCollapsed("Sentinel HUD Configuration##SentinelHUD-Configuration", true);
    }

    private void RequestModernClose() => IsOpen = false;

    private static void DrawModernPluginIcon(SentinelModernIconDrawContext context)
        => DrawFontAwesomeIcon(
            FontAwesomeIcon.ShieldAlt,
            context.DrawList,
            context.Minimum,
            context.Maximum,
            SentinelModernPalette.Text);

    private static void DrawModernNavigationIcon(
        FontAwesomeIcon icon,
        SentinelModernNavIconDrawContext context)
        => DrawFontAwesomeIcon(
            icon,
            context.DrawList,
            context.Minimum,
            context.Maximum,
            context.Colour);

    private static void DrawFontAwesomeIcon(
        FontAwesomeIcon icon,
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum,
        Vector4 colour)
    {
        var glyph = icon.ToIconString();
        ImGui.PushFont(UiBuilder.IconFont);
        try
        {
            var size = ImGui.CalcTextSize(glyph);
            drawList.AddText(
                minimum + (((maximum - minimum) - size) * 0.5f),
                ImGui.ColorConvertFloat4ToU32(colour),
                glyph);
        }
        finally
        {
            ImGui.PopFont();
        }
    }

    private static (string Title, string Description) GetModernPageMetadata(
        SentinelHudModernContentPage page)
        => page switch
        {
            SentinelHudModernContentPage.General => ("General", "Global state, editing mode and shared HUD behaviour."),
            SentinelHudModernContentPage.Player => ("Player", "Choose exactly what your own compact actor panel displays."),
            SentinelHudModernContentPage.Target => ("Target", "Target data, casting information and native target-bar overlay."),
            SentinelHudModernContentPage.FocusTarget => ("Focus Target", "Focus information and the focus target's current target."),
            SentinelHudModernContentPage.TargetOfTarget => ("Target-of-Target", "A compact, independently positioned represented-actor panel."),
            SentinelHudModernContentPage.Layout => ("Layout", "Visual editor, reset tools and appearance-copy actions."),
            SentinelHudModernContentPage.Awareness => ("Awareness", "Self visibility, true-position marker and incoming PvP attention."),
            SentinelHudModernContentPage.EncounterAwareness => ("Encounter Awareness", "Trusted hazard providers and exact-position danger feedback."),
            SentinelHudModernContentPage.Camera => ("Camera", "Optional extended third-person zoom with conflict-safe restoration."),
            SentinelHudModernContentPage.Convenience => ("Questing / Convenience", "Independent opt-in dialogue, cutscene, reward and AFK helpers."),
            SentinelHudModernContentPage.Appearance => ("Appearance", "Sentinel presentation and shared bar-colour choices."),
            _ => ("Diagnostics", "Live state and compact evidence for troubleshooting."),
        };

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        activeClassicStyleScope?.Dispose();
        activeClassicStyleScope = null;
        modernStyle.Dispose();
        modernShellState.Dispose();
    }

    private void DrawGeneral()
    {
        var config = configuration.Current;
        DrawToggle("Enable Sentinel HUD", config.Enabled, value => Update(c => c.Enabled = value));
        if (DrawToggle("Lock HUD", config.Locked, value => Update(c => c.Locked = value)))
            renderer.RequestRepositionAll();

        var globalScale = config.GlobalScale;
        if (ImGui.SliderFloat("Global scale", ref globalScale, 0.65f, 1.75f, "%.2fx"))
        {
            Update(c => c.GlobalScale = globalScale);
            renderer.RequestRepositionAll();
        }
        var globalOpacity = config.GlobalOpacity;
        if (ImGui.SliderFloat("Global opacity", ref globalOpacity, 0.2f, 1f, "%.0f%%"))
            Update(c => c.GlobalOpacity = globalOpacity);

        ImGui.Spacing();
        ImGui.TextWrapped(config.Locked
            ? "Gameplay mode is active. Enabled interaction regions use left-click to target and right-click for FFXIV's native actor menu; other HUD space remains click-through."
            : "Visual editing is active. Drag the panel body to move it, any edge to resize one axis, or a corner to resize width and bar height together. Actor actions are disabled.");
    }

    private void DrawPlayer()
    {
        var config = configuration.Current.Player;
        DrawModuleVisibility(HudModuleKind.Player, config);
        if (OpenSection("Information"))
        {
            DrawToggle("Show character name", config.ShowName, value => Update(c => c.Player.ShowName = value));
            DrawToggle("Show job", config.ShowJob, value => Update(c => c.Player.ShowJob = value));
            DrawToggle("Show role", config.ShowRole, value => Update(c => c.Player.ShowRole = value));
            DrawToggle("Show level", config.ShowLevel, value => Update(c => c.Player.ShowLevel = value));
            DrawHpToggles(config.ShowCurrentHp, config.ShowMaximumHp, config.ShowHpPercentage,
                value => Update(c => c.Player.ShowCurrentHp = value),
                value => Update(c => c.Player.ShowMaximumHp = value),
                value => Update(c => c.Player.ShowHpPercentage = value));
            DrawMpMode(config.MpDisplay, value => Update(c => c.Player.MpDisplay = value));
            DrawShieldMode(config.ShieldDisplay, value => Update(c => c.Player.ShieldDisplay = value));
            DrawToggle("Show cast name", config.ShowCastName,
                value => Update(c => c.Player.ShowCastName = value));
            DrawToggle("Show cast bar", config.ShowCastBar,
                value => Update(c => c.Player.ShowCastBar = value));
            DrawToggle("Show cast percentage", config.ShowCastPercentage,
                value => Update(c => c.Player.ShowCastPercentage = value));
            DrawToggle("Show remaining cast time", config.ShowCastRemainingTime,
                value => Update(c => c.Player.ShowCastRemainingTime = value));
            DrawToggle("Show statuses (first five)", config.ShowStatuses,
                value => Update(c => c.Player.ShowStatuses = value));
        }
        DrawModuleSizeLayout(HudModuleKind.Player, config);
        DrawModuleAppearance(HudModuleKind.Player, config);
    }

    private void DrawTarget()
    {
        var config = configuration.Current.Target;
        DrawModuleVisibility(HudModuleKind.Target, config);
        if (OpenSection("Information"))
        {
            DrawToggle("Show target name", config.ShowName, value => Update(c => c.Target.ShowName = value));
            DrawToggle("Show job for player targets", config.ShowJob, value => Update(c => c.Target.ShowJob = value));
            DrawToggle("Show role for player targets", config.ShowRole, value => Update(c => c.Target.ShowRole = value));
            DrawToggle("Show level", config.ShowLevel, value => Update(c => c.Target.ShowLevel = value));
            DrawHpToggles(config.ShowCurrentHp, config.ShowMaximumHp, config.ShowHpPercentage,
                value => Update(c => c.Target.ShowCurrentHp = value),
                value => Update(c => c.Target.ShowMaximumHp = value),
                value => Update(c => c.Target.ShowHpPercentage = value));
            DrawMpMode(config.MpDisplay, value => Update(c => c.Target.MpDisplay = value));
            DrawToggle("Show distance", config.ShowDistance, value => Update(c => c.Target.ShowDistance = value));
            DrawShieldMode(config.ShieldDisplay, value => Update(c => c.Target.ShieldDisplay = value));
            DrawToggle("Show cast name", config.ShowCastName, value => Update(c => c.Target.ShowCastName = value));
            DrawToggle("Show cast bar", config.ShowCastBar, value => Update(c => c.Target.ShowCastBar = value));
            DrawToggle("Show cast percentage", config.ShowCastPercentage,
                value => Update(c => c.Target.ShowCastPercentage = value));
            DrawToggle("Show statuses (first five)", config.ShowStatuses,
                value => Update(c => c.Target.ShowStatuses = value));
        }
        DrawModuleSizeLayout(HudModuleKind.Target, config);
        DrawModuleAppearance(HudModuleKind.Target, config);
        DrawNativeTargetOverlay(config.NativeHpOverlay);
    }

    private void DrawFocusTarget()
    {
        var config = configuration.Current.FocusTarget;
        DrawModuleVisibility(HudModuleKind.FocusTarget, config);
        if (OpenSection("Information"))
        {
            DrawToggle("Show focus target name", config.ShowName,
                value => Update(c => c.FocusTarget.ShowName = value));
            DrawHpToggles(config.ShowCurrentHp, config.ShowMaximumHp, config.ShowHpPercentage,
                value => Update(c => c.FocusTarget.ShowCurrentHp = value),
                value => Update(c => c.FocusTarget.ShowMaximumHp = value),
                value => Update(c => c.FocusTarget.ShowHpPercentage = value));
            DrawMpMode(config.MpDisplay, value => Update(c => c.FocusTarget.MpDisplay = value));
            DrawToggle("Show distance", config.ShowDistance,
                value => Update(c => c.FocusTarget.ShowDistance = value));
            DrawShieldMode(config.ShieldDisplay,
                value => Update(c => c.FocusTarget.ShieldDisplay = value));
            DrawToggle("Show cast name", config.ShowCastName,
                value => Update(c => c.FocusTarget.ShowCastName = value));
            DrawToggle("Show cast bar", config.ShowCastBar,
                value => Update(c => c.FocusTarget.ShowCastBar = value));
            DrawToggle("Show cast percentage", config.ShowCastPercentage,
                value => Update(c => c.FocusTarget.ShowCastPercentage = value));
        }
        if (OpenSection("Focus Target's Target"))
        {
            var targetOfFocus = config.TargetOfFocus;
            ImGui.TextWrapped("Shows the actor currently targeted by your Focus Target when that actor is present in the client object table.");
            DrawToggle("Show##FocusToT", targetOfFocus.Show,
                value => Update(c => c.FocusTarget.TargetOfFocus.Show = value));
            DrawToggle("Show name##FocusToT", targetOfFocus.ShowName,
                value => Update(c => c.FocusTarget.TargetOfFocus.ShowName = value));
            DrawToggle("Show HP##FocusToT", targetOfFocus.ShowCurrentHp,
                value => Update(c => c.FocusTarget.TargetOfFocus.ShowCurrentHp = value));
            DrawToggle("Show HP percentage##FocusToT", targetOfFocus.ShowHpPercentage,
                value => Update(c => c.FocusTarget.TargetOfFocus.ShowHpPercentage = value));
            DrawToggle("Show job when applicable##FocusToT", targetOfFocus.ShowJob,
                value => Update(c => c.FocusTarget.TargetOfFocus.ShowJob = value));
            DrawToggle("Show level##FocusToT", targetOfFocus.ShowLevel,
                value => Update(c => c.FocusTarget.TargetOfFocus.ShowLevel = value));
            DrawToggle("Click to target##FocusToT", targetOfFocus.ClickToTarget,
                value => Update(c => c.FocusTarget.TargetOfFocus.ClickToTarget = value));
            DrawToggle("Right-click native context menu##FocusToT",
                targetOfFocus.RightClickContextMenu,
                value => Update(c => c.FocusTarget.TargetOfFocus.RightClickContextMenu = value));
            ImGui.TextDisabled("Only the visible target row receives mouse input; the rest of a locked module stays click-through.");
        }
        DrawModuleSizeLayout(HudModuleKind.FocusTarget, config);
        DrawModuleAppearance(HudModuleKind.FocusTarget, config);
    }

    private void DrawTargetOfTarget()
    {
        var config = configuration.Current.TargetOfTarget;
        DrawModuleVisibility(HudModuleKind.TargetOfTarget, config);
        if (OpenSection("Information"))
        {
            DrawToggle("Show name", config.ShowName, value => Update(c => c.TargetOfTarget.ShowName = value));
            DrawHpToggles(config.ShowCurrentHp, config.ShowMaximumHp, config.ShowHpPercentage,
                value => Update(c => c.TargetOfTarget.ShowCurrentHp = value),
                value => Update(c => c.TargetOfTarget.ShowMaximumHp = value),
                value => Update(c => c.TargetOfTarget.ShowHpPercentage = value));
            DrawToggle("Show distance", config.ShowDistance,
                value => Update(c => c.TargetOfTarget.ShowDistance = value));
        }
        DrawModuleSizeLayout(HudModuleKind.TargetOfTarget, config);
        DrawModuleAppearance(HudModuleKind.TargetOfTarget, config);
    }

    private void DrawAwareness()
    {
        DrawSectionHeader("Self Highlight");
        var highlight = configuration.Current.SelfHighlight;
        ImGui.TextWrapped("Uses FFXIV's native model-conforming silhouette renderer without changing any targeting state.");
        var mode = (int)highlight.Mode;
        if (DrawComboSetting(
                "SelfHighlight.Mode",
                "Mode",
                "Choose when the model-conforming self highlight is visible.",
                ref mode,
                HighlightModes))
            Update(c => c.SelfHighlight.Mode = (SelfHighlightMode)mode);
        DrawHighlightColour(highlight.ColourPreset, highlight.CustomColour, "SelfHighlight",
            value => Update(c => c.SelfHighlight.ColourPreset = value),
            value => Update(c => c.SelfHighlight.CustomColour.Set(value)));
        var nativeSelection = NativeHighlightPolicy.Resolve(highlight);
        ImGui.TextWrapped(nativeSelection.IsSupported
            ? highlight.ColourPreset == HighlightColourPreset.Custom
                ? $"Applied native silhouette palette colour: {nativeSelection.DisplayName}. The requested custom RGB remains saved and visible in the picker."
                : $"Applied native colour: {nativeSelection.DisplayName}."
            : "White is not substituted: the native silhouette is disabled because the current FFXIV outline palette has no White entry.");
        ImGui.TextDisabled("The model-conforming renderer exposes Red, Green, Blue, Yellow, Orange, Magenta and Black. Custom uses the closest real native palette colour (shown above); it never silently reports the requested RGB as exact.");
        ImGui.TextDisabled($"Runtime state: {renderer.SelfHighlightState}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawSectionHeader("Player Position Marker");
        var marker = configuration.Current.PlayerPositionMarker;
        var markerMode = (int)marker.Mode;
        if (DrawComboSetting(
                "PositionMarker.Mode",
                "Mode",
                "Choose when the exact-position marker is visible.",
                ref markerMode,
                HighlightModes))
            Update(c => c.PlayerPositionMarker.Mode = (SelfHighlightMode)markerMode);
        var markerStyle = (int)marker.Style;
        if (DrawComboSetting(
                "PositionMarker.Style",
                "Style",
                "Camera Facing stays round and readable; Ground Projected follows the terrain plane.",
                ref markerStyle,
                MarkerStyles))
            Update(c => c.PlayerPositionMarker.Style = (PlayerPositionMarkerStyle)markerStyle);
        ImGui.TextDisabled(marker.Style == PlayerPositionMarkerStyle.CameraFacing
            ? "Camera Facing uses the exact actor world origin with no terrain snap, then keeps the dot circular and readable on screen."
            : "Ground Projected follows the terrain plane and naturally flattens at shallow camera angles.");
        DrawHighlightColour(marker.ColourPreset, marker.CustomColour, "PositionMarker",
            value => Update(c => c.PlayerPositionMarker.ColourPreset = value),
            value => Update(c => c.PlayerPositionMarker.CustomColour.Set(value)));

        var radius = marker.Radius;
        if (DrawSliderSetting(
                "PositionMarker.Radius",
                "Marker radius",
                "Total outside marker radius, including its optional border.",
                ref radius,
                PlayerPositionMarkerPolicy.MinimumRadius,
                PlayerPositionMarkerPolicy.MaximumRadius,
                marker.Style == PlayerPositionMarkerStyle.CameraFacing ? "%.2f size" : "%.2f yalms"))
            Update(c => c.PlayerPositionMarker.Radius = radius);
        ImGui.TextDisabled(marker.Style == PlayerPositionMarkerStyle.CameraFacing
            ? $"Total outside radius: {PlayerPositionMarkerPolicy.ResolveCameraFacingRadius(marker.Radius):0.0} px, including any border."
            : "Radius is the total outside world radius, including any border.");
        var opacity = marker.Opacity;
        if (DrawSliderSetting(
                "PositionMarker.Opacity",
                "Marker opacity",
                null,
                ref opacity,
                0.1f,
                1f,
                "%.2f"))
            Update(c => c.PlayerPositionMarker.Opacity = opacity);
        DrawToggle("Thin contrasting border", marker.ShowBorder,
            value => Update(c => c.PlayerPositionMarker.ShowBorder = value));
        if (marker.ShowBorder)
        {
            var thickness = marker.BorderThickness;
            if (DrawSliderSetting(
                    "PositionMarker.BorderThickness",
                    "Border thickness",
                    "Drawn inward so it never enlarges the configured marker radius.",
                    ref thickness,
                    PlayerPositionMarkerPolicy.MinimumBorderThickness,
                    PlayerPositionMarkerPolicy.MaximumBorderThickness, "%.2f px"))
                Update(c => c.PlayerPositionMarker.BorderThickness = thickness);
            ImGui.TextDisabled("The border is drawn inward and does not increase the configured radius.");
        }
        var markerPreview = PlayerPositionMarkerPolicy.ResolveColour(marker);
        ImGui.TextUnformatted("Marker preview:");
        ImGui.SameLine();
        ImGui.ColorButton("Position marker preview##SentinelHUD", markerPreview);
        ImGui.TextDisabled($"Runtime state: {renderer.PositionMarkerState}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawSectionHeader("Threat Awareness");
        var threat = configuration.Current.TargetingMeCounter;
        DrawToggle("Enable Targeting Me Counter", threat.Enabled,
            value => Update(c => c.TargetingMeCounter.Enabled = value));
        ImGui.TextWrapped("Counts currently observed enemy PvP players whose hard target is you. It cannot detect soft targets, mouseover, queued attacks, future intent, or enemies outside the client-resolved actor set.");
        var threatVisibility = (int)threat.PvPVisibility;
        if (DrawComboSetting(
                "TargetingMe.PvPVisibility",
                "PvP visibility",
                null,
                ref threatVisibility,
                PvPThreatVisibilityModes))
            Update(c => c.TargetingMeCounter.PvPVisibility = (PvPThreatVisibility)threatVisibility);
        DrawToggle("Hide when count is 0", threat.HideWhenZero,
            value => Update(c => c.TargetingMeCounter.HideWhenZero = value));
        DrawToggle("Show targeter jobs", threat.ShowJobs,
            value => Update(c => c.TargetingMeCounter.ShowJobs = value));
        DrawToggle("Show targeter name / job / distance details", threat.ShowTargeterDetails,
            value => Update(c => c.TargetingMeCounter.ShowTargeterDetails = value));

        var threatScale = threat.Scale;
        if (DrawSliderSetting(
                "TargetingMe.Scale", "Counter scale", null,
                ref threatScale, 0.6f, 1.8f, "%.2fx"))
        {
            Update(c => c.TargetingMeCounter.Scale = threatScale);
            renderer.RequestReposition(HudModuleKind.TargetingMeCounter);
        }
        var threatWidth = threat.Width;
        if (DrawSliderSetting(
                "TargetingMe.Width", "Counter width", null,
                ref threatWidth,
                HudSizingPolicy.MinimumWidth, HudSizingPolicy.MaximumWidth, "%.0f px"))
        {
            Update(c => c.TargetingMeCounter.Width = threatWidth);
            renderer.RequestReposition(HudModuleKind.TargetingMeCounter);
        }
        var threatOpacity = threat.Opacity;
        if (DrawSliderSetting(
                "TargetingMe.Opacity", "Counter opacity", null,
                ref threatOpacity, 0.15f, 1f, "%.0f%%"))
            Update(c => c.TargetingMeCounter.Opacity = threatOpacity);
        var numberSize = threat.NumberSize;
        if (DrawSliderSetting(
                "TargetingMe.NumberSize", "Counter number size", null,
                ref numberSize, 24f, 120f, "%.0f px"))
            Update(c => c.TargetingMeCounter.NumberSize = numberSize);
        var jobSize = threat.JobTextSize;
        if (DrawSliderSetting(
                "TargetingMe.JobTextSize", "Job text size", null,
                ref jobSize, 10f, 40f, "%.0f px"))
            Update(c => c.TargetingMeCounter.JobTextSize = jobSize);
        if (threat.ShowTargeterDetails)
        {
            var detailSize = threat.DetailTextSize;
            if (DrawSliderSetting(
                    "TargetingMe.DetailTextSize", "Detail text size", null,
                    ref detailSize, 10f, 28f, "%.0f px"))
                Update(c => c.TargetingMeCounter.DetailTextSize = detailSize);
        }

        if (OpenSection("Threat warning colours"))
        {
            DrawColour("Normal / low", threat.NormalColour,
                value => Update(c => c.TargetingMeCounter.NormalColour.Set(value)));
            DrawColour("Moderate", threat.ModerateColour,
                value => Update(c => c.TargetingMeCounter.ModerateColour.Set(value)));
            DrawColour("High", threat.HighColour,
                value => Update(c => c.TargetingMeCounter.HighColour.Set(value)));
            DrawColour("Extreme", threat.ExtremeColour,
                value => Update(c => c.TargetingMeCounter.ExtremeColour.Set(value)));
            ImGui.TextDisabled("Preserves PvP Sentinel threat semantics: nearby enemy density can raise the warning level even before every enemy hard-targets you.");
        }
        if (ImGui.Button("Reset Targeting Me Counter position"))
            renderer.ResetModuleLayout(HudModuleKind.TargetingMeCounter);
        ImGui.TextDisabled($"Runtime: {renderer.PvPThreat.Explanation}");
    }

    private void DrawEncounterAwareness()
    {
        var encounter = configuration.Current.EncounterAwareness;
        var marker = configuration.Current.PlayerPositionMarker;
        DrawSectionHeader("General");
        DrawToggle("Enable Encounter Awareness", encounter.Enabled,
            value => Update(c => c.EncounterAwareness.Enabled = value));
        ImGui.TextWrapped("Hazards are isolated behind providers. The marker tests the local player's actual world-position point against cached world-space geometry.");

        ImGui.Spacing();
        DrawSectionHeader("Player Danger");
        DrawToggle("Change Position Marker colour when unsafe", marker.DangerDetectionEnabled,
            value => Update(c => c.PlayerPositionMarker.DangerDetectionEnabled = value));
        var dangerPreset = (int)marker.DangerColourPreset;
        if (DrawComboSetting(
                "Encounter.DangerColour", "Danger colour", null,
                ref dangerPreset, DangerColours))
            Update(c => c.PlayerPositionMarker.DangerColourPreset = (DangerColourPreset)dangerPreset);
        if (marker.DangerColourPreset == DangerColourPreset.Custom)
        {
            var custom = new Vector3(marker.CustomDangerColour.Red,
                marker.CustomDangerColour.Green, marker.CustomDangerColour.Blue);
            if (DrawColourSetting(
                    "Encounter.CustomDangerColour",
                    "Custom danger colour",
                    null,
                    ref custom))
                Update(c => c.PlayerPositionMarker.CustomDangerColour.Set(new Vector4(custom, 1f)));
        }
        var dangerPreview = PlayerPositionMarkerPolicy.ResolveColour(marker, true);
        ImGui.TextUnformatted("Danger preview:");
        ImGui.SameLine();
        ImGui.ColorButton("Danger preview##SentinelHUD", dangerPreview);
        ImGui.TextUnformatted($"Player currently unsafe: {renderer.PlayerInDanger}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawSectionHeader("Native Detection");
        DrawToggle("Enable conservative visible-cast detection", encounter.NativeDetectionEnabled,
            value => Update(c => c.EncounterAwareness.NativeDetectionEnabled = value));
        ImGui.TextWrapped("Uses hostile casts only when current action data exposes a supported standard circle, donut, rectangle, cone, line or cross and a native omen/telegraph. Unknown and boss-specific mechanics are skipped rather than guessed.");
        ImGui.TextDisabled($"Status: {renderer.EncounterNativeState}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawSectionHeader("Splatoon Integration");
        DrawToggle("Enable optional Splatoon IPC", encounter.SplatoonIntegrationEnabled,
            value => Update(c => c.EncounterAwareness.SplatoonIntegrationEnabled = value));
        DrawToggle("Treat unclassified visible Splatoon geometry as danger",
            encounter.TreatUnclassifiedSplatoonGeometryAsDanger,
            value => Update(c => c.EncounterAwareness.TreatUnclassifiedSplatoonGeometryAsDanger = value));
        ImGui.TextWrapped("Splatoon's current geometry IPC v1 exposes active shapes but not whether each is Danger, Safe or Information. Sentinel fails closed by default. The opt-in above treats every supported visible area as dangerous and may therefore flag safe-zone drawings.");
        ImGui.TextUnformatted($"Installed / connected: {renderer.SplatoonInstalled} / {renderer.SplatoonConnected}");
        ImGui.TextDisabled($"Status: {renderer.EncounterSplatoonState}");
        ImGui.TextUnformatted($"Active encounter: {renderer.ActiveEncounter}");
        ImGui.TextUnformatted($"Current trusted hazards: {renderer.EncounterHazardCount}");
    }

    private void DrawConvenience()
    {
        var convenience = configuration.Current.Convenience;
        DrawSectionHeader("Questing / Convenience");
        DrawToggle("Skip Dialogue", convenience.SkipDialogue,
            value => Update(c => c.Convenience.SkipDialogue = value));
        ImGui.TextWrapped("Advances only the ordinary Talk text box. Sentinel pauses whenever a response list or Yes/No prompt is visible and never chooses a dialogue response.");
        ImGui.TextDisabled($"Dialogue runtime: {questConvenience.DialogueState}");

        DrawToggle("Skip Cutscenes", convenience.SkipCutscenes,
            value => Update(c => c.Convenience.SkipCutscenes = value));
        ImGui.TextWrapped("Requests FFXIV's normal skip dialog only when the client exposes a skippable cutscene, then confirms that dedicated dialog. Protected and unskippable cutscenes are left alone.");
        ImGui.TextDisabled($"Cutscene runtime: {questConvenience.CutsceneState}");
        if (questConvenience.AwaitingCutsceneSkipConfirmation)
            ImGui.TextDisabled("Confirmation state: Awaiting the game-provided cutscene skip prompt");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawSectionHeader("Quest Reward Selection");
        var rewardMode = (int)convenience.QuestRewardSelection;
        if (DrawComboSetting(
                "Convenience.RewardSelection",
                "Reward selection",
                "Manual never chooses a reward; automatic modes use only positively identified choose-one entries.",
                ref rewardMode,
                QuestRewardModes))
            Update(c => c.Convenience.QuestRewardSelection = (QuestRewardSelectionMode)rewardMode);
        ImGui.TextWrapped("Manual never touches rewards. Automatic modes act only on positively identified choose-one entries in JournalResult; guaranteed gil, EXP, items and unlocks are not selected or altered.");
        ImGui.TextDisabled($"Reward runtime: {questConvenience.RewardState}");
        ImGui.TextDisabled($"Current job: {questConvenience.CurrentJob}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawSectionHeader("Prevent AFK Disconnect");
        DrawToggle("Prevent AFK Disconnect", convenience.PreventAfkDisconnect,
            value => Update(c => c.Convenience.PreventAfkDisconnect = value));
        ImGui.TextWrapped("Default Off. When enabled, Sentinel periodically resets the client's inactivity timers directly. It does not move your character, send chat, synthesize keys, or alter controller input. Disabling resumes ordinary timer accumulation.");
        ImGui.TextDisabled($"Runtime state: {renderer.AntiAfkState}");
    }

    private void DrawCamera()
    {
        DrawSectionHeader("Extended Zoom");
        var camera = configuration.Current.Camera;
        DrawToggle("Extended zoom enabled", camera.Enabled, value => Update(c => c.Camera.Enabled = value));
        var maximum = camera.MaximumZoomDistance;
        if (DrawSliderSetting(
                "Camera.MaximumZoom",
                "Maximum zoom distance",
                "Sets the normal third-person zoom ceiling while the feature is enabled.",
                ref maximum,
                CameraZoomPolicy.StockMaximum,
                CameraZoomPolicy.MaximumSupported, "%.1f yalms"))
            Update(c => c.Camera.MaximumZoomDistance = maximum);
        ImGui.TextWrapped("Changes only the normal third-person maximum. Your live zoom distance is restored through death/respawn. It pauses for first person, GPose, cutscenes, transitions, and known camera-control plugins.");
        if (ImGui.Button("Disable and restore normal camera limits"))
        {
            Update(c => c.Camera.Enabled = false);
            renderer.RestoreCameraDefaults();
        }
        ImGui.SameLine();
        if (ImGui.Button("Retry after conflict"))
            renderer.RetryCameraZoom();
        ImGui.TextUnformatted($"Runtime state: {renderer.CameraZoomState}");
        ImGui.TextUnformatted($"Current maximum: {renderer.CameraCurrentMaximum:0.0} yalms");
        if (renderer.CameraConflict is not null)
            ImGui.TextWrapped($"Camera controller detected: {renderer.CameraConflict}. Sentinel HUD will not compete with it.");
    }

    private void DrawAppearance()
    {
        var appearance = configuration.Current.Appearance;
        DrawSectionHeader("Bar Colours");
        var playerMode = (int)appearance.PlayerHpColourMode;
        if (DrawComboSetting(
                "Appearance.PlayerHpColourMode",
                "Player HP colour mode",
                null,
                ref playerMode,
                HpColourModes))
            Update(c => c.Appearance.PlayerHpColourMode = (HpColourMode)playerMode);
        var targetMode = (int)appearance.TargetHpColourMode;
        if (DrawComboSetting(
                "Appearance.TargetHpColourMode",
                "Target / Focus / ToT HP colour mode",
                null,
                ref targetMode,
                HpColourModes))
            Update(c => c.Appearance.TargetHpColourMode = (HpColourMode)targetMode);
        ImGui.TextDisabled("Gradient: green at high HP, yellow through mid HP, red at 35% and below.");
        ImGui.Spacing();
        DrawColour("Player HP", appearance.PlayerHealth, value => Update(c => c.Appearance.PlayerHealth.Set(value)));
        DrawColour("Friendly HP", appearance.FriendlyHealth, value => Update(c => c.Appearance.FriendlyHealth.Set(value)));
        DrawColour("Hostile HP", appearance.HostileHealth, value => Update(c => c.Appearance.HostileHealth.Set(value)));
        DrawColour("Neutral HP", appearance.NeutralHealth, value => Update(c => c.Appearance.NeutralHealth.Set(value)));
        DrawColour("Shield", appearance.Shield, value => Update(c => c.Appearance.Shield.Set(value)));
        DrawColour("MP", appearance.Mp, value => Update(c => c.Appearance.Mp.Set(value)));
        ImGui.Spacing();
        ImGui.TextWrapped("In Static / Role-Based mode, hostile characters use red; players, party/alliance members and friends use the friendly colour; other objects use neutral. Health-State Gradient intentionally overrides disposition colours.");
        ImGui.TextDisabled("Background, border, number style and alignment remain independently configurable per module.");
    }

    private void DrawLayout()
    {
        var config = configuration.Current;
        ImGui.TextWrapped("Unlock the HUD to open the visual editor. Drag the body to move, horizontal edges for width, vertical edges for bar height, or a corner for both. Text is reflowed and never stretched. The titleless origin remains identical in both modes.");
        if (ImGui.Button(config.Locked ? "Unlock HUD" : "Lock HUD"))
        {
            Update(c => c.Locked = !c.Locked);
            renderer.RequestRepositionAll();
        }
        if (!modernThemeActive)
            ImGui.SetNextItemWidth(220f);
        DrawComboSetting(
            "Layout.SelectedModule",
            "Selected module",
            "Reset or copy appearance from this module.",
            ref selectedLayoutModule,
            ModuleNames);
        var kind = (HudModuleKind)selectedLayoutModule;
        if (ImGui.Button("Reset selected module"))
            renderer.ResetModuleLayout(kind);
        ImGui.SameLine();
        if (ImGui.Button("Reset complete HUD layout"))
            renderer.ResetAllLayouts();
        if (ImGui.Button("Copy selected appearance to other modules"))
            renderer.CopyAppearanceToOtherModules(kind);
        ImGui.TextDisabled("Copies scale, width, bar height, opacity, border, HP alignment and number format only.");

        ImGui.Separator();
        ImGui.TextDisabled("Saved normalized anchors");
        foreach (var moduleKind in Enum.GetValues<HudModuleKind>())
        {
            var module = GetModule(configuration.Current, moduleKind);
            ImGui.BulletText($"{ModuleNames[(int)moduleKind]}: {module.Layout.AnchorX:0.000}, {module.Layout.AnchorY:0.000}");
        }
    }

    private void DrawDiagnostics()
    {
        var config = configuration.Current;
        ImGui.TextUnformatted($"Configuration schema: {config.Version}");
        ImGui.TextWrapped($"Configuration load: {configurationStore.StateDescription}");
        if (configurationStore.BackupPath is not null)
            ImGui.TextWrapped($"Migration/recovery backup: {configurationStore.BackupPath}");
        ImGui.TextUnformatted($"HUD enabled / locked: {config.Enabled} / {config.Locked}");
        ImGui.TextUnformatted($"Player module visible: {renderer.PlayerVisible}");
        ImGui.TextUnformatted($"Target visible / resolved: {renderer.TargetVisible} / {renderer.TargetResolved}");
        ImGui.TextUnformatted($"Focus visible / resolved: {renderer.FocusTargetVisible} / {renderer.FocusTargetResolved}");
        ImGui.TextUnformatted($"Focus Target's Target resolved: {renderer.FocusTargetTargetResolved}");
        ImGui.TextWrapped($"Focus Target click state: {renderer.FocusTargetClickState}");
        ImGui.TextWrapped($"Actor context-menu state: {renderer.ActorContextMenuState}");
        ImGui.TextUnformatted($"Native context menu visible: {renderer.NativeActorContextMenuVisible}");
        ImGui.TextUnformatted($"Actor input suspended for menu: {renderer.ActorInputSuspendedForContextMenu}");
        ImGui.TextWrapped($"Context-menu placement: {renderer.ActorContextMenuPlacementState}");
        if (renderer.NativeActorContextMenuVisible)
        {
            ImGui.TextUnformatted($"Native menu position: {renderer.ActorContextMenuPosition.X:0}, {renderer.ActorContextMenuPosition.Y:0}");
            ImGui.TextUnformatted($"Native menu size: {renderer.ActorContextMenuSize.X:0} × {renderer.ActorContextMenuSize.Y:0}");
        }
        ImGui.TextUnformatted($"Target-of-target visible / resolved: {renderer.TargetOfTargetVisible} / {renderer.TargetOfTargetResolved}");
        ImGui.TextUnformatted($"Self highlight mode / active: {config.SelfHighlight.Mode} / {renderer.SelfHighlightActive}");
        ImGui.TextUnformatted($"Self highlight applied colour: {renderer.SelfHighlightAppliedColour}");
        ImGui.TextWrapped($"Self highlight state: {renderer.SelfHighlightState}");
        ImGui.TextUnformatted($"Position marker mode / style / active: {config.PlayerPositionMarker.Mode} / {config.PlayerPositionMarker.Style} / {renderer.PositionMarkerActive}");
        ImGui.TextUnformatted($"Position marker terrain projection: {renderer.PositionMarkerUsedTerrainProjection}");
        ImGui.TextWrapped($"Position marker state: {renderer.PositionMarkerState}");
        ImGui.TextUnformatted($"Encounter awareness enabled / player unsafe: {renderer.EncounterAwarenessEnabled} / {renderer.PlayerInDanger}");
        ImGui.TextUnformatted($"Encounter hazards: {renderer.EncounterHazardCount}");
        ImGui.TextWrapped($"Native danger provider: {renderer.EncounterNativeState}");
        ImGui.TextWrapped($"Splatoon provider: {renderer.EncounterSplatoonState}");
        ImGui.TextUnformatted($"Native target overlay mode / active: {config.Target.NativeHpOverlay.Mode} / {renderer.NativeTargetOverlayActive}");
        ImGui.TextUnformatted($"Native overlay target exists: {renderer.NativeTargetOverlayTargetExists}");
        ImGui.TextUnformatted($"Split addon available / visible: {renderer.NativeTargetSplitAddonAvailable} / {renderer.NativeTargetSplitAddonVisible}");
        ImGui.TextUnformatted($"Combined addon available / visible: {renderer.NativeTargetCombinedAddonAvailable} / {renderer.NativeTargetCombinedAddonVisible}");
        ImGui.TextUnformatted($"Detected layout / addon: {renderer.NativeTargetDetectedLayout} / {renderer.NativeTargetDetectedAddon}");
        ImGui.TextUnformatted($"Anchor source: {renderer.NativeTargetAnchorSource}");
        ImGui.TextWrapped($"Native target overlay state: {renderer.NativeTargetOverlayState}");
        if (renderer.NativeTargetOverlayActive)
        {
            ImGui.TextUnformatted($"Native target anchor: {renderer.NativeTargetOverlayAnchor.X:0.0}, {renderer.NativeTargetOverlayAnchor.Y:0.0}");
            ImGui.TextUnformatted($"Native target bounds: {renderer.NativeTargetOverlaySize.X:0.0} × {renderer.NativeTargetOverlaySize.Y:0.0}");
        }
        ImGui.TextUnformatted($"Extended zoom enabled / active: {config.Camera.Enabled} / {renderer.CameraZoomActive}");
        ImGui.TextWrapped($"Extended zoom state: {renderer.CameraZoomState}");
        ImGui.TextUnformatted($"Prevent AFK Disconnect enabled / active: {config.Convenience.PreventAfkDisconnect} / {renderer.AntiAfkActive}");
        ImGui.TextWrapped($"Anti-AFK state: {renderer.AntiAfkState}");
        ImGui.TextUnformatted($"Dialogue skipping enabled: {config.Convenience.SkipDialogue}");
        ImGui.TextWrapped($"Dialogue skipping state: {questConvenience.DialogueState}");
        ImGui.TextUnformatted($"Cutscene skipping enabled: {config.Convenience.SkipCutscenes}");
        ImGui.TextWrapped($"Cutscene skipping state: {questConvenience.CutsceneState}");
        ImGui.TextUnformatted($"Awaiting cutscene confirmation: {questConvenience.AwaitingCutsceneSkipConfirmation}");
        ImGui.TextWrapped($"Detected cutscene addon: {questConvenience.CutsceneDetectedAddon}");
        ImGui.TextWrapped($"Last cutscene result: {questConvenience.CutsceneLastResult}");
        ImGui.TextUnformatted($"Quest reward mode: {config.Convenience.QuestRewardSelection}");
        ImGui.TextUnformatted($"Current job: {questConvenience.CurrentJob}");
        ImGui.TextUnformatted($"Reward window detected: {questConvenience.RewardWindowDetected}");
        ImGui.TextWrapped($"Reward runtime: {questConvenience.RewardState}");
        ImGui.TextWrapped($"Last selected reward: {questConvenience.LastSelectedRewardName} ({questConvenience.LastSelectedRewardId})");
        ImGui.TextWrapped($"Last selection reason: {questConvenience.LastSelectionReason}");
        ImGui.Separator();
        ImGui.TextUnformatted($"Targeting Me Counter visible / active: {renderer.TargetingMeCounterVisible} / {renderer.PvPThreat.Active}");
        ImGui.TextUnformatted($"PvP mode: {renderer.PvPThreat.PvPMode}");
        ImGui.TextUnformatted($"Local Battalion/team: {(renderer.PvPThreat.LocalBattalion <= 2 ? renderer.PvPThreat.LocalBattalion.ToString() : "UNRESOLVED")}");
        ImGui.TextUnformatted($"Classification source / authoritative: {renderer.PvPThreat.Source} / {renderer.PvPThreat.ClassificationAuthoritative}");
        ImGui.TextUnformatted($"Observed players / enemies: {renderer.PvPThreat.ObservedPlayerCount} / {renderer.PvPThreat.ObservedEnemyCount}");
        ImGui.TextUnformatted($"Nearby enemies / allies: {renderer.PvPThreat.NearbyEnemyCount} / {renderer.PvPThreat.NearbyFriendlyCount}");
        ImGui.TextUnformatted($"Currently targeting me: {renderer.PvPThreat.TargeterCount}");
        ImGui.TextUnformatted($"Threat warning level: {renderer.PvPThreat.Level}");
        ImGui.TextWrapped($"Classification state: {renderer.PvPThreat.Explanation}");
        if (renderer.PvPThreat.Targeters.Count > 0
            && ImGui.TreeNode($"Observed hard targeters ({renderer.PvPThreat.TargeterCount})"))
        {
            foreach (var targeter in renderer.PvPThreat.Targeters)
                ImGui.BulletText($"{targeter.JobAbbreviation} — {targeter.Name}, {targeter.Distance:0.0}y");
            ImGui.TreePop();
        }
        if (ImGui.Button("Clear diagnostic history"))
            diagnostics.Clear();
        ImGui.Separator();
        if (ImGui.BeginChild("SentinelHUD-DiagnosticHistory", Vector2.Zero, true))
        {
            foreach (var entry in diagnostics.Snapshot().TakeLast(40))
            {
                ImGui.TextDisabled($"{entry.Timestamp.LocalDateTime:HH:mm:ss} [{entry.Level}]");
                ImGui.SameLine();
                ImGui.TextWrapped(entry.Message);
            }
        }
        ImGui.EndChild();
    }

    private void DrawModuleVisibility(HudModuleKind kind, HudModuleConfiguration module)
    {
        if (!OpenSection("Visibility"))
            return;
        DrawToggle("Enable module", module.Enabled, value => Update(c => GetModule(c, kind).Enabled = value));
        var visibility = (int)module.Visibility;
        if (DrawComboSetting(
                $"{kind}.Visibility",
                "Show when",
                "Controls when the enabled module occupies HUD space.",
                ref visibility,
                ModuleVisibilityModes))
            Update(c => GetModule(c, kind).Visibility = (ModuleVisibilityCondition)visibility);
        DrawToggle("Click to target", module.ClickToTarget,
            value => Update(c => GetModule(c, kind).ClickToTarget = value));
        DrawToggle("Right-click native context menu", module.RightClickContextMenu,
            value => Update(c => GetModule(c, kind).RightClickContextMenu = value));
        if (module.ClickToTarget || module.RightClickContextMenu)
        {
            var clickableArea = (int)module.ClickableArea;
            if (DrawComboSetting(
                    $"{kind}.ClickableArea",
                    "Clickable area",
                    "Choose how much of the locked actor panel accepts targeting input.",
                    ref clickableArea,
                    ClickableAreas))
                Update(c => GetModule(c, kind).ClickableArea = (ModuleClickableArea)clickableArea);
        }
        ImGui.TextDisabled("Unlocking the HUD temporarily shows enabled modules for editing.");
        ImGui.TextDisabled("Edit mode overrides actor actions. If both actor interactions are disabled, the locked region is fully mouse-pass-through.");
    }

    private void DrawModuleSizeLayout(HudModuleKind kind, HudModuleConfiguration module)
    {
        if (!OpenSection("Size / Layout"))
            return;
        var scale = module.Scale;
        if (DrawSliderSetting(
                $"{kind}.Scale",
                "Module scale",
                "Scales type and controls without changing the saved panel width.",
                ref scale,
                0.6f,
                1.8f,
                "%.2fx"))
        {
            Update(c => GetModule(c, kind).Scale = scale);
            renderer.RequestReposition(kind);
        }
        var width = module.Width;
        if (DrawSliderSetting(
                $"{kind}.Width",
                "Module width",
                "Extends bars and text layout without stretching fonts or icons.",
                ref width,
                HudSizingPolicy.MinimumWidth,
                HudSizingPolicy.MaximumWidth,
                "%.0f px"))
        {
            Update(c => GetModule(c, kind).Width = width);
            renderer.RequestReposition(kind);
        }
        var barHeight = module.BarHeight;
        if (DrawSliderSetting(
                $"{kind}.BarHeight",
                "Bar height",
                "Controls HP, MP and cast-bar thickness independently of font scale.",
                ref barHeight,
                HudSizingPolicy.MinimumBarHeight,
                HudSizingPolicy.MaximumBarHeight,
                "%.0f px"))
            Update(c => GetModule(c, kind).BarHeight = barHeight);
        if (ImGui.Button("Reset this module's position"))
            renderer.ResetModuleLayout(kind);
    }

    private void DrawModuleAppearance(HudModuleKind kind, HudModuleConfiguration module)
    {
        if (!OpenSection("Appearance"))
            return;
        var opacity = module.Opacity;
        if (DrawSliderSetting(
                $"{kind}.BackgroundOpacity",
                "Background opacity",
                null,
                ref opacity,
                0.15f,
                1f,
                "%.0f%%"))
            Update(c => GetModule(c, kind).Opacity = opacity);
        DrawToggle("Window border", module.BorderEnabled,
            value => Update(c => GetModule(c, kind).BorderEnabled = value));
        if (module.BorderEnabled)
        {
            var borderOpacity = module.BorderOpacity;
            if (DrawSliderSetting(
                    $"{kind}.BorderOpacity",
                    "Border opacity",
                    null,
                    ref borderOpacity,
                    0f,
                    1f,
                    "%.0f%%"))
                Update(c => GetModule(c, kind).BorderOpacity = borderOpacity);
        }
        var alignment = (int)module.HpTextAlignment;
        if (DrawComboSetting(
                $"{kind}.TextAlignment",
                "HP / cast text alignment",
                null,
                ref alignment,
                TextAlignments))
            Update(c => GetModule(c, kind).HpTextAlignment = (HudTextAlignment)alignment);
        var numberFormat = (int)module.NumberFormat;
        if (DrawComboSetting(
                $"{kind}.NumberFormat",
                "HP number format",
                null,
                ref numberFormat,
                NumberFormats))
            Update(c => GetModule(c, kind).NumberFormat = (HudNumberFormat)numberFormat);
    }

    private void DrawNativeTargetOverlay(NativeTargetOverlayConfiguration overlay)
    {
        if (!OpenSection("Native Target HP Overlay"))
            return;
        ImGui.TextWrapped("Draws exact Sentinel HP beneath the visible stock target HP gauge. It detects combined _TargetInfo and split _TargetInfoMainTarget layouts, follows HUD Layout movement, and never modifies native nodes.");
        var mode = (int)overlay.Mode;
        if (DrawComboSetting(
                "Target.NativeOverlay.Mode",
                "Overlay mode",
                null,
                ref mode,
                NativeOverlayModes))
            Update(c => c.Target.NativeHpOverlay.Mode = (NativeTargetOverlayMode)mode);
        var format = (int)overlay.HpFormat;
        if (DrawComboSetting(
                "Target.NativeOverlay.HpFormat",
                "Overlay HP format",
                null,
                ref format,
                NativeOverlayFormats))
            Update(c => c.Target.NativeHpOverlay.HpFormat = (NativeTargetHpFormat)format);
        var numberFormat = (int)overlay.NumberFormat;
        if (DrawComboSetting(
                "Target.NativeOverlay.NumberFormat",
                "Overlay number format",
                null,
                ref numberFormat,
                NumberFormats))
            Update(c => c.Target.NativeHpOverlay.NumberFormat = (HudNumberFormat)numberFormat);
        var offsetX = overlay.OffsetX;
        if (DrawSliderSetting(
                "Target.NativeOverlay.OffsetX",
                "Horizontal offset",
                null,
                ref offsetX,
                -250f,
                250f,
                "%.0f px"))
            Update(c => c.Target.NativeHpOverlay.OffsetX = offsetX);
        var offsetY = overlay.OffsetY;
        if (DrawSliderSetting(
                "Target.NativeOverlay.OffsetY",
                "Vertical offset",
                null,
                ref offsetY,
                -120f,
                120f,
                "%.0f px"))
            Update(c => c.Target.NativeHpOverlay.OffsetY = offsetY);
        ImGui.TextDisabled($"Runtime state: {renderer.NativeTargetOverlayState}");
    }

    private void DrawShieldMode(ShieldDisplayMode current, Action<ShieldDisplayMode> setter)
    {
        var mode = (int)current;
        if (DrawComboSetting(
                "Module.ShieldDisplay",
                "Shield display",
                "Choose text, integrated bar overlay, both, or neither.",
                ref mode,
                ShieldModes))
            setter((ShieldDisplayMode)mode);
    }

    private void DrawMpMode(MpDisplayMode current, Action<MpDisplayMode> setter)
    {
        var mode = (int)current;
        if (DrawComboSetting(
                "Module.MpDisplay",
                "MP display",
                "Choose MP text, bar, both, or neither.",
                ref mode,
                MpModes))
            setter((MpDisplayMode)mode);
    }

    private void DrawHighlightColour(HighlightColourPreset preset, SerializableColour custom,
        string id, Action<HighlightColourPreset> setPreset, Action<Vector4> setCustom)
    {
        var selected = (int)preset;
        if (DrawComboSetting(
                $"{id}.ColourPreset",
                "Colour",
                null,
                ref selected,
                HighlightColours))
            setPreset((HighlightColourPreset)selected);
        if (preset == HighlightColourPreset.Custom)
        {
            var value = new Vector3(custom.Red, custom.Green, custom.Blue);
            if (DrawColourSetting(
                    $"{id}.CustomColour",
                    "Custom colour",
                    null,
                    ref value))
                setCustom(new Vector4(value, 1f));
        }
    }

    private void DrawColour(string label, SerializableColour colour, Action<Vector4> setter)
    {
        var value = new Vector3(colour.Red, colour.Green, colour.Blue);
        if (DrawColourSetting($"Appearance.{label}", label, null, ref value))
            setter(new Vector4(value, 1f));
    }

    private bool DrawComboSetting(
        string id,
        string label,
        string? description,
        ref int value,
        string[] choices)
    {
        if (!modernThemeActive)
            return ImGui.Combo($"{label}##{id}", ref value, choices, choices.Length);

        var next = value;
        var changed = false;
        SentinelModernSettingsRow.Draw(
            $"SentinelHUD.Setting.{id}",
            label,
            description,
            () => changed = ImGui.Combo("##Value", ref next, choices, choices.Length),
            controlWidth: 230f,
            scale: ImGuiHelpers.GlobalScale);
        value = next;
        return changed;
    }

    private bool DrawSliderSetting(
        string id,
        string label,
        string? description,
        ref float value,
        float minimum,
        float maximum,
        string format)
    {
        if (!modernThemeActive)
            return ImGui.SliderFloat($"{label}##{id}", ref value, minimum, maximum, format);

        var next = value;
        var changed = false;
        SentinelModernSettingsRow.Draw(
            $"SentinelHUD.Setting.{id}",
            label,
            description,
            () => changed = ImGui.SliderFloat("##Value", ref next, minimum, maximum, format),
            controlWidth: 230f,
            scale: ImGuiHelpers.GlobalScale);
        value = next;
        return changed;
    }

    private bool DrawColourSetting(
        string id,
        string label,
        string? description,
        ref Vector3 value)
    {
        if (!modernThemeActive)
            return ImGui.ColorEdit3($"{label}##{id}", ref value);

        var next = value;
        var changed = false;
        SentinelModernSettingsRow.Draw(
            $"SentinelHUD.Setting.{id}",
            label,
            description,
            () => changed = ImGui.ColorEdit3("##Value", ref next),
            controlWidth: 230f,
            scale: ImGuiHelpers.GlobalScale);
        value = next;
        return changed;
    }

    private void DrawHpToggles(bool current, bool maximum, bool percentage,
        Action<bool> setCurrent, Action<bool> setMaximum, Action<bool> setPercentage)
    {
        DrawToggle("Show current HP", current, setCurrent);
        DrawToggle("Show maximum HP", maximum, setMaximum);
        DrawToggle("Show HP percentage", percentage, setPercentage);
    }

    private bool OpenSection(string title)
    {
        if (!modernThemeActive)
            return ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen);
        return SentinelModernControls.CollapsingSection(title);
    }

    private static void DrawTab(string title, Action draw)
    {
        if (!ImGui.BeginTabItem(title))
            return;
        try
        {
            ImGui.Spacing();
            draw();
        }
        finally
        {
            ImGui.EndTabItem();
        }
    }

    private bool DrawToggle(string label, bool current, Action<bool> setter)
    {
        if (modernThemeActive)
        {
            var value = current;
            var separator = label.IndexOf("##", StringComparison.Ordinal);
            var visibleLabel = separator >= 0 ? label[..separator] : label;
            if (!SentinelModernSwitch.Draw(
                    $"SentinelHUD.Toggle.{label}",
                    visibleLabel,
                    ref value,
                    modernShellState.Motion,
                    ImGuiHelpers.GlobalScale))
                return false;
            setter(value);
            return true;
        }

        var classicValue = current;
        if (!ImGui.Checkbox(label, ref classicValue))
            return false;
        setter(classicValue);
        return true;
    }

    private void DrawSectionHeader(string title)
    {
        if (!modernThemeActive)
        {
            SentinelUi.SectionHeader(title);
            return;
        }

        SentinelModernUi.SectionHeader(title);
    }

    private void Update(Action<Configuration> mutation) => configuration.Update(mutation);

    private static HudModuleConfiguration GetModule(Configuration config, HudModuleKind kind)
        => kind switch
        {
            HudModuleKind.Player => config.Player,
            HudModuleKind.Target => config.Target,
            HudModuleKind.FocusTarget => config.FocusTarget,
            HudModuleKind.TargetOfTarget => config.TargetOfTarget,
            _ => config.TargetingMeCounter,
        };
}
