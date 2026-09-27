using Dalamud.Plugin.Services;

namespace SentinelHUD.Services;

/// <summary>
/// Resolves an actor again at click time and uses Dalamud's hard-target property.
/// It never simulates a world click or retains a native actor pointer.
/// </summary>
public sealed class ActorTargetingService(ActorReferenceResolver resolver, ITargetManager targetManager)
{
    private readonly ActorReferenceResolver resolver = resolver;
    private readonly ITargetManager targetManager = targetManager;

    public string LastActionResult { get; private set; } = "No click attempted";

    public bool TryTarget(ulong gameObjectId)
        => TryTarget(ActorReference.Direct(gameObjectId));

    public bool TryTarget(ActorReference reference)
    {
        try
        {
            if (!resolver.TryResolve(reference, out var actor, out var failureReason))
            {
                LastActionResult = $"Target request skipped: {failureReason}";
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
