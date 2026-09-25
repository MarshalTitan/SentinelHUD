using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SentinelCore.Configuration;
using SentinelCore.Diagnostics;
using SentinelCore.UI;
using SentinelHUD.Core;

namespace SentinelHUD.UI;

public sealed class ConfigurationWindow : Window
{
    private static readonly string[] HighlightModes = ["Off", "Always", "Combat Only", "Duty Only"];
    private static readonly string[] HighlightColours = ["Yellow", "Green", "Blue", "White", "Custom"];
    private static readonly string[] ModuleNames = ["Player", "Target", "Focus Target", "Target of Target"];
    private static readonly string[] ModuleVisibilityModes = ["Always", "Combat Only", "Duty Only", "Combat or Duty"];
    private static readonly string[] ShieldModes = ["Off", "Text Only", "Bar Only", "Bar + Text"];
    private static readonly string[] MpModes = ["Off", "Text Only", "Bar Only", "Bar + Text"];
    private static readonly string[] HpColourModes = ["Static / Role-Based", "Health-State Gradient"];
    private static readonly string[] TextAlignments = ["Left", "Center", "Right"];
    private static readonly string[] NumberFormats = ["Full", "Compact"];
    private static readonly string[] NativeOverlayModes = ["Off", "Always", "Combat Only"];
    private static readonly string[] NativeOverlayFormats = ["Current / Maximum", "Percentage", "Current / Maximum + Percentage"];

    private readonly ConfigurationCoordinator<Configuration> configuration;
    private readonly HudRenderer renderer;
    private readonly DiagnosticBuffer diagnostics;
    private int selectedLayoutModule;

