using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using SentinelCore.Configuration;
using SentinelCore.Diagnostics;
using SentinelCore.UI;
using SentinelHUD.Core;
using SentinelHUD.Services;

namespace SentinelHUD.UI;

public sealed class HudRenderer
{
    private static readonly Vector2 UnknownWindowSize = new(285f, 120f);
    private readonly ConfigurationCoordinator<Configuration> configuration;
    private readonly HudDataService data;
    private readonly ISelfHighlightService selfHighlight;
    private readonly PlayerPositionMarkerRenderer positionMarker;
    private readonly NativeTargetHpOverlayRenderer nativeTargetOverlay;
    private readonly IExtendedCameraZoomService cameraZoom;
    private readonly IGameGui gameGui;
    private readonly IClientState clientState;
    private readonly ICondition condition;
    private readonly DiagnosticBuffer diagnostics;
    private readonly Dictionary<HudModuleKind, LayoutRuntimeState> layoutStates = new();
    private HudDiagnosticState lastDiagnosticState;
    private bool hasDiagnosticState;

    public HudRenderer(
        ConfigurationCoordinator<Configuration> configuration,
        HudDataService data,
        ISelfHighlightService selfHighlight,
        PlayerPositionMarkerRenderer positionMarker,
        NativeTargetHpOverlayRenderer nativeTargetOverlay,
        IExtendedCameraZoomService cameraZoom,
        IGameGui gameGui,
        IClientState clientState,
        ICondition condition,
        DiagnosticBuffer diagnostics)
    {
        this.configuration = configuration;
        this.data = data;
        this.selfHighlight = selfHighlight;
        this.positionMarker = positionMarker;
        this.nativeTargetOverlay = nativeTargetOverlay;
        this.cameraZoom = cameraZoom;
        this.gameGui = gameGui;
        this.clientState = clientState;
        this.condition = condition;
        this.diagnostics = diagnostics;
        foreach (var kind in Enum.GetValues<HudModuleKind>())
            layoutStates[kind] = new LayoutRuntimeState();
    }

    public bool PlayerVisible { get; private set; }
    public bool TargetVisible { get; private set; }
    public bool FocusTargetVisible { get; private set; }
    public bool TargetOfTargetVisible { get; private set; }
    public bool TargetResolved { get; private set; }
    public bool FocusTargetResolved { get; private set; }
    public bool TargetOfTargetResolved { get; private set; }
    public bool SelfHighlightActive => selfHighlight.IsActive;
    public string SelfHighlightState => selfHighlight.StateReason;
    public string SelfHighlightAppliedColour => selfHighlight.AppliedColourName;
    public bool SelfHighlightSupportsArbitraryColour => selfHighlight.SupportsArbitraryColour;
    public bool PositionMarkerActive => positionMarker.IsActive;
    public bool PositionMarkerUsedTerrainProjection => positionMarker.UsedTerrainProjection;
    public string PositionMarkerState => positionMarker.StateReason;
    public bool NativeTargetOverlayActive => nativeTargetOverlay.IsActive;
    public string NativeTargetOverlayState => nativeTargetOverlay.StateReason;
    public Vector2 NativeTargetOverlayAnchor => nativeTargetOverlay.AnchorPosition;
    public bool CameraZoomActive => cameraZoom.IsActive;
    public string CameraZoomState => cameraZoom.StateReason;
    public string? CameraConflict => cameraZoom.ConflictingPluginName;
    public float CameraCurrentMaximum => cameraZoom.CurrentMaximum;

    public void UpdateGameState()
    {
        var config = configuration.Current;
        cameraZoom.Update(config.Camera, config.Enabled);
        selfHighlight.Update(config.SelfHighlight, config.Enabled);
    }

