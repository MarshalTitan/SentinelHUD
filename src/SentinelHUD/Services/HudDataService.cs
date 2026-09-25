using System.Globalization;
using System.Numerics;
using System.Text;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using SentinelCore.Dalamud.Jobs;
using SentinelCore.Jobs;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace SentinelHUD.Services;

public sealed class HudDataService
{
    private readonly IObjectTable objectTable;
    private readonly ITargetManager targetManager;
    private readonly IDataManager dataManager;
    private readonly Dictionary<uint, JobMetadata> jobs;
    private readonly Dictionary<uint, string> actionNames = new();
    private readonly Dictionary<uint, string> statusNames = new();
    private readonly StringBuilder statusBuilder = new(192);

    public HudDataService(IObjectTable objectTable, ITargetManager targetManager, IDataManager dataManager)
    {
        this.objectTable = objectTable;
        this.targetManager = targetManager;
        this.dataManager = dataManager;
        jobs = new DalamudJobMetadataProvider(dataManager)
            .Snapshot()
            .ToDictionary(job => job.ClassJobId);
    }

    public IPlayerCharacter? LocalPlayer => IsUsable(objectTable.LocalPlayer) ? objectTable.LocalPlayer : null;

    public IGameObject? Target => Validate(targetManager.Target);

    public IGameObject? FocusTarget => Validate(targetManager.FocusTarget);

    public IGameObject? ResolveTargetOfTarget(IGameObject? target)
    {
        if (target is null
            || !IsUsable(target)
            || target.TargetObjectId == 0
            || target.TargetObjectId == ulong.MaxValue)
            return null;
        return Validate(objectTable.SearchById(target.TargetObjectId));
    }

    public JobMetadata? GetJob(IGameObject actor)
        => actor is ICharacter character && jobs.TryGetValue(character.ClassJob.RowId, out var metadata)
            ? metadata
            : null;

    public string GetRoleName(IGameObject actor)
        => GetJob(actor)?.Category switch
        {
            JobCategory.Tank => "Tank",
            JobCategory.Healer => "Healer",
            JobCategory.MeleeDps => "Melee DPS",
            JobCategory.PhysicalRangedDps => "Physical Ranged DPS",
            JobCategory.MagicalRangedDps => "Magical Ranged DPS",
            JobCategory.LimitedJob => "Limited Job",
            JobCategory.Crafter => "Crafter",
            JobCategory.Gatherer => "Gatherer",
            _ => "Other",
        };

    public RoleHue GetRoleHue(IGameObject actor) => GetJob(actor)?.RoleHue ?? RoleHue.Neutral;

    public float GetDistance(IGameObject actor)
    {
        var player = LocalPlayer;
        if (player is null)
            return float.NaN;
        var centreDistance = Vector3.Distance(player.Position, actor.Position);
        return Math.Max(0f, centreDistance - player.HitboxRadius - actor.HitboxRadius);
    }

    public string GetCastName(IBattleChara actor)
    {
        if (!actor.IsCasting || actor.CastActionId == 0)
            return string.Empty;
        if (actionNames.TryGetValue(actor.CastActionId, out var cached))
            return cached;

        var name = dataManager.GetExcelSheet<LuminaAction>().TryGetRow(actor.CastActionId, out var action)
            ? action.Name.ToString()
            : string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            name = string.Create(CultureInfo.InvariantCulture, $"Action {actor.CastActionId}");
        actionNames[actor.CastActionId] = name;
        return name;
    }

    public string GetStatusSummary(IBattleChara actor, int maximumStatuses = 5)
    {
        statusBuilder.Clear();
        var written = 0;
        foreach (var status in actor.StatusList)
        {
            if (status.StatusId == 0)
                continue;
            var name = GetStatusName(status.StatusId);
            if (name.Length == 0)
                continue;
            if (written > 0)
                statusBuilder.Append("  •  ");
            statusBuilder.Append(name);
            if (status.RemainingTime > 0f && status.RemainingTime < 3600f)
                statusBuilder.Append(CultureInfo.InvariantCulture, $" {status.RemainingTime:0}s");
            written++;
            if (written >= maximumStatuses)
                break;
        }
        return statusBuilder.ToString();
    }

    private string GetStatusName(uint statusId)
    {
        if (statusNames.TryGetValue(statusId, out var cached))
            return cached;
        var name = dataManager.GetExcelSheet<Status>().TryGetRow(statusId, out var status)
            ? status.Name.ToString()
            : string.Empty;
        statusNames[statusId] = name;
        return name;
    }

    private static IGameObject? Validate(IGameObject? actor) => IsUsable(actor) ? actor : null;

    private static bool IsUsable(IGameObject? actor)
    {
        try
        {
            return actor is not null && actor.IsValid() && actor.Address != nint.Zero;
        }
        catch
        {
            return false;
        }
    }
}
