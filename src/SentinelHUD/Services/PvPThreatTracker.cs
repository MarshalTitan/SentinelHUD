using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

public sealed record ObservedPvPTargeter(
    ulong GameObjectId,
    uint EntityId,
    string Name,
    uint JobId,
    string JobAbbreviation,
    float Distance,
    PvPThreatObservationSource Source);

public sealed record PvPThreatSnapshot(
    DateTime CapturedAtUtc,
    bool Active,
    bool IsPvP,
    bool IsFrontline,
    byte LocalBattalion,
    bool ClassificationAuthoritative,
    int ObservedPlayerCount,
    int ObservedEnemyCount,
    int NearbyEnemyCount,
    int NearbyFriendlyCount,
    IReadOnlyList<ObservedPvPTargeter> Targeters,
    PvPThreatLevel Level,
    PvPThreatObservationSource Source,
    string PvPMode,
    string Explanation)
{
    public int TargeterCount => Targeters.Count;

    public static PvPThreatSnapshot Inactive(string explanation, bool isPvP = false,
        bool isFrontline = false, string pvpMode = "None") =>
        new(DateTime.UtcNow, false, isPvP, isFrontline, byte.MaxValue, false,
            0, 0, 0, 0, [], PvPThreatLevel.None,
            PvPThreatObservationSource.Unavailable, pvpMode, explanation);
}

public sealed class PvPThreatTracker
{
    private const float NearbyRadius = 18f;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(125);

    private readonly IClientState clientState;
    private readonly ICondition condition;
    private readonly IObjectTable objects;
    private readonly IPartyList partyList;
    private readonly IDataManager data;
    private readonly List<PlayerObservation> observations = new(72);
    private readonly List<ObservedPvPTargeter> targeters = new(16);
    private readonly HashSet<uint> rosterEntityIds = [];
    private readonly HashSet<ulong> rosterObjectIds = [];
    private readonly HashSet<uint> friendlyEntityIds = [];
    private readonly Dictionary<uint, string> jobAbbreviations = [];
    private DateTime nextUpdateUtc = DateTime.MinValue;
    private uint cachedTerritoryId = uint.MaxValue;
    private bool cachedTerritoryIsFrontline;

    public PvPThreatTracker(IClientState clientState, ICondition condition, IObjectTable objects,
        IPartyList partyList, IDataManager data)
    {
        this.clientState = clientState;
        this.condition = condition;
        this.objects = objects;
        this.partyList = partyList;
        this.data = data;
    }

    public PvPThreatSnapshot Current { get; private set; } =
        PvPThreatSnapshot.Inactive("Targeting Me Counter has not sampled game state yet.");

    public void Update(TargetingMeCounterConfiguration config, bool hudEnabled)
    {
        if (!hudEnabled || !config.Enabled)
        {
            SetInactive(hudEnabled
                ? "Targeting Me Counter is disabled."
                : "Sentinel HUD is disabled.");
            return;
        }

        bool isPvP;
        bool isFrontline;
        string mode;
        try
        {
            isPvP = clientState.IsPvPExcludingDen;
            isFrontline = IsCurrentTerritoryFrontline(isPvP);
            mode = isFrontline ? "Frontline" : isPvP ? "Other PvP duty" : "Not in PvP";
        }
        catch (Exception exception)
        {
            SetInactive($"PvP state could not be resolved safely ({exception.GetType().Name}).");
            return;
        }
        if (!isPvP)
        {
            SetInactive("Counter is inactive outside PvP content.", false, false, mode);
            return;
        }

        if (config.PvPVisibility == PvPThreatVisibility.FrontlineOnly && !isFrontline)
        {
            SetInactive("Visibility is Frontline Only and the current PvP duty is not Frontline.",
                true, false, mode);
            return;
        }

        var now = DateTime.UtcNow;
        if (now < nextUpdateUtc)
            return;
        nextUpdateUtc = now + RefreshInterval;

        var local = objects.LocalPlayer;
        if (local is null || local.EntityId == 0)
        {
            SetInactive("Local player is unavailable.", true, isFrontline, mode);
            return;
        }
        if (local.IsDead || local.CurrentHp == 0)
        {
            SetInactive("Local player is dead; the observation set was cleared for respawn.",
                true, isFrontline, mode);
            return;
        }

        try
        {
            Capture(now, local, isFrontline, mode);
        }
        catch (Exception exception)
        {
            SetInactive($"Threat observation failed closed ({exception.GetType().Name}).",
                true, isFrontline, mode);
        }
    }