    public void Draw()
    {
        var config = configuration.Current;
        ResetVisibilityState();
        var runtime = GetRuntimeVisibilityState();
        var player = config.Enabled ? data.LocalPlayer : null;
        var target = config.Enabled ? data.Target : null;

        positionMarker.Draw(config.PlayerPositionMarker, config.Enabled, player,
            runtime.IsLoggedIn, runtime.IsInCombat, runtime.IsInDuty);
        nativeTargetOverlay.Draw(config.Target.NativeHpOverlay, config.Enabled,
            runtime.IsLoggedIn, runtime.IsInCombat, target);

        if (!config.Enabled || gameGui.GameUiHidden)
        {
            RecordState(config);
            return;
        }

        var focus = data.FocusTarget;
        var targetOfTarget = data.ResolveTargetOfTarget(target);
        TargetResolved = target is not null;
        FocusTargetResolved = focus is not null;
        TargetOfTargetResolved = targetOfTarget is not null;

        if (ShouldDrawModule(config.Player, config.Locked, runtime) && (player is not null || !config.Locked))
        {
            DrawModule(HudModuleKind.Player, config.Player, config, player, DrawPlayer);
            PlayerVisible = true;
        }
        if (ShouldDrawModule(config.Target, config.Locked, runtime) && (target is not null || !config.Locked))
        {
            DrawModule(HudModuleKind.Target, config.Target, config, target, DrawTarget);
            TargetVisible = true;
        }
        if (ShouldDrawModule(config.FocusTarget, config.Locked, runtime) && (focus is not null || !config.Locked))
        {
            DrawModule(HudModuleKind.FocusTarget, config.FocusTarget, config, focus, DrawFocusTarget);
            FocusTargetVisible = true;
        }
        if (ShouldDrawModule(config.TargetOfTarget, config.Locked, runtime)
            && (targetOfTarget is not null || !config.Locked))
        {
            DrawModule(HudModuleKind.TargetOfTarget, config.TargetOfTarget, config,
                targetOfTarget, DrawTargetOfTarget);
            TargetOfTargetVisible = true;
        }

        RecordState(config);
    }

    public void RequestRepositionAll()
    {
        foreach (var state in layoutStates.Values)
            state.ApplySavedPosition = true;
    }

    public void RequestReposition(HudModuleKind kind) => layoutStates[kind].ApplySavedPosition = true;

    public void ResetModuleLayout(HudModuleKind kind)
    {
        configuration.Update(config => GetModule(config, kind).Layout = GetDefaultModule(kind).Layout);
        RequestReposition(kind);
    }

    public void ResetAllLayouts()
    {
        configuration.Update(config =>
        {
            foreach (var kind in Enum.GetValues<HudModuleKind>())
                GetModule(config, kind).Layout = GetDefaultModule(kind).Layout;
        });
        RequestRepositionAll();
    }

    public void CopyAppearanceToOtherModules(HudModuleKind sourceKind)
    {
        configuration.Update(config =>
        {
            var source = GetModule(config, sourceKind);
            foreach (var kind in Enum.GetValues<HudModuleKind>())
            {
                if (kind == sourceKind)
                    continue;
                var target = GetModule(config, kind);
                target.Scale = source.Scale;
                target.Width = source.Width;
                target.BarHeight = source.BarHeight;
                target.Opacity = source.Opacity;
                target.BorderEnabled = source.BorderEnabled;
                target.BorderOpacity = source.BorderOpacity;
                target.HpTextAlignment = source.HpTextAlignment;
                target.NumberFormat = source.NumberFormat;
            }
        });
        RequestRepositionAll();
    }

    public void RestoreCameraDefaults() => cameraZoom.Restore();
    public void RetryCameraZoom() => cameraZoom.RetryAfterConflict();

