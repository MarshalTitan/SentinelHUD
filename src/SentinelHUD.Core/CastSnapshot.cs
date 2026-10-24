namespace SentinelHUD.Core;

/// <summary>A frame-local copy of one actor's cast; never holds a native pointer.</summary>
public readonly record struct CastSnapshot(ulong ActorId, bool IsCasting, uint ActionId,
    byte ActionType, float Current, float Total)
{
    public float Progress => IsCasting && Total > 0f ? Current / Total : 0f;

    public static CastSnapshot Create(ulong actorId, bool casting, uint actionId, byte actionType,
        float current, float total)
        => SameActor(actorId, actorId) && casting && actionId != 0 && float.IsFinite(current) && float.IsFinite(total) && total > 0f
            ? new(actorId, true, actionId, actionType, Math.Clamp(current, 0f, total), total)
            : new(actorId, false, 0, 0, 0f, 0f);

    public static bool SameActor(ulong a, ulong b) => a is not (0 or ulong.MaxValue) && a == b;

    public static CastSnapshot ForTarget(ulong playerId, CastSnapshot player, ulong targetId, CastSnapshot target)
    {
        if (SameActor(playerId, targetId))
            return SameActor(player.ActorId, playerId) ? player : default;
        return SameActor(target.ActorId, targetId) ? target : default;
    }
}
