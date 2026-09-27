using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

/// <summary>
/// Opens the same target context path used by the stock HUD. FFXIV builds the menu at invocation
/// time, so actor type, relationship, party state and content restrictions remain native behavior.
/// </summary>
public sealed unsafe class ActorContextMenuService(ActorReferenceResolver resolver, IGameGui gameGui)
{
    private const string ContextMenuAddonName = "ContextMenu";
    private readonly ActorReferenceResolver resolver = resolver;
    private readonly IGameGui gameGui = gameGui;
    private readonly ContextMenuSuppressionPolicy suppression = new();

    public string LastActionResult { get; private set; } = "No actor context menu requested";
    public bool NativeMenuVisible { get; private set; }
    public bool ShouldSuppressHud { get; private set; }

    public void RefreshVisibility()
    {
        try
        {
            var addon = gameGui.GetAddonByName<AtkUnitBase>(ContextMenuAddonName);
            NativeMenuVisible = addon is not null && addon->IsVisible;
        }
        catch
        {
            NativeMenuVisible = false;
        }

        ShouldSuppressHud = suppression.Update(NativeMenuVisible, Environment.TickCount64);
    }

    public bool TryOpen(ulong gameObjectId)
        => TryOpen(ActorReference.Direct(gameObjectId));

    public bool TryOpen(ActorReference reference)
    {
        if (!resolver.TryResolve(reference, out var actor, out var failureReason))
        {
            LastActionResult = $"Actor context menu skipped: {failureReason}";
            return false;
        }

        var agent = AgentHUD.Instance();
        if (agent is null)
        {
            LastActionResult = "Actor context menu unavailable: HUD agent is not ready";
            return false;
        }

        agent->OpenContextMenuFromTarget((GameObject*)actor.Address);
        suppression.NotifyOpenRequested(Environment.TickCount64);
        ShouldSuppressHud = true;
        LastActionResult = $"Opened native context menu for {actor.Name.TextValue}";
        return true;
    }
}