    private void Capture(DateTime capturedAtUtc, IPlayerCharacter local, bool isFrontline,
        string pvpMode)
    {
        observations.Clear();
        targeters.Clear();
        CaptureRoster(local);

        var localBattalion = ReadBattalion(local);
        var hasBattalionSource = isFrontline
                                 && PvPThreatPolicy.IsValidFrontlineBattalion(localBattalion);
        var invalidBattalionCount = 0;
        var battalionFriendlyCount = 1;
        friendlyEntityIds.Clear();
        friendlyEntityIds.Add(local.EntityId);

        foreach (var player in objects.PlayerObjects)
        {
            if (player.EntityId == 0 || player.EntityId == local.EntityId)
                continue;

            var battalion = ReadBattalion(player);
            var flags = player.StatusFlags;
            var rosterMember = rosterEntityIds.Contains(player.EntityId)
                               || rosterObjectIds.Contains(player.GameObjectId);
            observations.Add(new PlayerObservation(
                player.GameObjectId,
                player.EntityId,
                player.Name.TextValue,
                player.ClassJob.RowId,
                player.Position,
                player.TargetObjectId,
                battalion,
                flags.HasFlag(StatusFlags.PartyMember),
                flags.HasFlag(StatusFlags.AllianceMember),
                flags.HasFlag(StatusFlags.Hostile),
                rosterMember,
                player.IsDead || player.CurrentHp == 0,
                player.IsTargetable));

            if (!hasBattalionSource)
                continue;
            if (!PvPThreatPolicy.IsValidFrontlineBattalion(battalion))
            {
                invalidBattalionCount++;
                continue;
            }

            if (battalion == localBattalion)
            {
                battalionFriendlyCount++;
                friendlyEntityIds.Add(player.EntityId);
            }
        }

        var classificationAuthoritative = hasBattalionSource
                                          && invalidBattalionCount == 0
                                          && battalionFriendlyCount is >= 1 and <= 24
                                          && friendlyEntityIds.Count == battalionFriendlyCount;
        var source = classificationAuthoritative
            ? PvPThreatObservationSource.BattalionTeam
            : PvPThreatObservationSource.NativeHostileFlagFallback;
        var nearbyEnemies = 0;
        var nearbyFriendlies = 1;
        var observedEnemies = 0;

        foreach (var player in observations)
        {
            var isEnemy = classificationAuthoritative
                ? PvPThreatPolicy.IsBattalionEnemy(localBattalion, player.Battalion)
                : PvPThreatPolicy.IsFallbackHostile(
                    false,
                    player.HostileFlag,
                    player.PartyMemberFlag,
                    player.AllianceMemberFlag,
                    player.IsRosterMember);
            var isFriendly = classificationAuthoritative
                ? player.Battalion == localBattalion
                : player.IsRosterMember || player.PartyMemberFlag || player.AllianceMemberFlag;
            var distance = HorizontalDistance(player.Position, local.Position);

            if (isEnemy)
            {
                observedEnemies++;
                if (!player.IsDead && player.IsTargetable && distance <= NearbyRadius)
                    nearbyEnemies++;
                if (!player.IsDead && player.IsTargetable
                                   && player.TargetObjectId == local.GameObjectId)
                {
                    targeters.Add(new ObservedPvPTargeter(
                        player.GameObjectId,
                        player.EntityId,
                        player.Name,
                        player.JobId,
                        ResolveJobAbbreviation(player.JobId),
                        distance,
                        source));
                }
            }
            else if (isFriendly && !player.IsDead && distance <= NearbyRadius)
            {
                nearbyFriendlies++;
            }
        }

        targeters.Sort(static (left, right) => left.Distance.CompareTo(right.Distance));
        var snapshotTargeters = targeters.ToArray();
        var level = PvPThreatPolicy.EvaluateLevel(snapshotTargeters.Length, nearbyEnemies);
        var classification = classificationAuthoritative
            ? $"Authoritative zero-based Battalion team {localBattalion}; different valid teams are enemies."
            : hasBattalionSource
                ? $"Battalion classification failed validation ({invalidBattalionCount} invalid observation(s)); using conservative native Hostile plus party/alliance/roster exclusions."
                : "Authoritative Frontline Battalion data is unavailable; using conservative native Hostile plus party/alliance/roster exclusions.";
        Current = new PvPThreatSnapshot(
            capturedAtUtc,
            true,
            true,
            isFrontline,
            localBattalion,
            classificationAuthoritative,
            observations.Count,
            observedEnemies,
            nearbyEnemies,
            nearbyFriendlies,
            snapshotTargeters,
            level,
            source,
            pvpMode,
            $"{classification} Currently observed enemy hard targets={snapshotTargeters.Length}; "
            + $"nearby enemies/allies within {NearbyRadius:0}y={nearbyEnemies}/{nearbyFriendlies}.");
    }

