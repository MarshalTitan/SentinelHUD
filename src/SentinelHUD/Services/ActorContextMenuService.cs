using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace SentinelHUD.Services;

/// <summary>
/// Opens the same target context path used by the stock HUD. FFXIV builds the menu at invocation
/// time, so actor type, relationship, party state and content restrictions remain native behavior.
/// </summary>
public sealed unsafe class ActorContextMenuService(IObjectTable objectTable)
{
    private readonly IObjectTable objectTable = objectTable;

    public string LastActionResult { get; private set; } = "No actor context menu requested";

    public bool TryOpen(ulong gameObjectId)
    {
        if (gameObjectId is 0 or ulong.MaxValue)
        {
            LastActionResult = "Actor context menu skipped: invalid object ID";
            return false;
        }

        var actor = objectTable.SearchById(gameObjectId);
        if (actor is null || !actor.IsValid() || actor.Address == nint.Zero)
        {
            LastActionResult = "Actor context menu skipped: actor is no longer available";
            return false;
        }

        var agent = AgentHUD.Instance();
        if (agent is null)
        {
            LastActionResult = "Actor context menu unavailable: HUD agent is not ready";
            return false;
        }

        agent->OpenContextMenuFromTarget((GameObject*)actor.Address);
        LastActionResult = $"Opened native context menu for {actor.Name.TextValue}";
        return true;
    }
}
