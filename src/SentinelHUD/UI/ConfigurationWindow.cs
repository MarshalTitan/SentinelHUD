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

    private readonly ConfigurationCoordinator<Configuration> configuration;
    private readonly HudRenderer renderer;
    private readonly DiagnosticBuffer diagnostics;
    private int selectedLayoutModule;

    public ConfigurationWindow(
        ConfigurationCoordinator<Configuration> configuration,
        HudRenderer renderer,
        DiagnosticBuffer diagnostics)
        : base("Sentinel HUD Configuration##SentinelHUD-Configuration", ImGuiWindowFlags.NoCollapse)
    {
        this.configuration = configuration;
        this.renderer = renderer;
        this.diagnostics = diagnostics;
        Size = new Vector2(720f, 670f);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(620f, 520f),
        };
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
        ImGui.TextWrapped(
            config.Locked
                ? "The HUD is locked and click-through. Use Unlock HUD here or /shud unlock before dragging modules."
                : "HUD editing is active. Target-dependent modules stay visible with placeholders so they can be positioned.");

        if (ImGui.Button(config.Locked ? "Unlock HUD" : "Lock HUD"))
        {
            Update(c => c.Locked = !c.Locked);
            renderer.RequestRepositionAll();
        }
    }

    private void DrawPlayer()
    {
        var config = configuration.Current.Player;
        DrawModuleCommon(HudModuleKind.Player, config);
        DrawToggle("Show character name", config.ShowName, value => Update(c => c.Player.ShowName = value));
        DrawToggle("Show job", config.ShowJob, value => Update(c => c.Player.ShowJob = value));
        DrawToggle("Show role", config.ShowRole, value => Update(c => c.Player.ShowRole = value));
        DrawToggle("Show level", config.ShowLevel, value => Update(c => c.Player.ShowLevel = value));
        DrawHpToggles(
            config.ShowCurrentHp,
            config.ShowMaximumHp,
            config.ShowHpPercentage,
            value => Update(c => c.Player.ShowCurrentHp = value),
            value => Update(c => c.Player.ShowMaximumHp = value),
            value => Update(c => c.Player.ShowHpPercentage = value));
        DrawToggle("Show MP", config.ShowMp, value => Update(c => c.Player.ShowMp = value));
        DrawToggle("Show shield", config.ShowShield, value => Update(c => c.Player.ShowShield = value));
        DrawToggle("Show statuses (first five)", config.ShowStatuses, value => Update(c => c.Player.ShowStatuses = value));
    }

    private void DrawTarget()
    {
        var config = configuration.Current.Target;
        DrawModuleCommon(HudModuleKind.Target, config);
        DrawToggle("Show target name", config.ShowName, value => Update(c => c.Target.ShowName = value));
        DrawHpToggles(
            config.ShowCurrentHp,
            config.ShowMaximumHp,
            config.ShowHpPercentage,
            value => Update(c => c.Target.ShowCurrentHp = value),
            value => Update(c => c.Target.ShowMaximumHp = value),
            value => Update(c => c.Target.ShowHpPercentage = value));
        DrawToggle("Show distance", config.ShowDistance, value => Update(c => c.Target.ShowDistance = value));
        DrawToggle("Show job", config.ShowJob, value => Update(c => c.Target.ShowJob = value));
        DrawToggle("Show role", config.ShowRole, value => Update(c => c.Target.ShowRole = value));
        DrawToggle("Show shield", config.ShowShield, value => Update(c => c.Target.ShowShield = value));
        DrawToggle("Show cast name", config.ShowCastName, value => Update(c => c.Target.ShowCastName = value));
        DrawToggle("Show cast bar", config.ShowCastBar, value => Update(c => c.Target.ShowCastBar = value));
        DrawToggle("Show cast percentage", config.ShowCastPercentage, value => Update(c => c.Target.ShowCastPercentage = value));
        DrawToggle("Show statuses (first five)", config.ShowStatuses, value => Update(c => c.Target.ShowStatuses = value));
    }

    private void DrawFocusTarget()
    {
        var config = configuration.Current.FocusTarget;
        DrawModuleCommon(HudModuleKind.FocusTarget, config);
        DrawToggle("Show focus target name", config.ShowName, value => Update(c => c.FocusTarget.ShowName = value));
        DrawHpToggles(
            config.ShowCurrentHp,
            config.ShowMaximumHp,
            config.ShowHpPercentage,
            value => Update(c => c.FocusTarget.ShowCurrentHp = value),
            value => Update(c => c.FocusTarget.ShowMaximumHp = value),
            value => Update(c => c.FocusTarget.ShowHpPercentage = value));
        DrawToggle("Show distance", config.ShowDistance, value => Update(c => c.FocusTarget.ShowDistance = value));
        DrawToggle("Show shield", config.ShowShield, value => Update(c => c.FocusTarget.ShowShield = value));
        DrawToggle("Show cast name", config.ShowCastName, value => Update(c => c.FocusTarget.ShowCastName = value));
        DrawToggle("Show cast bar", config.ShowCastBar, value => Update(c => c.FocusTarget.ShowCastBar = value));
        DrawToggle("Show cast percentage", config.ShowCastPercentage, value => Update(c => c.FocusTarget.ShowCastPercentage = value));
    }

    private void DrawTargetOfTarget()
    {
        var config = configuration.Current.TargetOfTarget;
        DrawModuleCommon(HudModuleKind.TargetOfTarget, config);
        DrawToggle("Show name", config.ShowName, value => Update(c => c.TargetOfTarget.ShowName = value));
        DrawHpToggles(
            config.ShowCurrentHp,
            config.ShowMaximumHp,
            config.ShowHpPercentage,
            value => Update(c => c.TargetOfTarget.ShowCurrentHp = value),
            value => Update(c => c.TargetOfTarget.ShowMaximumHp = value),
            value => Update(c => c.TargetOfTarget.ShowHpPercentage = value));
        DrawToggle("Show distance", config.ShowDistance, value => Update(c => c.TargetOfTarget.ShowDistance = value));
    }

    private void DrawAwareness()
    {
        SentinelUi.SectionHeader("Self Highlight");
        var config = configuration.Current.SelfHighlight;
        ImGui.TextWrapped(
            "Uses FFXIV's native model-conforming silhouette renderer. Sentinel never changes hard, soft, mouseover, controller, interaction, or action targeting.");
        ImGui.Spacing();

        var mode = (int)config.Mode;
        if (ImGui.Combo("Mode", ref mode, HighlightModes, HighlightModes.Length))
            Update(c => c.SelfHighlight.Mode = (SelfHighlightMode)mode);

        var preset = (int)config.ColourPreset;
        if (ImGui.Combo("Colour", ref preset, HighlightColours, HighlightColours.Length))
            Update(c => c.SelfHighlight.ColourPreset = (HighlightColourPreset)preset);

        if (config.ColourPreset == HighlightColourPreset.Custom)
        {
            var custom = new Vector3(config.CustomColour.Red, config.CustomColour.Green, config.CustomColour.Blue);
            if (ImGui.ColorEdit3("Custom colour", ref custom))
                Update(c => c.SelfHighlight.CustomColour.Set(new Vector4(custom, 1f)));
        }

        var preview = SelfHighlightPolicy.ResolveColour(config);
        preview.W = 1f;
        ImGui.TextUnformatted("Requested colour:");
        ImGui.SameLine();
        ImGui.ColorButton("Self highlight preview##SentinelHUD", preview);
        var nativeSelection = NativeHighlightPolicy.Resolve(config);
        ImGui.TextWrapped(
            nativeSelection.IsExact
                ? $"Applied native colour: {nativeSelection.DisplayName}."
                : $"Applied native colour: {nativeSelection.DisplayName} (nearest safe native palette colour). Exact White and arbitrary Custom colours are not exposed by FFXIV's native silhouette API.");
        ImGui.TextDisabled("The native silhouette API does not expose opacity/intensity control.");
        ImGui.Spacing();
        ImGui.TextDisabled($"Runtime state: {renderer.SelfHighlightState}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        SentinelUi.SectionHeader("Player Position Marker");
        var marker = configuration.Current.PlayerPositionMarker;
        DrawToggle("Enable position marker", marker.Enabled, value => Update(c => c.PlayerPositionMarker.Enabled = value));

        var markerPreset = (int)marker.ColourPreset;
        if (ImGui.Combo("Marker colour", ref markerPreset, HighlightColours, HighlightColours.Length))
            Update(c => c.PlayerPositionMarker.ColourPreset = (HighlightColourPreset)markerPreset);
        if (marker.ColourPreset == HighlightColourPreset.Custom)
        {
            var custom = new Vector3(marker.CustomColour.Red, marker.CustomColour.Green, marker.CustomColour.Blue);
            if (ImGui.ColorEdit3("Custom marker colour", ref custom))
                Update(c => c.PlayerPositionMarker.CustomColour.Set(new Vector4(custom, 1f)));
        }

        var radius = marker.Radius;
        if (ImGui.SliderFloat(
                "Marker radius",
                ref radius,
                PlayerPositionMarkerPolicy.MinimumRadius,
                PlayerPositionMarkerPolicy.MaximumRadius,
                "%.2f yalms"))
            Update(c => c.PlayerPositionMarker.Radius = radius);

        var opacity = marker.Opacity;
        if (ImGui.SliderFloat("Marker opacity", ref opacity, 0.1f, 1f, "%.2f"))
            Update(c => c.PlayerPositionMarker.Opacity = opacity);
        DrawToggle("Thin contrasting border", marker.ShowBorder, value => Update(c => c.PlayerPositionMarker.ShowBorder = value));

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
        if (ImGui.SliderFloat(
                "Maximum zoom distance",
                ref maximum,
                CameraZoomPolicy.StockMaximum,
                CameraZoomPolicy.MaximumSupported,
                "%.1f yalms"))
            Update(c => c.Camera.MaximumZoomDistance = maximum);

        ImGui.TextWrapped(
            "Sentinel changes only the normal third-person maximum zoom limit. It pauses for first person, GPose, cutscenes, territory transitions, and known camera-control plugins.");
        ImGui.Spacing();
        if (ImGui.Button("Disable and restore normal camera limits"))
        {
            Update(c => c.Camera.Enabled = false);
            renderer.RestoreCameraDefaults();
        }
        ImGui.SameLine();
        if (ImGui.Button("Retry after conflict"))
            renderer.RetryCameraZoom();

        ImGui.Spacing();
        ImGui.TextUnformatted($"Runtime state: {renderer.CameraZoomState}");
        ImGui.TextUnformatted($"Current maximum: {renderer.CameraCurrentMaximum:0.0} yalms");
        if (renderer.CameraConflict is not null)
            ImGui.TextWrapped($"Camera controller detected: {renderer.CameraConflict}. Sentinel HUD will not compete with it.");
    }

    private void DrawLayout()
    {
        var config = configuration.Current;
        ImGui.TextWrapped("Unlock the HUD, drag module windows, then lock it again. Positions use resolution-safe normalized anchors and are saved after movement stops.");
        ImGui.Spacing();

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

        ImGui.Spacing();
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
        ImGui.TextUnformatted($"Target-of-target visible / resolved: {renderer.TargetOfTargetVisible} / {renderer.TargetOfTargetResolved}");
        ImGui.TextUnformatted($"Self highlight mode: {config.SelfHighlight.Mode}");
        ImGui.TextUnformatted($"Self highlight active: {renderer.SelfHighlightActive}");
        ImGui.TextUnformatted($"Self highlight applied colour: {renderer.SelfHighlightAppliedColour}");
        ImGui.TextWrapped($"Self highlight state: {renderer.SelfHighlightState}");
        ImGui.TextUnformatted($"Position marker enabled / active: {config.PlayerPositionMarker.Enabled} / {renderer.PositionMarkerActive}");
        ImGui.TextUnformatted($"Position marker terrain projection: {renderer.PositionMarkerUsedTerrainProjection}");
        ImGui.TextWrapped($"Position marker state: {renderer.PositionMarkerState}");
        ImGui.TextUnformatted($"Extended zoom enabled / active: {config.Camera.Enabled} / {renderer.CameraZoomActive}");
        ImGui.TextWrapped($"Extended zoom state: {renderer.CameraZoomState}");

        ImGui.Spacing();
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

    private void DrawModuleCommon(HudModuleKind kind, HudModuleConfiguration module)
    {
        DrawToggle("Enable module", module.Enabled, value => Update(c => GetModule(c, kind).Enabled = value));
        var scale = module.Scale;
        if (ImGui.SliderFloat("Module scale", ref scale, 0.6f, 1.8f, "%.2fx"))
        {
            Update(c => GetModule(c, kind).Scale = scale);
            renderer.RequestReposition(kind);
        }
        var opacity = module.Opacity;
        if (ImGui.SliderFloat("Module opacity", ref opacity, 0.15f, 1f, "%.0f%%"))
            Update(c => GetModule(c, kind).Opacity = opacity);
        if (ImGui.Button("Reset this module's position"))
            renderer.ResetModuleLayout(kind);
        ImGui.Separator();
    }

    private void DrawHpToggles(
        bool current,
        bool maximum,
        bool percentage,
        Action<bool> setCurrent,
        Action<bool> setMaximum,
        Action<bool> setPercentage)
    {
        DrawToggle("Show current HP", current, setCurrent);
        DrawToggle("Show maximum HP", maximum, setMaximum);
        DrawToggle("Show HP percentage", percentage, setPercentage);
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
        var value = current;
        if (!ImGui.Checkbox(label, ref value))
            return false;
        setter(value);
        return true;
    }

    private void Update(Action<Configuration> mutation)
        => configuration.Update(mutation);

    private static HudModuleConfiguration GetModule(Configuration config, HudModuleKind kind)
        => kind switch
        {
            HudModuleKind.Player => config.Player,
            HudModuleKind.Target => config.Target,
            HudModuleKind.FocusTarget => config.FocusTarget,
            _ => config.TargetOfTarget,
        };
}
