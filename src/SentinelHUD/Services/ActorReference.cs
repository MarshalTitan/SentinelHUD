using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace SentinelHUD.Services;

public enum ActorReferenceKind
{
    DirectObjectId,
    FocusTargetTarget,
}

/// <summary>
/// Describes how an interaction should resolve its represented actor at click time.
/// Relationship references deliberately do not retain a child object or native pointer.
/// </summary>
public readonly record struct ActorReference(ActorReferenceKind Kind, ulong ObjectId)
{
    public static ActorReference Direct(ulong objectId)
        => new(ActorReferenceKind.DirectObjectId, objectId);

    public static ActorReference CurrentFocusTargetTarget
        => new(ActorReferenceKind.FocusTargetTarget, 0);

    public bool IsWellFormed
        => Kind == ActorReferenceKind.FocusTargetTarget
           || ObjectId is not 0 and not ulong.MaxValue;
}

/// <summary>
/// Re-resolves direct and relationship-based actor references through the current object table.
/// </summary>
public sealed class ActorReferenceResolver(IObjectTable objectTable, ITargetManager targetManager)
{
    private readonly IObjectTable objectTable = objectTable;
    private readonly ITargetManager targetManager = targetManager;

    public bool TryResolve(ActorReference reference, out IGameObject actor, out string failureReason)
    {
        actor = null!;
        if (!reference.IsWellFormed)
        {
            failureReason = "invalid object reference";
            return false;
        }

        var objectId = reference.ObjectId;
        if (reference.Kind == ActorReferenceKind.FocusTargetTarget)
        {
            var focus = targetManager.FocusTarget;
            if (focus is null || !focus.IsValid() || focus.Address == nint.Zero)
            {
                failureReason = "Focus Target is no longer available";
                return false;
            }

            objectId = focus.TargetObjectId;
            if (objectId is 0 or ulong.MaxValue)
            {
                failureReason = "Focus Target no longer has a resolvable target";
                return false;
            }
        }

        var resolved = objectTable.SearchById(objectId);
        if (resolved is null || !resolved.IsValid() || resolved.Address == nint.Zero)
        {
            failureReason = "represented object left the object table";
            return false;
        }

        actor = resolved;
        failureReason = string.Empty;
        return true;
    }
}