    private void DrawModule(HudModuleKind kind, HudModuleConfiguration module, Configuration root,
        IGameObject? actor, Action<IGameObject?, HudModuleConfiguration> drawContent)
    {
        var viewport = ImGui.GetMainViewport();
        var state = layoutStates[kind];
        var viewportChanged = Vector2.DistanceSquared(viewport.WorkSize, state.LastViewportSize) > 0.25f
                              || Vector2.DistanceSquared(viewport.WorkPos, state.LastViewportPosition) > 0.25f;
        var applySavedPosition = root.Locked || state.ApplySavedPosition || viewportChanged;
        using var style = SentinelStyleScope.PushWindow(module.Scale * root.GlobalScale);
        var expectedEditChromeHeight = root.Locked
            ? 0f
            : state.LastEditChromeHeight > 0f
                ? state.LastEditChromeHeight
                : ImGui.GetFrameHeight();
        var desiredContentPosition = LayoutPolicy.ToPixelPosition(module.Layout, viewport.WorkPos,
            viewport.WorkSize, state.LastContentSize);
        if (applySavedPosition)
        {
            ImGui.SetNextWindowPos(LayoutPolicy.ToOuterWindowPosition(
                desiredContentPosition, expectedEditChromeHeight), ImGuiCond.Always);
        }

        ImGui.SetNextWindowSizeConstraints(new Vector2(module.Width, 1f),
            new Vector2(module.Width, float.MaxValue));
        ImGui.SetNextWindowBgAlpha(module.Opacity * root.GlobalOpacity);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, module.BorderEnabled ? 1f : 0f);
        var borderColour = SentinelPalette.HeaderGold;
        borderColour.W = module.BorderOpacity * root.GlobalOpacity;
        ImGui.PushStyleColor(ImGuiCol.Border, borderColour);
        try
        {
            var flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar
                        | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoSavedSettings
                        | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoNav
                        | ImGuiWindowFlags.NoFocusOnAppearing;
            if (root.Locked)
                flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar;

            var title = kind switch
            {
                HudModuleKind.Player => "Player##SentinelHUD-Player",
                HudModuleKind.Target => "Target##SentinelHUD-Target",
                HudModuleKind.FocusTarget => "Focus Target##SentinelHUD-FocusTarget",
                _ => "Target of Target##SentinelHUD-TargetOfTarget",
            };
            var began = ImGui.Begin(title, flags);
            try
            {
                var actualPosition = ImGui.GetWindowPos();
                var measuredEditChromeHeight = root.Locked
                    ? 0f
                    : Math.Max(0f, ImGui.GetCursorScreenPos().Y - actualPosition.Y
                                   - ImGui.GetStyle().WindowPadding.Y);
                if (applySavedPosition
                    && Math.Abs(measuredEditChromeHeight - expectedEditChromeHeight) > 0.1f)
                {
                    actualPosition = LayoutPolicy.ToOuterWindowPosition(
                        desiredContentPosition, measuredEditChromeHeight);
                    ImGui.SetWindowPos(actualPosition, ImGuiCond.Always);
                }

                ImGui.SetWindowFontScale(module.Scale * root.GlobalScale);
                if (began)
                {
                    if (actor is null)
                        SentinelUi.MutedText("No actor resolved — drag this module into place.");
                    else
                        drawContent(actor, module);
                }

                actualPosition = ImGui.GetWindowPos();
                var actualSize = ImGui.GetWindowSize();
                var actualContentPosition = LayoutPolicy.ToContentPosition(
                    actualPosition, measuredEditChromeHeight);
                var actualContentSize = LayoutPolicy.ToContentSize(actualSize, measuredEditChromeHeight);
                var reachableContentPosition = LayoutPolicy.KeepReachable(actualContentPosition,
                    viewport.WorkPos, viewport.WorkSize, actualContentSize);
                if (Vector2.DistanceSquared(actualContentPosition, reachableContentPosition) > 0.25f)
                {
                    actualContentPosition = reachableContentPosition;
                    actualPosition = LayoutPolicy.ToOuterWindowPosition(
                        actualContentPosition, measuredEditChromeHeight);
                    ImGui.SetWindowPos(actualPosition, ImGuiCond.Always);
                }

                state.LastContentSize = actualContentSize.X > 0f && actualContentSize.Y > 0f
                    ? actualContentSize
                    : UnknownWindowSize;
                if (measuredEditChromeHeight > 0f)
                    state.LastEditChromeHeight = measuredEditChromeHeight;
                state.LastViewportPosition = viewport.WorkPos;
                state.LastViewportSize = viewport.WorkSize;
                state.ApplySavedPosition = false;
                if (!root.Locked && !applySavedPosition)
                {
                    var normalized = LayoutPolicy.ToNormalizedPosition(actualContentPosition,
                        viewport.WorkPos, viewport.WorkSize, state.LastContentSize);
                    if (Math.Abs(normalized.AnchorX - module.Layout.AnchorX) > 0.0001f
                        || Math.Abs(normalized.AnchorY - module.Layout.AnchorY) > 0.0001f)
                    {
                        configuration.Update(config =>
                        {
                            var liveLayout = GetModule(config, kind).Layout;
                            liveLayout.AnchorX = normalized.AnchorX;
                            liveLayout.AnchorY = normalized.AnchorY;
                        }, TimeSpan.FromMilliseconds(650));
                    }
                }
            }
            finally
            {
                ImGui.End();
            }
        }
        finally
        {
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }
    }

    private void DrawPlayer(IGameObject? actor, HudModuleConfiguration baseConfiguration)
    {
        var config = (PlayerModuleConfiguration)baseConfiguration;
        if (actor is not ICharacter character)
            return;
        DrawCompactHeader(actor, character, config.ShowName, config.ShowJob, config.ShowRole,
            config.ShowLevel, true);
        DrawHealth(character, actor, HudModuleKind.Player, config, config.ShieldDisplay);
        DrawShieldText(character, config.ShieldDisplay);
        DrawMp(character, config, config.MpDisplay);
        if (config.ShowStatuses && actor is IBattleChara battleChara)
            DrawStatuses(battleChara);
    }

    private void DrawTarget(IGameObject? actor, HudModuleConfiguration baseConfiguration)
    {
        var config = (TargetModuleConfiguration)baseConfiguration;
        if (actor is null)
            return;
        if (actor is ICharacter character)
        {
            DrawCompactHeader(actor, character, config.ShowName, config.ShowJob, config.ShowRole,
                config.ShowLevel, actor.ObjectKind == ObjectKind.Pc);
            DrawHealth(character, actor, HudModuleKind.Target, config, config.ShieldDisplay);
            DrawShieldText(character, config.ShieldDisplay);
            DrawMp(character, config, config.MpDisplay);
        }
        else if (config.ShowName)
        {
            ImGui.TextColored(SentinelPalette.HeaderGold, actor.Name.TextValue);
        }
        if (config.ShowDistance)
            ImGui.TextUnformatted(HudFormatting.Distance(data.GetDistance(actor)));
        if (actor is IBattleChara battleChara)
        {
            DrawCast(battleChara, config, config.ShowCastName, config.ShowCastBar,
                config.ShowCastPercentage);
            if (config.ShowStatuses)
                DrawStatuses(battleChara);
        }
    }

    private void DrawFocusTarget(IGameObject? actor, HudModuleConfiguration baseConfiguration)
    {
        var config = (FocusTargetModuleConfiguration)baseConfiguration;
        if (actor is null)
            return;
        if (config.ShowName)
            ImGui.TextColored(SentinelPalette.HeaderGold, actor.Name.TextValue);
        if (actor is ICharacter character)
        {
            DrawHealth(character, actor, HudModuleKind.FocusTarget, config, config.ShieldDisplay);
            DrawShieldText(character, config.ShieldDisplay);
            DrawMp(character, config, config.MpDisplay);
        }
        if (config.ShowDistance)
            ImGui.TextUnformatted(HudFormatting.Distance(data.GetDistance(actor)));
        if (actor is IBattleChara battleChara)
            DrawCast(battleChara, config, config.ShowCastName, config.ShowCastBar,
                config.ShowCastPercentage);
    }

    private void DrawTargetOfTarget(IGameObject? actor, HudModuleConfiguration baseConfiguration)
    {
        var config = (TargetOfTargetModuleConfiguration)baseConfiguration;
        if (actor is null)
            return;
        if (config.ShowName)
            ImGui.TextColored(SentinelPalette.HeaderGold, actor.Name.TextValue);
        if (actor is ICharacter character)
            DrawHealth(character, actor, HudModuleKind.TargetOfTarget, config, ShieldDisplayMode.Off);
        if (config.ShowDistance)
            ImGui.TextUnformatted(HudFormatting.Distance(data.GetDistance(actor)));
    }

    private void DrawCompactHeader(IGameObject actor, ICharacter character, bool showName,
        bool showJob, bool showRole, bool showLevel, bool allowPlayerJob)
    {
        var metadata = string.Empty;
        if (allowPlayerJob && showJob && data.GetJob(actor) is { } job)
            metadata = job.Abbreviation;
        if (allowPlayerJob && showRole)
            metadata = AppendHeaderPart(metadata, data.GetRoleName(actor));
        if (showLevel)
            metadata = AppendHeaderPart(metadata, $"Lv.{character.Level}");

        if (!showName)
        {
            if (metadata.Length > 0)
                ImGui.TextDisabled(metadata);
            return;
        }
        var name = actor.Name.TextValue;
        if (metadata.Length == 0)
        {
            ImGui.TextColored(SentinelPalette.HeaderGold, name);
            return;
        }

        var available = ImGui.GetContentRegionAvail().X;
        var nameWidth = ImGui.CalcTextSize(name).X;
        var metadataWidth = ImGui.CalcTextSize(metadata).X;
        var startX = ImGui.GetCursorPosX();
        ImGui.TextColored(SentinelPalette.HeaderGold, name);
        if (nameWidth + metadataWidth + 16f <= available)
        {
            ImGui.SameLine(startX + available - metadataWidth);
            ImGui.TextDisabled(metadata);
        }
        else
        {
            ImGui.TextDisabled(metadata);
        }
    }

    private void DrawHealth(ICharacter character, IGameObject actor, HudModuleKind kind,
        HudModuleConfiguration module, ShieldDisplayMode shieldDisplay)
    {
        var (showCurrent, showMaximum, showPercentage) = GetHpVisibility(module);
        var formatted = HudFormatting.HitPoints(character.CurrentHp, character.MaxHp,
            showCurrent, showMaximum, showPercentage, module.NumberFormat);
        var showShieldBar = shieldDisplay is ShieldDisplayMode.BarOnly or ShieldDisplayMode.BarAndText;
        if (formatted.Length == 0 && !showShieldBar)
            return;

        var fraction = character.MaxHp == 0
            ? 0f
            : Math.Clamp((float)character.CurrentHp / character.MaxHp, 0f, 1f);
        var shieldFraction = showShieldBar ? Math.Clamp(character.ShieldPercentage / 100f, 0f, 1f) : 0f;
        DrawMeter(fraction, shieldFraction, module.BarHeight, formatted, module.HpTextAlignment,
            ResolveHealthColour(actor, kind, fraction), configuration.Current.Appearance.Shield.ToVector4());
    }

    private void DrawMp(ICharacter character, HudModuleConfiguration module, MpDisplayMode mode)
    {
        if (mode == MpDisplayMode.Off || character.MaxMp == 0)
            return;

        var current = character.CurrentMp;
        var maximum = character.MaxMp;
        var text = $"MP  {HudFormatting.Number(current, module.NumberFormat)} / "
                   + HudFormatting.Number(maximum, module.NumberFormat);
        if (mode == MpDisplayMode.TextOnly)
        {
            ImGui.TextUnformatted(text);
            return;
        }

        var fraction = Math.Clamp((float)current / maximum, 0f, 1f);
        var overlay = mode == MpDisplayMode.BarAndText ? text : string.Empty;
        DrawMeter(fraction, 0f, module.BarHeight, overlay, module.HpTextAlignment,
            configuration.Current.Appearance.Mp.ToVector4(), Vector4.Zero);
    }

    private void DrawShieldText(ICharacter character, ShieldDisplayMode mode)
    {
        if (character.ShieldPercentage == 0
            || mode is not (ShieldDisplayMode.TextOnly or ShieldDisplayMode.BarAndText))
            return;
        ImGui.TextColored(configuration.Current.Appearance.Shield.ToVector4(),
            $"Shield  {HudFormatting.Shield(character.MaxHp, character.ShieldPercentage)}");
    }

    private void DrawCast(IBattleChara actor, HudModuleConfiguration module,
        bool showName, bool showBar, bool showPercentage)
    {
        if (!actor.IsCasting)
            return;
        var total = actor.TotalCastTime;
        var current = actor.CurrentCastTime;
        var fraction = total <= 0f ? 0f : Math.Clamp(current / total, 0f, 1f);
        var name = showName ? data.GetCastName(actor) : string.Empty;
        var percentage = showPercentage ? HudFormatting.CastPercentage(current, total) : string.Empty;
        var overlay = (name.Length, percentage.Length) switch
        {
            (> 0, > 0) => $"{name} — {percentage}",
            (> 0, _) => name,
            (_, > 0) => percentage,
            _ => string.Empty,
        };
        if (showBar)
        {
            DrawMeter(fraction, 0f, module.BarHeight, overlay, module.HpTextAlignment,
                SentinelPalette.AccentBlue, Vector4.Zero);
        }
        else if (overlay.Length > 0)
        {
            ImGui.TextUnformatted(overlay);
        }
    }

    private static void DrawMeter(float fraction, float shieldFraction, float height,
        string overlay, HudTextAlignment alignment, Vector4 fillColour, Vector4 shieldColour)
    {
        var width = Math.Max(60f, ImGui.GetContentRegionAvail().X);
        var size = new Vector2(width, height);
        var minimum = ImGui.GetCursorScreenPos();
        var maximum = minimum + size;
        ImGui.Dummy(size);
        var drawList = ImGui.GetWindowDrawList();
        var background = new Vector4(0.045f, 0.05f, 0.065f, 0.92f);
        drawList.AddRectFilled(minimum, maximum, ImGui.ColorConvertFloat4ToU32(background), 2f);

        var shieldLayout = ShieldBarPolicy.Calculate(fraction, shieldFraction);
        var hpEnd = minimum.X + (width * shieldLayout.HealthEnd);
        if (hpEnd > minimum.X)
            drawList.AddRectFilled(minimum, new Vector2(hpEnd, maximum.Y),
                ImGui.ColorConvertFloat4ToU32(fillColour), 2f);
        if (shieldLayout.HasOverlay)
        {
            var overlayStart = minimum.X + (width * shieldLayout.ShieldOverlayStart);
            drawList.AddRectFilled(new Vector2(overlayStart, minimum.Y),
                new Vector2(hpEnd, maximum.Y), ImGui.ColorConvertFloat4ToU32(shieldColour), 2f);
        }
        if (shieldLayout.HasExtension)
        {
            var extensionEnd = minimum.X + (width * shieldLayout.ShieldExtensionEnd);
            drawList.AddRectFilled(new Vector2(hpEnd, minimum.Y),
                new Vector2(extensionEnd, maximum.Y), ImGui.ColorConvertFloat4ToU32(shieldColour), 2f);
        }
        drawList.AddRect(minimum, maximum, 0xA0000000, 2f);
        if (overlay.Length == 0)
            return;

        var textSize = ImGui.CalcTextSize(overlay);
        var textX = alignment switch
        {
            HudTextAlignment.Left => minimum.X + 5f,
            HudTextAlignment.Right => maximum.X - textSize.X - 5f,
            _ => minimum.X + ((width - textSize.X) * 0.5f),
        };
        var textPosition = new Vector2(Math.Max(minimum.X + 3f, textX),
            minimum.Y + ((height - textSize.Y) * 0.5f));
        drawList.PushClipRect(minimum, maximum, true);
        drawList.AddText(textPosition + Vector2.One, 0xD0000000, overlay);
        drawList.AddText(textPosition, 0xFFF5F5F5, overlay);
        drawList.PopClipRect();
    }

    private Vector4 ResolveHealthColour(IGameObject actor, HudModuleKind kind, float healthFraction)
    {
        var appearance = configuration.Current.Appearance;
        var mode = kind == HudModuleKind.Player
            ? appearance.PlayerHpColourMode
            : appearance.TargetHpColourMode;
        Vector4 staticColour;
        if (kind == HudModuleKind.Player)
        {
            staticColour = appearance.PlayerHealth.ToVector4();
        }
        else if (actor is ICharacter character && character.StatusFlags.HasFlag(StatusFlags.Hostile))
        {
            staticColour = appearance.HostileHealth.ToVector4();
        }
        else if (actor.ObjectKind == ObjectKind.Pc
                 || actor is ICharacter { StatusFlags: var flags }
                 && (flags.HasFlag(StatusFlags.PartyMember)
                     || flags.HasFlag(StatusFlags.AllianceMember)
                     || flags.HasFlag(StatusFlags.Friend)))
        {
            staticColour = appearance.FriendlyHealth.ToVector4();
        }
        else
        {
            staticColour = appearance.NeutralHealth.ToVector4();
        }

        return HpColourPolicy.Resolve(mode, healthFraction, staticColour);
    }

    private void DrawStatuses(IBattleChara actor)
    {
        var statuses = data.GetStatusSummary(actor);
        if (statuses.Length > 0)
            ImGui.TextWrapped(statuses);
    }

    private RuntimeVisibilityState GetRuntimeVisibilityState()
        => new(clientState.IsLoggedIn, condition[ConditionFlag.InCombat],
            condition.Any(ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56,
                ConditionFlag.BoundByDuty95));

    private static bool ShouldDrawModule(HudModuleConfiguration module, bool locked,
        RuntimeVisibilityState runtime)
        => module.Enabled && (!locked || HudVisibilityPolicy.ShouldShowModule(module.Visibility,
            runtime.IsLoggedIn, runtime.IsInCombat, runtime.IsInDuty));

    private static string AppendHeaderPart(string current, string next)
        => current.Length == 0 ? next : $"{current}    {next}";

    private static (bool Current, bool Maximum, bool Percentage) GetHpVisibility(HudModuleConfiguration module)
        => module switch
        {
            PlayerModuleConfiguration player => (player.ShowCurrentHp, player.ShowMaximumHp,
                player.ShowHpPercentage),
            TargetModuleConfiguration target => (target.ShowCurrentHp, target.ShowMaximumHp,
                target.ShowHpPercentage),
            FocusTargetModuleConfiguration focus => (focus.ShowCurrentHp, focus.ShowMaximumHp,
                focus.ShowHpPercentage),
            TargetOfTargetModuleConfiguration targetOfTarget => (targetOfTarget.ShowCurrentHp,
                targetOfTarget.ShowMaximumHp, targetOfTarget.ShowHpPercentage),
            _ => (false, false, false),
        };

    private void RecordState(Configuration config)
    {
        var state = new HudDiagnosticState(config.Enabled, PlayerVisible, TargetVisible,
            FocusTargetVisible, TargetOfTargetVisible, TargetResolved, FocusTargetResolved,
            TargetOfTargetResolved, config.SelfHighlight.Mode, SelfHighlightActive,
            SelfHighlightState, config.PlayerPositionMarker.Mode, PositionMarkerActive,
            PositionMarkerState, config.Target.NativeHpOverlay.Mode, NativeTargetOverlayActive,
            NativeTargetOverlayState, config.Camera.Enabled, CameraZoomActive, CameraZoomState);
        if (hasDiagnosticState && state == lastDiagnosticState)
            return;
        lastDiagnosticState = state;
        hasDiagnosticState = true;
        diagnostics.Debug(
            $"HUD state changed: player={PlayerVisible}, target={TargetVisible}/{TargetResolved}, "
            + $"focus={FocusTargetVisible}/{FocusTargetResolved}, target-of-target={TargetOfTargetVisible}/{TargetOfTargetResolved}, "
            + $"highlight={config.SelfHighlight.Mode}/{SelfHighlightActive} ({SelfHighlightState}), "
            + $"marker={config.PlayerPositionMarker.Mode}/{PositionMarkerActive} ({PositionMarkerState}), "
            + $"native-target={config.Target.NativeHpOverlay.Mode}/{NativeTargetOverlayActive} ({NativeTargetOverlayState}), "
            + $"camera={config.Camera.Enabled}/{CameraZoomActive} ({CameraZoomState}).");
    }

    private void ResetVisibilityState()
    {
        PlayerVisible = false;
        TargetVisible = false;
        FocusTargetVisible = false;
        TargetOfTargetVisible = false;
        TargetResolved = false;
        FocusTargetResolved = false;
        TargetOfTargetResolved = false;
    }

    private static HudModuleConfiguration GetModule(Configuration config, HudModuleKind kind)
        => kind switch
        {
            HudModuleKind.Player => config.Player,
            HudModuleKind.Target => config.Target,
            HudModuleKind.FocusTarget => config.FocusTarget,
            _ => config.TargetOfTarget,
        };

    private static HudModuleConfiguration GetDefaultModule(HudModuleKind kind)
        => kind switch
        {
            HudModuleKind.Player => HudConfigurationDefaults.CreatePlayer(),
            HudModuleKind.Target => HudConfigurationDefaults.CreateTarget(),
            HudModuleKind.FocusTarget => HudConfigurationDefaults.CreateFocusTarget(),
            _ => HudConfigurationDefaults.CreateTargetOfTarget(),
        };

    private sealed class LayoutRuntimeState
    {
        public bool ApplySavedPosition { get; set; } = true;
        public Vector2 LastContentSize { get; set; } = UnknownWindowSize;
        public float LastEditChromeHeight { get; set; }
        public Vector2 LastViewportPosition { get; set; } = new(float.NaN, float.NaN);
        public Vector2 LastViewportSize { get; set; } = new(float.NaN, float.NaN);
    }

    private readonly record struct RuntimeVisibilityState(bool IsLoggedIn, bool IsInCombat, bool IsInDuty);

    private readonly record struct HudDiagnosticState(
        bool HudEnabled, bool PlayerVisible, bool TargetVisible, bool FocusTargetVisible,
        bool TargetOfTargetVisible, bool TargetResolved, bool FocusTargetResolved,
        bool TargetOfTargetResolved, SelfHighlightMode HighlightMode, bool HighlightActive,
        string HighlightState, SelfHighlightMode MarkerMode, bool MarkerActive, string MarkerState,
        NativeTargetOverlayMode NativeTargetMode, bool NativeTargetActive, string NativeTargetState,
        bool CameraEnabled, bool CameraActive, string CameraState);
}
