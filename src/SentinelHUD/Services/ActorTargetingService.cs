using Dalamud.Plugin.Services;

namespace SentinelHUD.Services;

/// <summary>
/// Resolves an actor again at click time and uses Dalamud's hard-target property.
/// It never simulates a world click or retains a native actor pointer.
/// </summary>
public sealed class ActorTargetingService(IObjectTable objectTable, ITargetManager targetManager)
{
    private readonly IObjectTable objectTable = objectTable;
    private readonly ITargetManager targetManager = targetManager;

    public string LastActionResult { get; private set; } = "No click attempted";

    public bool TryTarget(ulong gameObjectId)
    {
        try
        {
            var actor = objectTable.SearchById(gameObjectId);
            if (actor is null || !actor.IsValid() || actor.Address == nint.Zero)
            {
                LastActionResult = "Actor left the object table before the click";
                return false;
            }
            if (!actor.IsTargetable)
            {
                LastActionResult = "Resolved actor is not currently targetable";
                return false;
            }

            targetManager.Target = actor;
            LastActionResult = "Hard target changed through Dalamud target manager";
            return true;
        }
        catch
        {
            LastActionResult = "Target request failed safely";
            return false;
        }
    }
}