    private void CaptureRoster(IPlayerCharacter local)
    {
        rosterEntityIds.Clear();
        rosterObjectIds.Clear();
        rosterEntityIds.Add(local.EntityId);
        rosterObjectIds.Add(local.GameObjectId);
        foreach (var member in partyList)
        {
            if (member.EntityId != 0)
                rosterEntityIds.Add(member.EntityId);
            if (member.GameObject is { } gameObject)
                rosterObjectIds.Add(gameObject.GameObjectId);
        }
    }

    private bool IsCurrentTerritoryFrontline(bool isPvP)
    {
        if (!isPvP || !IsBoundByDuty())
            return false;
        if (cachedTerritoryId == clientState.TerritoryType)
            return cachedTerritoryIsFrontline;

        cachedTerritoryId = clientState.TerritoryType;
        var territory = data.GetExcelSheet<TerritoryType>()
            .FirstOrDefault(row => row.RowId == cachedTerritoryId);
        var content = territory.RowId == 0 ? default : territory.ContentFinderCondition.Value;
        cachedTerritoryIsFrontline = territory.RowId != 0
                                     && territory.IsPvpZone
                                     && content.RowId != 0
                                     && content.PvP
                                     && content.DailyFrontlineChallenge;
        return cachedTerritoryIsFrontline;
    }

    private bool IsBoundByDuty() =>
        condition[ConditionFlag.BoundByDuty]
        || condition[ConditionFlag.BoundByDuty56]
        || condition[ConditionFlag.BoundByDuty95];

    private string ResolveJobAbbreviation(uint jobId)
    {
        if (jobAbbreviations.TryGetValue(jobId, out var abbreviation))
            return abbreviation;
        var job = data.GetExcelSheet<ClassJob>().FirstOrDefault(row => row.RowId == jobId);
        abbreviation = job.RowId == 0 ? $"Job {jobId}" : job.Abbreviation.ToString();
        jobAbbreviations[jobId] = abbreviation;
        return abbreviation;
    }

    private void SetInactive(string explanation, bool isPvP = false, bool isFrontline = false,
        string pvpMode = "None")
    {
        if (!Current.Active && Current.Explanation == explanation
                            && Current.IsPvP == isPvP && Current.IsFrontline == isFrontline)
            return;
        Current = PvPThreatSnapshot.Inactive(explanation, isPvP, isFrontline, pvpMode);
    }

    private static unsafe byte ReadBattalion(IPlayerCharacter player)
    {
        var native = (Character*)player.Address;
        return native is null ? byte.MaxValue : native->Battalion;
    }

    private static float HorizontalDistance(Vector3 left, Vector3 right) =>
        Vector2.Distance(new Vector2(left.X, left.Z), new Vector2(right.X, right.Z));

    private readonly record struct PlayerObservation(
        ulong GameObjectId,
        uint EntityId,
        string Name,
        uint JobId,
        Vector3 Position,
        ulong TargetObjectId,
        byte Battalion,
        bool PartyMemberFlag,
        bool AllianceMemberFlag,
        bool HostileFlag,
        bool IsRosterMember,
        bool IsDead,
        bool IsTargetable);
}