    public ConfigurationWindow(
        ConfigurationCoordinator<Configuration> configuration,
        HudRenderer renderer,
        DiagnosticBuffer diagnostics)
        : base("Sentinel HUD Configuration##SentinelHUD-Configuration")
    {
        this.configuration = configuration;
        this.renderer = renderer;
        this.diagnostics = diagnostics;
        Size = new Vector2(760f, 720f);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(640f, 540f) };
    }

    public override void Draw()
    {
        using var style = SentinelStyleScope.PushWindow();
        SentinelUi.SectionHeader("Sentinel HUD");
        ImGui.TextDisabled("Modular enhancements for the native FFXIV HUD");
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
            DrawTab("Camera", DrawCamera);
            DrawTab("Appearance", DrawAppearance);
            DrawTab("Layout", DrawLayout);
            DrawTab("Diagnostics", DrawDiagnostics);
        }
        finally
        {
            ImGui.EndTabBar();
        }
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
            ? "The HUD is locked and click-through. Unlock it before dragging modules."
            : "HUD editing is active. Conditional and target-dependent modules remain visible with placeholders for positioning.");
        if (ImGui.Button(config.Locked ? "Unlock HUD" : "Lock HUD"))
        {
            Update(c => c.Locked = !c.Locked);
            renderer.RequestRepositionAll();
        }
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
        SentinelUi.SectionHeader("Self Highlight");
        var highlight = configuration.Current.SelfHighlight;
        ImGui.TextWrapped("Uses FFXIV's native model-conforming silhouette renderer without changing any targeting state.");
        var mode = (int)highlight.Mode;
        if (ImGui.Combo("Mode##SelfHighlight", ref mode, HighlightModes, HighlightModes.Length))
            Update(c => c.SelfHighlight.Mode = (SelfHighlightMode)mode);
        DrawHighlightColour(highlight.ColourPreset, highlight.CustomColour, "SelfHighlight",
            value => Update(c => c.SelfHighlight.ColourPreset = value),
            value => Update(c => c.SelfHighlight.CustomColour.Set(value)));
        var preview = SelfHighlightPolicy.ResolveColour(highlight);
        preview.W = 1f;
        ImGui.TextUnformatted("Requested colour:");
        ImGui.SameLine();
        ImGui.ColorButton("Self highlight preview##SentinelHUD", preview);
        var nativeSelection = NativeHighlightPolicy.Resolve(highlight);
        ImGui.TextWrapped(nativeSelection.IsExact
            ? $"Applied native colour: {nativeSelection.DisplayName}."
            : $"Applied native colour: {nativeSelection.DisplayName} (nearest safe native palette colour). Exact White and arbitrary Custom colours are not exposed by the native silhouette API.");
        ImGui.TextDisabled($"Runtime state: {renderer.SelfHighlightState}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        SentinelUi.SectionHeader("Player Position Marker");
        var marker = configuration.Current.PlayerPositionMarker;
        var markerMode = (int)marker.Mode;
        if (ImGui.Combo("Mode##PositionMarker", ref markerMode, HighlightModes, HighlightModes.Length))
            Update(c => c.PlayerPositionMarker.Mode = (SelfHighlightMode)markerMode);
        DrawHighlightColour(marker.ColourPreset, marker.CustomColour, "PositionMarker",
            value => Update(c => c.PlayerPositionMarker.ColourPreset = value),
            value => Update(c => c.PlayerPositionMarker.CustomColour.Set(value)));

        var radius = marker.Radius;
        ImGui.SetNextItemWidth(320f);
        if (ImGui.SliderFloat("Marker radius", ref radius, PlayerPositionMarkerPolicy.MinimumRadius,
                PlayerPositionMarkerPolicy.MaximumRadius, "%.2f yalms"))
            Update(c => c.PlayerPositionMarker.Radius = radius);
        ImGui.TextDisabled("Radius is the total outside radius, including any border.");
        var opacity = marker.Opacity;
        if (ImGui.SliderFloat("Marker opacity", ref opacity, 0.1f, 1f, "%.2f"))
            Update(c => c.PlayerPositionMarker.Opacity = opacity);
        DrawToggle("Thin contrasting border", marker.ShowBorder,
            value => Update(c => c.PlayerPositionMarker.ShowBorder = value));
        if (marker.ShowBorder)
        {
            var thickness = marker.BorderThickness;
            if (ImGui.SliderFloat("Border thickness", ref thickness,
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
    }

    private void DrawCamera()
    {
        SentinelUi.SectionHeader("Extended Zoom");
        var camera = configuration.Current.Camera;
        DrawToggle("Extended zoom enabled", camera.Enabled, value => Update(c => c.Camera.Enabled = value));
        var maximum = camera.MaximumZoomDistance;
        if (ImGui.SliderFloat("Maximum zoom distance", ref maximum, CameraZoomPolicy.StockMaximum,
                CameraZoomPolicy.MaximumSupported, "%.1f yalms"))
            Update(c => c.Camera.MaximumZoomDistance = maximum);
        ImGui.TextWrapped("Changes only the normal third-person maximum. It pauses for first person, GPose, cutscenes, transitions, and known camera-control plugins.");
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
        SentinelUi.SectionHeader("Bar Colours");
        var appearance = configuration.Current.Appearance;
        var playerMode = (int)appearance.PlayerHpColourMode;
        if (ImGui.Combo("Player HP colour mode", ref playerMode, HpColourModes, HpColourModes.Length))
            Update(c => c.Appearance.PlayerHpColourMode = (HpColourMode)playerMode);
        var targetMode = (int)appearance.TargetHpColourMode;
        if (ImGui.Combo("Target / Focus / ToT HP colour mode", ref targetMode,
                HpColourModes, HpColourModes.Length))
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
        ImGui.TextWrapped("Unlock the HUD, drag module windows, then lock it again. Positions use resolution-safe normalized anchors and save after movement stops.");
        if (ImGui.Button(config.Locked ? "Unlock HUD" : "Lock HUD"))
        {
            Update(c => c.Locked = !c.Locked);
            renderer.RequestRepositionAll();
        }
        ImGui.SetNextItemWidth(220f);
        ImGui.Combo("Selected module", ref selectedLayoutModule, ModuleNames, ModuleNames.Length);
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
        ImGui.TextUnformatted($"HUD enabled / locked: {config.Enabled} / {config.Locked}");
        ImGui.TextUnformatted($"Player module visible: {renderer.PlayerVisible}");
        ImGui.TextUnformatted($"Target visible / resolved: {renderer.TargetVisible} / {renderer.TargetResolved}");
        ImGui.TextUnformatted($"Focus visible / resolved: {renderer.FocusTargetVisible} / {renderer.FocusTargetResolved}");
        ImGui.TextUnformatted($"Focus Target's Target resolved: {renderer.FocusTargetTargetResolved}");
        ImGui.TextWrapped($"Focus Target click state: {renderer.FocusTargetClickState}");
        ImGui.TextUnformatted($"Target-of-target visible / resolved: {renderer.TargetOfTargetVisible} / {renderer.TargetOfTargetResolved}");
        ImGui.TextUnformatted($"Self highlight mode / active: {config.SelfHighlight.Mode} / {renderer.SelfHighlightActive}");
        ImGui.TextUnformatted($"Self highlight applied colour: {renderer.SelfHighlightAppliedColour}");
        ImGui.TextWrapped($"Self highlight state: {renderer.SelfHighlightState}");
        ImGui.TextUnformatted($"Position marker mode / active: {config.PlayerPositionMarker.Mode} / {renderer.PositionMarkerActive}");
        ImGui.TextUnformatted($"Position marker terrain projection: {renderer.PositionMarkerUsedTerrainProjection}");
        ImGui.TextWrapped($"Position marker state: {renderer.PositionMarkerState}");
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
        if (ImGui.Combo("Show when", ref visibility, ModuleVisibilityModes, ModuleVisibilityModes.Length))
            Update(c => GetModule(c, kind).Visibility = (ModuleVisibilityCondition)visibility);
        ImGui.TextDisabled("Unlocking the HUD temporarily shows enabled modules for editing.");
    }

    private void DrawModuleSizeLayout(HudModuleKind kind, HudModuleConfiguration module)
    {
        if (!OpenSection("Size / Layout"))
            return;
        var scale = module.Scale;
        if (ImGui.SliderFloat("Module scale", ref scale, 0.6f, 1.8f, "%.2fx"))
        {
            Update(c => GetModule(c, kind).Scale = scale);
            renderer.RequestReposition(kind);
        }
        var width = module.Width;
        if (ImGui.SliderFloat("Module width", ref width, HudSizingPolicy.MinimumWidth,
                HudSizingPolicy.MaximumWidth, "%.0f px"))
        {
            Update(c => GetModule(c, kind).Width = width);
            renderer.RequestReposition(kind);
        }
        var barHeight = module.BarHeight;
        if (ImGui.SliderFloat("Bar height", ref barHeight, HudSizingPolicy.MinimumBarHeight,
                HudSizingPolicy.MaximumBarHeight, "%.0f px"))
            Update(c => GetModule(c, kind).BarHeight = barHeight);
        if (ImGui.Button("Reset this module's position"))
            renderer.ResetModuleLayout(kind);
    }

    private void DrawModuleAppearance(HudModuleKind kind, HudModuleConfiguration module)
    {
        if (!OpenSection("Appearance"))
            return;
        var opacity = module.Opacity;
        if (ImGui.SliderFloat("Background opacity", ref opacity, 0.15f, 1f, "%.0f%%"))
            Update(c => GetModule(c, kind).Opacity = opacity);
        DrawToggle("Window border", module.BorderEnabled,
            value => Update(c => GetModule(c, kind).BorderEnabled = value));
        if (module.BorderEnabled)
        {
            var borderOpacity = module.BorderOpacity;
            if (ImGui.SliderFloat("Border opacity", ref borderOpacity, 0f, 1f, "%.0f%%"))
                Update(c => GetModule(c, kind).BorderOpacity = borderOpacity);
        }
        var alignment = (int)module.HpTextAlignment;
        if (ImGui.Combo("HP / cast text alignment", ref alignment, TextAlignments, TextAlignments.Length))
            Update(c => GetModule(c, kind).HpTextAlignment = (HudTextAlignment)alignment);
        var numberFormat = (int)module.NumberFormat;
        if (ImGui.Combo("HP number format", ref numberFormat, NumberFormats, NumberFormats.Length))
            Update(c => GetModule(c, kind).NumberFormat = (HudNumberFormat)numberFormat);
    }

    private void DrawNativeTargetOverlay(NativeTargetOverlayConfiguration overlay)
    {
        if (!OpenSection("Native Target HP Overlay"))
            return;
        ImGui.TextWrapped("Draws exact Sentinel HP beneath the visible stock target HP gauge. It detects combined _TargetInfo and split _TargetInfoMainTarget layouts, follows HUD Layout movement, and never modifies native nodes.");
        var mode = (int)overlay.Mode;
        if (ImGui.Combo("Overlay mode", ref mode, NativeOverlayModes, NativeOverlayModes.Length))
            Update(c => c.Target.NativeHpOverlay.Mode = (NativeTargetOverlayMode)mode);
        var format = (int)overlay.HpFormat;
        if (ImGui.Combo("Overlay HP format", ref format, NativeOverlayFormats, NativeOverlayFormats.Length))
            Update(c => c.Target.NativeHpOverlay.HpFormat = (NativeTargetHpFormat)format);
        var numberFormat = (int)overlay.NumberFormat;
        if (ImGui.Combo("Overlay number format", ref numberFormat, NumberFormats, NumberFormats.Length))
            Update(c => c.Target.NativeHpOverlay.NumberFormat = (HudNumberFormat)numberFormat);
        var offsetX = overlay.OffsetX;
        if (ImGui.SliderFloat("Horizontal offset", ref offsetX, -250f, 250f, "%.0f px"))
            Update(c => c.Target.NativeHpOverlay.OffsetX = offsetX);
        var offsetY = overlay.OffsetY;
        if (ImGui.SliderFloat("Vertical offset", ref offsetY, -120f, 120f, "%.0f px"))
            Update(c => c.Target.NativeHpOverlay.OffsetY = offsetY);
        ImGui.TextDisabled($"Runtime state: {renderer.NativeTargetOverlayState}");
    }

    private void DrawShieldMode(ShieldDisplayMode current, Action<ShieldDisplayMode> setter)
    {
        var mode = (int)current;
        if (ImGui.Combo("Shield display", ref mode, ShieldModes, ShieldModes.Length))
            setter((ShieldDisplayMode)mode);
    }

    private void DrawMpMode(MpDisplayMode current, Action<MpDisplayMode> setter)
    {
        var mode = (int)current;
        if (ImGui.Combo("MP display", ref mode, MpModes, MpModes.Length))
            setter((MpDisplayMode)mode);
    }

    private void DrawHighlightColour(HighlightColourPreset preset, SerializableColour custom,
        string id, Action<HighlightColourPreset> setPreset, Action<Vector4> setCustom)
    {
        var selected = (int)preset;
        if (ImGui.Combo($"Colour##{id}", ref selected, HighlightColours, HighlightColours.Length))
            setPreset((HighlightColourPreset)selected);
        if (preset == HighlightColourPreset.Custom)
        {
            var value = new Vector3(custom.Red, custom.Green, custom.Blue);
            if (ImGui.ColorEdit3($"Custom colour##{id}", ref value))
                setCustom(new Vector4(value, 1f));
        }
    }

    private static void DrawColour(string label, SerializableColour colour, Action<Vector4> setter)
    {
        var value = new Vector3(colour.Red, colour.Green, colour.Blue);
        if (ImGui.ColorEdit3(label, ref value))
            setter(new Vector4(value, 1f));
    }

    private void DrawHpToggles(bool current, bool maximum, bool percentage,
        Action<bool> setCurrent, Action<bool> setMaximum, Action<bool> setPercentage)
    {
        DrawToggle("Show current HP", current, setCurrent);
        DrawToggle("Show maximum HP", maximum, setMaximum);
        DrawToggle("Show HP percentage", percentage, setPercentage);
    }

    private static bool OpenSection(string title)
        => ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen);

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
        var value = current;
        if (!ImGui.Checkbox(label, ref value))
            return false;
        setter(value);
        return true;
    }

    private void Update(Action<Configuration> mutation) => configuration.Update(mutation);

    private static HudModuleConfiguration GetModule(Configuration config, HudModuleKind kind)
        => kind switch
        {
            HudModuleKind.Player => config.Player,
            HudModuleKind.Target => config.Target,
            HudModuleKind.FocusTarget => config.FocusTarget,
            _ => config.TargetOfTarget,
        };
}
