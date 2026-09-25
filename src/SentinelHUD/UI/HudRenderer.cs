using System.Numerics;
using Dalamud.Bindings.ImGui;
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
    private static readonly Vector2 UnknownWindowSize = new(260f, 120f);

    private readonly ConfigurationCoordinator<Configuration> configuration;
    private readonly HudDataService data;
    private readonly SelfHighlightRenderer selfHighlight;
    private readonly IGameGui gameGui;
    private readonly DiagnosticBuffer diagnostics;
    private readonly DiagnosticTracker diagnosticTracker;
    private readonly Dictionary<HudModuleKind, LayoutRuntimeState> layoutStates = new();

    public HudRenderer(
        ConfigurationCoordinator<Configuration> configuration,
        HudDataService data,
        SelfHighlightRenderer selfHighlight,
        IGameGui gameGui,
        DiagnosticBuffer diagnostics,
        DiagnosticTracker diagnosticTracker)
    {
        this.configuration = configuration;
        this.data = data;
        this.selfHighlight = selfHighlight;
        this.gameGui = gameGui;
        this.diagnostics = diagnostics;
        this.diagnosticTracker = diagnosticTracker;
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

    public void Draw()
    {
        var config = configuration.Current;
        ResetVisibilityState();
        if (!config.Enabled || gameGui.GameUiHidden)
        {
            RecordState(config);
            return;
        }

        selfHighlight.Draw(config.SelfHighlight);

        var player = data.LocalPlayer;
        var target = data.Target;
        var focus = data.FocusTarget;
        var targetOfTarget = data.ResolveTargetOfTarget(target);
        TargetResolved = target is not null;
        FocusTargetResolved = focus is not null;
        TargetOfTargetResolved = targetOfTarget is not null;

        if (config.Player.Enabled && (player is not null || !config.Locked))
        {
            DrawModule(HudModuleKind.Player, config.Player, config, player, DrawPlayer);
            PlayerVisible = true;
        }
        if (config.Target.Enabled && (target is not null || !config.Locked))
        {
            DrawModule(HudModuleKind.Target, config.Target, config, target, DrawTarget);
            TargetVisible = true;
        }
        if (config.FocusTarget.Enabled && (focus is not null || !config.Locked))
        {
            DrawModule(HudModuleKind.FocusTarget, config.FocusTarget, config, focus, DrawFocusTarget);
            FocusTargetVisible = true;
        }
        if (config.TargetOfTarget.Enabled && (targetOfTarget is not null || !config.Locked))
        {
            DrawModule(
                HudModuleKind.TargetOfTarget,
                config.TargetOfTarget,
                config,
                targetOfTarget,
                DrawTargetOfTarget);
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
        configuration.Update(config =>
        {
            GetModule(config, kind).Layout = GetDefaultModule(kind).Layout;
        });
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

    private void DrawModule(
        HudModuleKind kind,
        HudModuleConfiguration module,
        Configuration root,
        IGameObject? actor,
        Action<IGameObject?, HudModuleConfiguration> drawContent)
    {
        var viewport = ImGui.GetMainViewport();
        var state = layoutStates[kind];
        var viewportChanged = Vector2.DistanceSquared(viewport.WorkSize, state.LastViewportSize) > 0.25f
                              || Vector2.DistanceSquared(viewport.WorkPos, state.LastViewportPosition) > 0.25f;
        var desiredPosition = LayoutPolicy.ToPixelPosition(
            module.Layout,
            viewport.WorkPos,
            viewport.WorkSize,
            state.LastWindowSize);

        if (root.Locked || state.ApplySavedPosition || viewportChanged)
            ImGui.SetNextWindowPos(desiredPosition, ImGuiCond.Always);

        ImGui.SetNextWindowBgAlpha(module.Opacity * root.GlobalOpacity);
        using var style = SentinelStyleScope.PushWindow(module.Scale * root.GlobalScale);
        var flags = ImGuiWindowFlags.AlwaysAutoResize
                    | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.NoScrollWithMouse
                    | ImGuiWindowFlags.NoSavedSettings
                    | ImGuiWindowFlags.NoDocking
                    | ImGuiWindowFlags.NoNav
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
            ImGui.SetWindowFontScale(module.Scale * root.GlobalScale);
            if (began)
            {
                if (actor is null)
                    SentinelUi.MutedText("No actor resolved — drag this module into place.");
                else
                    drawContent(actor, module);
            }

            var actualPosition = ImGui.GetWindowPos();
            var actualSize = ImGui.GetWindowSize();
            var reachable = LayoutPolicy.KeepReachable(
                actualPosition,
                viewport.WorkPos,
                viewport.WorkSize,
                actualSize);
            if (Vector2.DistanceSquared(actualPosition, reachable) > 0.25f)
            {
                ImGui.SetWindowPos(reachable, ImGuiCond.Always);
                actualPosition = reachable;
            }

            state.LastWindowSize = actualSize.X > 0f && actualSize.Y > 0f ? actualSize : UnknownWindowSize;
            state.LastViewportPosition = viewport.WorkPos;
            state.LastViewportSize = viewport.WorkSize;
            state.ApplySavedPosition = false;

            if (!root.Locked)
            {
                var normalized = LayoutPolicy.ToNormalizedPosition(
                    actualPosition,
                    viewport.WorkPos,
                    viewport.WorkSize,
                    state.LastWindowSize);
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

    private void DrawPlayer(IGameObject? actor, HudModuleConfiguration baseConfiguration)
    {
        var config = (PlayerModuleConfiguration)baseConfiguration;
        if (actor is not ICharacter character)
            return;

        if (config.ShowName)
            ImGui.TextColored(SentinelPalette.HeaderGold, actor.Name.TextValue);
        DrawJobLine(actor, character, config.ShowJob, config.ShowRole, config.ShowLevel);
        DrawHealth(character, config.ShowCurrentHp, config.ShowMaximumHp, config.ShowHpPercentage, actor);
        if (config.ShowMp)
            ImGui.TextUnformatted($"MP  {character.CurrentMp:N0} / {character.MaxMp:N0}");
        if (config.ShowShield && character.ShieldPercentage > 0)
            ImGui.TextColored(SentinelPalette.AccentBlue, $"Shield  {HudFormatting.Shield(character.MaxHp, character.ShieldPercentage)}");
        if (config.ShowStatuses && actor is IBattleChara battleChara)
            DrawStatuses(battleChara);
    }

    private void DrawTarget(IGameObject? actor, HudModuleConfiguration baseConfiguration)
    {
        var config = (TargetModuleConfiguration)baseConfiguration;
        if (actor is null)
            return;
        if (config.ShowName)
            ImGui.TextColored(SentinelPalette.HeaderGold, actor.Name.TextValue);
        if (actor is ICharacter character)
        {
            DrawJobLine(actor, character, config.ShowJob, config.ShowRole, false);
            DrawHealth(character, config.ShowCurrentHp, config.ShowMaximumHp, config.ShowHpPercentage, actor);
            if (config.ShowShield && character.ShieldPercentage > 0)
                ImGui.TextColored(SentinelPalette.AccentBlue, $"Shield  {HudFormatting.Shield(character.MaxHp, character.ShieldPercentage)}");
        }
        if (config.ShowDistance)
            ImGui.TextUnformatted(HudFormatting.Distance(data.GetDistance(actor)));
        if (actor is IBattleChara battleChara)
        {
            DrawCast(battleChara, config.ShowCastName, config.ShowCastBar, config.ShowCastPercentage);
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
            DrawHealth(character, config.ShowCurrentHp, config.ShowMaximumHp, config.ShowHpPercentage, actor);
            if (config.ShowShield && character.ShieldPercentage > 0)
                ImGui.TextColored(SentinelPalette.AccentBlue, $"Shield  {HudFormatting.Shield(character.MaxHp, character.ShieldPercentage)}");
        }
        if (config.ShowDistance)
            ImGui.TextUnformatted(HudFormatting.Distance(data.GetDistance(actor)));
        if (actor is IBattleChara battleChara)
            DrawCast(battleChara, config.ShowCastName, config.ShowCastBar, config.ShowCastPercentage);
    }

    private void DrawTargetOfTarget(IGameObject? actor, HudModuleConfiguration baseConfiguration)
    {
        var config = (TargetOfTargetModuleConfiguration)baseConfiguration;
        if (actor is null)
            return;
        if (config.ShowName)
            ImGui.TextColored(SentinelPalette.HeaderGold, actor.Name.TextValue);
        if (actor is ICharacter character)
            DrawHealth(character, config.ShowCurrentHp, config.ShowMaximumHp, config.ShowHpPercentage, actor);
        if (config.ShowDistance)
            ImGui.TextUnformatted(HudFormatting.Distance(data.GetDistance(actor)));
    }

    private void DrawJobLine(
        IGameObject actor,
        ICharacter character,
        bool showJob,
        bool showRole,
        bool showLevel)
    {
        var drew = false;
        var job = data.GetJob(actor);
        if (showJob)
        {
            ImGui.TextUnformatted(job?.Abbreviation ?? $"Job {character.ClassJob.RowId}");
            drew = true;
        }
        if (showRole)
        {
            if (drew)
                ImGui.SameLine();
            ImGui.TextDisabled(data.GetRoleName(actor));
            drew = true;
        }
        if (showLevel)
        {
            if (drew)
                ImGui.SameLine();
            ImGui.TextDisabled($"Lv. {character.Level}");
        }
    }

    private void DrawHealth(
        ICharacter character,
        bool showCurrent,
        bool showMaximum,
        bool showPercentage,
        IGameObject actor)
    {
        var formatted = HudFormatting.HitPoints(
            character.CurrentHp,
            character.MaxHp,
            showCurrent,
            showMaximum,
            showPercentage);
        if (formatted.Length == 0)
            return;

        var fraction = character.MaxHp == 0 ? 0f : Math.Clamp((float)character.CurrentHp / character.MaxHp, 0f, 1f);
        var palette = SentinelPalette.ForRole(data.GetRoleHue(actor));
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, palette.Active);
        ImGui.ProgressBar(fraction, new Vector2(Math.Max(210f, ImGui.CalcTextSize(formatted).X + 28f), 0f), formatted);
        ImGui.PopStyleColor();
    }

    private void DrawCast(IBattleChara actor, bool showName, bool showBar, bool showPercentage)
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
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, SentinelPalette.AccentBlue);
            ImGui.ProgressBar(fraction, new Vector2(230f, 0f), overlay);
            ImGui.PopStyleColor();
        }
        else if (overlay.Length > 0)
        {
            ImGui.TextUnformatted(overlay);
        }
    }

    private void DrawStatuses(IBattleChara actor)
    {
        var statuses = data.GetStatusSummary(actor);
        if (statuses.Length > 0)
            ImGui.TextWrapped(statuses);
    }

    private void RecordState(Configuration config)
    {
        var signature = string.Join(
            '|',
            config.Enabled,
            PlayerVisible,
            TargetVisible,
            FocusTargetVisible,
            TargetOfTargetVisible,
            TargetResolved,
            FocusTargetResolved,
            TargetOfTargetResolved,
            config.SelfHighlight.Mode,
            SelfHighlightActive,
            SelfHighlightState);
        if (diagnosticTracker.Changed("hud-visibility", signature))
        {
            diagnostics.Debug(
                $"HUD state changed: player={PlayerVisible}, target={TargetVisible}/{TargetResolved}, "
                + $"focus={FocusTargetVisible}/{FocusTargetResolved}, target-of-target={TargetOfTargetVisible}/{TargetOfTargetResolved}, "
                + $"highlight={config.SelfHighlight.Mode}/{SelfHighlightActive} ({SelfHighlightState}).");
        }
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
        public Vector2 LastWindowSize { get; set; } = UnknownWindowSize;
        public Vector2 LastViewportPosition { get; set; } = new(float.NaN, float.NaN);
        public Vector2 LastViewportSize { get; set; } = new(float.NaN, float.NaN);
    }
}
