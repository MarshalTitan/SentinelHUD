using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

public interface ISelfHighlightService : IDisposable
{
    bool IsActive { get; }
    string StateReason { get; }
    string AppliedColourName { get; }
    bool SupportsArbitraryColour { get; }
    void Update(SelfHighlightConfiguration configuration, bool hudEnabled);
    void Disable();
}

/// <summary>
/// Applies FFXIV's native draw-object outline directly to the local actor. This changes only render state;
/// no hard, soft, mouseover, controller, interaction, or action target is read or written.
/// </summary>
public sealed unsafe class NativeSelfHighlightService(
    IClientState clientState,
    ICondition condition,
    IGameGui gameGui,
    HudDataService data) : ISelfHighlightService
{
    private readonly IClientState clientState = clientState;
    private readonly ICondition condition = condition;
    private readonly IGameGui gameGui = gameGui;
    private readonly HudDataService data = data;
    private nint highlightedActorAddress;
    private NativeHighlightSelection? lastSelection;

    public bool IsActive { get; private set; }
    public string StateReason { get; private set; } = "Off";
    public string AppliedColourName { get; private set; } = "None";
    public bool SupportsArbitraryColour => false;

    public void Update(SelfHighlightConfiguration configuration, bool hudEnabled)
    {
        var inCombat = condition[ConditionFlag.InCombat];
        var inDuty = condition.Any(
            ConditionFlag.BoundByDuty,
            ConditionFlag.BoundByDuty56,
            ConditionFlag.BoundByDuty95);

        if (!hudEnabled
            || !SelfHighlightPolicy.ShouldRender(configuration.Mode, clientState.IsLoggedIn, inCombat, inDuty))
        {
            Disable();
            StateReason = !hudEnabled
                ? "Sentinel HUD disabled"
                : !clientState.IsLoggedIn
                    ? "Not logged in"
                    : configuration.Mode switch
                    {
                        SelfHighlightMode.Off => "Off",
                        SelfHighlightMode.CombatOnly => "Waiting for combat",
                        SelfHighlightMode.DutyOnly => "Waiting for duty",
                        _ => "Mode condition not met",
                    };
            return;
        }

        if (gameGui.GameUiHidden)
        {
            Disable();
            StateReason = "Game UI hidden";
            return;
        }

        var player = data.LocalPlayer;
        if (player is null || player.Address == nint.Zero)
        {
            Disable();
            StateReason = "Local player unavailable";
            return;
        }

        var native = (GameObject*)player.Address;
        if (native->DrawObject is null)
        {
            Disable();
            StateReason = "Local player model is not ready";
            return;
        }

        var selection = NativeHighlightPolicy.Resolve(configuration);
        var stateChanged = !IsActive || lastSelection != selection;
        native->Highlight(ToNativeColour(selection.Colour), includeMount: true);
        highlightedActorAddress = player.Address;
        IsActive = true;
        AppliedColourName = selection.DisplayName;
        if (stateChanged)
        {
            StateReason = selection.IsExact
                ? $"Native silhouette active ({selection.DisplayName})"
                : $"Native silhouette active ({selection.DisplayName}, nearest safe native colour)";
        }
        lastSelection = selection;
    }

    public void Disable()
    {
        if (highlightedActorAddress != nint.Zero)
        {
            var player = data.LocalPlayer;
            if (player is not null && player.Address == highlightedActorAddress)
            {
                var native = (GameObject*)player.Address;
                if (native->DrawObject is not null)
                    native->Highlight(ObjectHighlightColor.None, includeMount: true);
            }
        }

        highlightedActorAddress = nint.Zero;
        IsActive = false;
        AppliedColourName = "None";
        lastSelection = null;
    }

    public void Dispose() => Disable();

    private static ObjectHighlightColor ToNativeColour(NativeHighlightColour colour)
        => colour switch
        {
            NativeHighlightColour.Red => ObjectHighlightColor.Red,
            NativeHighlightColour.Green => ObjectHighlightColor.Green,
            NativeHighlightColour.Blue => ObjectHighlightColor.Blue,
            NativeHighlightColour.Orange => ObjectHighlightColor.Orange,
            NativeHighlightColour.Magenta => ObjectHighlightColor.Magenta,
            NativeHighlightColour.Black => ObjectHighlightColor.Black,
            _ => ObjectHighlightColor.Yellow,
        };
}
