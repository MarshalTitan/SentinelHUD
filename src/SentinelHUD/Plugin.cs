using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SentinelCore.Configuration;
using SentinelCore.Dalamud.Diagnostics;
using SentinelCore.Diagnostics;
using SentinelCore.Identity;
using SentinelCore.Lifecycle;
using SentinelHUD.Core;
using SentinelHUD.Persistence;
using SentinelHUD.Services;
using SentinelHUD.UI;

namespace SentinelHUD;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/shud";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    private readonly PluginLifetime lifetime;
    private readonly WindowSystem windows = new("SentinelHUD");
    private readonly SentinelIdentity identity;
    private readonly DalamudLoggerAdapter logger;
    private readonly DiagnosticBuffer diagnostics = new(120);
    private readonly DiagnosticTracker diagnosticTracker = new();
    private readonly ConfigurationCoordinator<Configuration> configuration;
    private readonly HudRenderer hudRenderer;
    private readonly QuestConvenienceService questConvenience;
    private readonly ConfigurationWindow configurationWindow;

    public Plugin()
    {
        identity = SentinelIdentity.FromAssembly("SentinelHUD", "Sentinel HUD", "MTitan", typeof(Plugin).Assembly);
        logger = new DalamudLoggerAdapter(PluginLog);
        lifetime = new PluginLifetime(exception => logger.Error("A Sentinel HUD resource failed to dispose.", exception));

        var store = new ResilientConfigurationStore(PluginInterface, diagnostics, logger);
        configuration = lifetime.Add(new ConfigurationCoordinator<Configuration>(
            store,
            static () => new Configuration(),
            static value => HudConfigurationMigrator.Normalize(value),
            saveAfterLoad: false));
        if (!store.ConfigurationFileExisted
            || store.UsedRawFallback
            || store.LoadFailed && store.BackupPath is not null
            || store.SourceVersion.GetValueOrDefault(HudConfigurationData.CurrentVersion)
               < HudConfigurationData.CurrentVersion)
        {
            configuration.SaveNow();
        }

        var data = new HudDataService(ObjectTable, TargetManager, DataManager);
        var highlight = lifetime.Add<ISelfHighlightService>(
            new NativeSelfHighlightService(ClientState, Condition, GameGui, data));
        var positionMarker = new PlayerPositionMarkerRenderer(GameGui);
        var nativeTargetOverlay = new NativeTargetHpOverlayRenderer(GameGui);
        var actorResolver = new ActorReferenceResolver(ObjectTable, TargetManager);
        var actorTargeting = new ActorTargetingService(actorResolver, TargetManager);
        var actorContextMenus = new ActorContextMenuService(actorResolver, GameGui);
        var actorInteractions = new ActorInteractionRenderer(actorTargeting, actorContextMenus);
        var hudEditor = new HudEditorInteractionRenderer(configuration);
        var encounterAwareness = lifetime.Add(new EncounterAwarenessService(
            ObjectTable, DataManager, PluginInterface));
        var antiAfk = new AntiAfkService();
        lifetime.Add(antiAfk.Disable);
        var pvpThreatTracker = new PvPThreatTracker(
            ClientState, Condition, ObjectTable, PartyList, DataManager);
        var threatCounterGlyphs = FontAtlasBuildToolkitUtilities.ToGlyphRange(
            "0123456789", false, false);
        var threatCounterFont = lifetime.Add(PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
            step => step.OnPreBuild(toolkit =>
                toolkit.AddDalamudDefaultFont(128f, threatCounterGlyphs))));
        questConvenience = lifetime.Add(new QuestConvenienceService(
            GameGui, Condition, ObjectTable, DataManager, diagnostics));
        var cameraZoom = lifetime.Add<IExtendedCameraZoomService>(
            new ExtendedCameraZoomService(PluginInterface, ClientState, Condition));
        hudRenderer = new HudRenderer(
            configuration,
            data,
            highlight,
            positionMarker,
            nativeTargetOverlay,
            actorInteractions,
            hudEditor,
            cameraZoom,
            encounterAwareness,
            antiAfk,
            pvpThreatTracker,
            threatCounterFont,
            GameGui,
            ClientState,
            Condition,
            diagnostics);
        configurationWindow = lifetime.Add(new ConfigurationWindow(
            configuration, hudRenderer, questConvenience, diagnostics, store, PluginInterface));
        windows.AddWindow(configurationWindow);
        lifetime.Add(() => windows.RemoveAllWindows());

        Framework.Update += OnFrameworkUpdate;
        lifetime.Add(() => Framework.Update -= OnFrameworkUpdate);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Sentinel HUD settings. Options: lock, unlock, reset, enable, disable, status",
        });
        lifetime.Add(() => CommandManager.RemoveHandler(CommandName));

        PluginInterface.UiBuilder.Draw += Draw;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfiguration;
        PluginInterface.UiBuilder.OpenMainUi += OpenConfiguration;
        lifetime.Add(() =>
        {
            PluginInterface.UiBuilder.Draw -= Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfiguration;
            PluginInterface.UiBuilder.OpenMainUi -= OpenConfiguration;
        });

        diagnostics.Information($"{identity.DiagnosticPrefix} loaded with configuration schema {configuration.Current.Version}.");
        logger.Information($"{identity.DiagnosticPrefix} loaded. Sentinel Core v0.3.0.0 pinned at 520f9b9837dc3286c24d6f812b8348a27f378a3e.");
    }

    public void Dispose()
    {
        diagnostics.Information($"{identity.DiagnosticPrefix} unloading.");
        lifetime.Dispose();
    }

    private void Draw()
    {
        try
        {
            hudRenderer.Draw();
            windows.Draw();
            configuration.FlushIfDue();
        }
        catch (Exception exception)
        {
            if (diagnosticTracker.Throttled("draw-failure", TimeSpan.FromSeconds(15)))
            {
                diagnostics.Error("A HUD draw failed; rendering will retry automatically.", exception);
                logger.Error("Sentinel HUD draw failed; rendering will retry.", exception);
            }
        }
    }

    private void OnFrameworkUpdate(IFramework _)
    {
        try
        {
            hudRenderer.UpdateGameState();
        }
        catch (Exception exception)
        {
            if (diagnosticTracker.Throttled("game-state-update-failure", TimeSpan.FromSeconds(15)))
            {
                diagnostics.Error("An awareness/camera update failed; it will retry automatically.", exception);
                logger.Error("Sentinel HUD awareness/camera update failed; retrying.", exception);
            }
        }

        try
        {
            questConvenience.Update(
                configuration.Current.Convenience,
                ClientState.IsLoggedIn,
                ClientState.TerritoryType);
        }
        catch (Exception exception)
        {
            if (diagnosticTracker.Throttled("quest-convenience-update-failure", TimeSpan.FromSeconds(15)))
            {
                diagnostics.Error("A quest convenience update failed; automation paused for this frame.", exception);
                logger.Error("Sentinel HUD quest convenience update failed; retrying safely.", exception);
            }
        }
    }

    private void OnCommand(string _, string arguments)
    {
        switch (arguments.Trim().ToLowerInvariant())
        {
            case "":
                configurationWindow.Toggle();
                break;
            case "lock":
                SetLocked(true);
                break;
            case "unlock":
                SetLocked(false);
                break;
            case "reset":
                hudRenderer.ResetAllLayouts();
                ChatGui.Print("[Sentinel HUD] Complete HUD layout reset to resolution-safe defaults.");
                break;
            case "enable":
                configuration.Update(config => config.Enabled = true);
                ChatGui.Print("[Sentinel HUD] Enabled.");
                break;
            case "disable":
                configuration.Update(config => config.Enabled = false);
                ChatGui.Print("[Sentinel HUD] Disabled. Configuration remains available through /shud.");
                break;
            case "status":
                ChatGui.Print(
                    $"[Sentinel HUD] enabled={configuration.Current.Enabled}, locked={configuration.Current.Locked}, "
                    + $"target={hudRenderer.TargetResolved}, focus={hudRenderer.FocusTargetResolved}, "
                    + $"highlight={configuration.Current.SelfHighlight.Mode}/{hudRenderer.SelfHighlightActive}, "
                    + $"marker={hudRenderer.PositionMarkerActive}, camera={hudRenderer.CameraZoomActive}.");
                break;
            case "help":
                ChatGui.Print("[Sentinel HUD] /shud | lock | unlock | reset | enable | disable | status");
                break;
            default:
                ChatGui.PrintError("[Sentinel HUD] Unknown option. Use /shud help.");
                break;
        }
    }

    private void SetLocked(bool locked)
    {
        configuration.Update(config => config.Locked = locked);
        hudRenderer.RequestRepositionAll();
        ChatGui.Print($"[Sentinel HUD] HUD {(locked ? "locked" : "unlocked for editing")}.");
    }

    private void OpenConfiguration() => configurationWindow.IsOpen = true;
}
