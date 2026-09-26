using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using SentinelHUD.Core;
using LuminaAction = Lumina.Excel.Sheets.Action;
using SplatoonGeometryItem = (string id, string source, string Namespace, string layout, string element, string kind, string renderEngine, uint color, System.Numerics.Vector3 center, System.Numerics.Vector3 start, System.Numerics.Vector3 end, float? radius, float? innerRadius, float? outerRadius, float? lineRadius, float? facingRad, float? halfAngleRad, float? angleMinRad, float? angleMaxRad);
using SplatoonGeometrySnapshot = (int version, uint frame, uint territoryId, long generatedAtTickMs, System.Collections.Generic.List<(string id, string source, string Namespace, string layout, string element, string kind, string renderEngine, uint color, System.Numerics.Vector3 center, System.Numerics.Vector3 start, System.Numerics.Vector3 end, float? radius, float? innerRadius, float? outerRadius, float? lineRadius, float? facingRad, float? halfAngleRad, float? angleMinRad, float? angleMaxRad)> items);

namespace SentinelHUD.Services;

public interface IEncounterDangerProvider : IDisposable
{
    string Name { get; }
    string State { get; }
    int HazardCount { get; }
    IReadOnlyList<DangerArea> Hazards { get; }
    void Disable();
}

public sealed class EncounterAwarenessService : IDisposable
{
    private readonly NativeCastDangerProvider native;
    private readonly SplatoonDangerProvider splatoon;

    public EncounterAwarenessService(
        IObjectTable objectTable,
        IDataManager dataManager,
        IDalamudPluginInterface pluginInterface)
    {
        native = new NativeCastDangerProvider(objectTable, dataManager);
        splatoon = new SplatoonDangerProvider(pluginInterface);
    }

    public bool IsEnabled { get; private set; }
    public bool IsPlayerInDanger { get; private set; }
    public int ActiveHazardCount => native.HazardCount + splatoon.HazardCount;
    public string NativeState => native.State;
    public string SplatoonState => splatoon.State;
    public bool SplatoonInstalled => splatoon.IsInstalled;
    public bool SplatoonConnected => splatoon.IsConnected;
    public string ActiveEncounter => ActiveHazardCount > 0
        ? $"Territory {TerritoryId} — {ActiveHazardCount} active hazard(s)"
        : TerritoryId == 0 ? "None" : $"Territory {TerritoryId} — no active hazards";
    public uint TerritoryId { get; private set; }

    public void Update(EncounterAwarenessConfiguration configuration, bool hudEnabled,
        bool isLoggedIn, uint territoryId, IPlayerCharacter? player)
    {
        IsEnabled = hudEnabled && configuration.Enabled && isLoggedIn && player is not null;
        TerritoryId = isLoggedIn ? territoryId : 0;
        if (!IsEnabled)
        {
            native.Disable();
            splatoon.Update(false, false, territoryId);
            IsPlayerInDanger = false;
            return;
        }

        native.Update(configuration.NativeDetectionEnabled, territoryId);
        splatoon.Update(configuration.SplatoonIntegrationEnabled,
            configuration.TreatUnclassifiedSplatoonGeometryAsDanger, territoryId);

        var point = player!.Position;
        IsPlayerInDanger = Contains(native.Hazards, point) || Contains(splatoon.Hazards, point);
    }

    public void Dispose()
    {
        native.Dispose();
        splatoon.Dispose();
    }

    private static bool Contains(IReadOnlyList<DangerArea> hazards, Vector3 point)
    {
        for (var index = 0; index < hazards.Count; index++)
        {
            if (hazards[index].Contains(point))
                return true;
        }
        return false;
    }
}

internal sealed class NativeCastDangerProvider : IEncounterDangerProvider
{
    private const int UpdateIntervalMilliseconds = 50;
    private const int MaximumHazards = 128;
    private readonly IObjectTable objectTable;
    private readonly IDataManager dataManager;
    private readonly List<DangerArea> hazards = new(MaximumHazards);
    private readonly Dictionary<uint, NativeActionShape?> shapeCache = new();
    private long nextUpdateTick;

    public NativeCastDangerProvider(IObjectTable objectTable, IDataManager dataManager)
    {
        this.objectTable = objectTable;
        this.dataManager = dataManager;
    }

    public string Name => "Native visible casts";
    public string State { get; private set; } = "Disabled";
    public int HazardCount => hazards.Count;
    public IReadOnlyList<DangerArea> Hazards => hazards;

    public void Update(bool enabled, uint territoryId)
    {
        if (!enabled)
        {
            Disable();
            return;
        }

        var now = Environment.TickCount64;
        if (now < nextUpdateTick)
            return;
        nextUpdateTick = now + UpdateIntervalMilliseconds;
        hazards.Clear();
        var activeCasts = 0;
        var unsupportedCasts = 0;

        foreach (var candidate in objectTable)
        {
            if (hazards.Count >= MaximumHazards)
                break;
            if (candidate is not IBattleNpc actor
                || candidate is not ICharacter character
                || !character.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile)
                || !actor.IsCasting
                || actor.CastActionId == 0)
                continue;

            activeCasts++;
            if (!TryGetShape(actor.CastActionId, out var shape)
                || !TryCreateArea(actor, shape, out var area))
            {
                unsupportedCasts++;
                continue;
            }
            hazards.Add(area);
        }

        State = hazards.Count > 0
            ? $"Active in territory {territoryId}: {hazards.Count} conservative visible-cast hazard(s)"
            : activeCasts > 0
                ? $"Observed {activeCasts} hostile cast(s); {unsupportedCasts} lacked a reliable standard visible shape"
                : "Ready; no supported hostile cast is active";
    }

    public void Disable()
    {
        hazards.Clear();
        State = "Disabled";
        nextUpdateTick = 0;
    }

    public void Dispose() => Disable();

    private bool TryGetShape(uint actionId, out NativeActionShape shape)
    {
        shape = default;
        if (shapeCache.TryGetValue(actionId, out var cached))
        {
            shape = cached.GetValueOrDefault();
            return cached.HasValue;
        }

        if (!dataManager.GetExcelSheet<LuminaAction>().TryGetRow(actionId, out var action)
            || action.Omen.ValueNullable is null
            || !TryDescribe(action, out shape))
        {
            shapeCache[actionId] = null;
            return false;
        }

        shapeCache[actionId] = shape;
        return true;
    }

    private static bool TryDescribe(LuminaAction action, out NativeActionShape shape)
    {
        var label = action.Name.ToString();
        if (label.Length == 0)
            label = $"Action {action.RowId}";
        var range = (float)action.EffectRange;
        var halfWidth = (float)action.XAxisModifier * 0.5f;
        shape = action.CastType switch
        {
            2 => new(DangerShapeKind.Circle, range, 0f, 0f, label, false),
            3 => TryConeHalfAngle(action, out var cone3)
                ? new(DangerShapeKind.Cone, range, 0f, cone3, label, true) : default,
            4 => new(DangerShapeKind.Rectangle, range, halfWidth, 0f, label, true),
            5 => new(DangerShapeKind.Circle, range, 0f, 0f, label, true),
            8 => new(DangerShapeKind.Line, range, halfWidth, 0f, label, true),
            10 => TryDonutInnerRadius(action, out var inner)
                ? new(DangerShapeKind.Donut, range, inner, 0f, label, true) : default,
            11 => new(DangerShapeKind.Cross, range, halfWidth, 0f, label, true),
            12 => new(DangerShapeKind.Rectangle, range, halfWidth, 0f, label, true),
            13 => TryConeHalfAngle(action, out var cone13)
                ? new(DangerShapeKind.Cone, range, 0f, cone13, label, true) : default,
            _ => default,
        };
        return !string.IsNullOrEmpty(shape.Label);
    }

    private bool TryCreateArea(IBattleNpc actor, NativeActionShape shape, out DangerArea area)
    {
        area = default;
        var origin = actor.Position;
        var target = actor.CastTargetObjectId is 0 or ulong.MaxValue
            ? null
            : objectTable.SearchById(actor.CastTargetObjectId);
        var targetPosition = target is { } && target.IsValid() ? target.Position : origin;
        var direction = DangerArea.DirectionTo(origin, targetPosition, actor.Rotation);
        const string source = "Native";

        switch (shape.Kind)
        {
            case DangerShapeKind.Circle:
            {
                var center = shape.CenterOnCaster ? origin : targetPosition;
                var radius = shape.Range + (shape.CenterOnCaster ? actor.HitboxRadius : 0f);
                if (radius <= 0f)
                    return false;
                area = DangerArea.Circle(center, radius, source, shape.Label);
                return true;
            }
            case DangerShapeKind.Donut:
                if (shape.Range <= 0f || shape.InnerRadius <= 0f)
                    return false;
                area = DangerArea.Donut(origin, shape.InnerRadius, shape.Range, source, shape.Label);
                return true;
            case DangerShapeKind.Cone:
                if (shape.Range <= 0f || shape.HalfAngle <= 0f)
                    return false;
                area = DangerArea.Cone(origin, shape.Range + actor.HitboxRadius, direction,
                    shape.HalfAngle, source, shape.Label);
                return true;
            case DangerShapeKind.Rectangle:
                if (shape.Range <= 0f || shape.HalfWidth <= 0f)
                    return false;
                area = DangerArea.Rectangle(origin, shape.Range + actor.HitboxRadius,
                    shape.HalfWidth, direction, source, shape.Label);
                return true;
            case DangerShapeKind.Cross:
                if (shape.Range <= 0f || shape.HalfWidth <= 0f)
                    return false;
                area = DangerArea.Cross(origin, shape.Range, shape.HalfWidth, direction,
                    source, shape.Label);
                return true;
            case DangerShapeKind.Line:
                if (target is null || shape.HalfWidth <= 0f)
                    return false;
                area = DangerArea.Line(origin, targetPosition, shape.HalfWidth, source, shape.Label);
                return true;
            default:
                return false;
        }
    }

    private static bool TryConeHalfAngle(LuminaAction action, out float halfAngle)
    {
        halfAngle = 0f;
        var omen = action.Omen.ValueNullable;
        if (omen is null)
            return false;
        var path = omen.Value.Path.ToString();
        var marker = path.IndexOf("fan", StringComparison.OrdinalIgnoreCase);
        if (marker < 0 || marker + 6 > path.Length
            || !int.TryParse(path.AsSpan(marker + 3, 3), out var fullAngle))
            return false;
        halfAngle = Math.Clamp(fullAngle, 1, 360) * (MathF.PI / 360f);
        return true;
    }

    private static bool TryDonutInnerRadius(LuminaAction action, out float innerRadius)
    {
        innerRadius = 0f;
        var omen = action.Omen.ValueNullable;
        if (omen is null)
            return false;
        var path = omen.Value.Path.ToString();
        var marker = path.IndexOf("sircle_", StringComparison.OrdinalIgnoreCase);
        if (marker < 0 || marker + 11 > path.Length)
            return false;
        return float.TryParse(path.AsSpan(marker + 9, 2), out innerRadius) && innerRadius > 0f;
    }

    private readonly record struct NativeActionShape(
        DangerShapeKind Kind,
        float Range,
        float InnerRadius,
        float HalfAngle,
        string Label,
        bool CenterOnCaster)
    {
        public float HalfWidth => InnerRadius;
    }
}

internal sealed class SplatoonDangerProvider : IEncounterDangerProvider
{
    private const int PollIntervalMilliseconds = 150;
    private const int DisabledStatusPollMilliseconds = 2_000;
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly List<DangerArea> hazards = new(128);
    private readonly Dalamud.Plugin.Ipc.ICallGateSubscriber<bool> isLoaded;
    private readonly Dalamud.Plugin.Ipc.ICallGateSubscriber<SplatoonGeometrySnapshot> geometry;
    private long nextPollTick;

    public SplatoonDangerProvider(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        isLoaded = pluginInterface.GetIpcSubscriber<bool>("Splatoon.IsLoaded");
        geometry = pluginInterface.GetIpcSubscriber<SplatoonGeometrySnapshot>(
            "Splatoon.GetActiveDrawGeometryV1");
    }

    public string Name => "Splatoon IPC";
    public string State { get; private set; } = "Disabled";
    public int HazardCount => hazards.Count;
    public IReadOnlyList<DangerArea> Hazards => hazards;
    public bool IsInstalled { get; private set; }
    public bool IsConnected { get; private set; }

    public void Update(bool enabled, bool trustUnclassifiedGeometry, uint territoryId)
    {
        var now = Environment.TickCount64;
        if (!enabled)
        {
            hazards.Clear();
            IsConnected = false;
            if (now >= nextPollTick)
            {
                IsInstalled = DetectInstallation();
                State = IsInstalled ? "Disabled; Splatoon is installed" : "Disabled; Splatoon is not installed";
                nextPollTick = now + DisabledStatusPollMilliseconds;
            }
            return;
        }

        if (now < nextPollTick)
            return;
        nextPollTick = now + PollIntervalMilliseconds;
        hazards.Clear();
        IsInstalled = DetectInstallation();
        if (!IsInstalled)
        {
            IsConnected = false;
            State = "Splatoon is not installed or loaded";
            return;
        }

        try
        {
            if (!isLoaded.InvokeFunc())
            {
                IsConnected = false;
                State = "Splatoon is loaded but its IPC is not ready";
                return;
            }

            var snapshot = geometry.InvokeFunc();
            IsConnected = true;
            if (snapshot.version != 1)
            {
                State = $"Connected; unsupported geometry IPC version {snapshot.version}";
                return;
            }
            if (snapshot.territoryId != territoryId)
            {
                State = $"Connected; waiting for territory {territoryId} geometry";
                return;
            }
            if (Environment.TickCount64 - snapshot.generatedAtTickMs > 1500)
            {
                State = "Connected; latest geometry snapshot is stale";
                return;
            }

            var unclassified = 0;
            foreach (var item in snapshot.items)
            {
                var semanticallyDangerous = item.source.Equals("danger", StringComparison.OrdinalIgnoreCase)
                    || item.Namespace.StartsWith("SentinelHUD.Danger", StringComparison.OrdinalIgnoreCase);
                if (!semanticallyDangerous && !trustUnclassifiedGeometry)
                {
                    unclassified++;
                    continue;
                }
                if (TryConvert(item, out var area))
                    hazards.Add(area);
            }

            State = hazards.Count > 0
                ? $"Connected: {hazards.Count} active hazard shape(s)"
                : unclassified > 0
                    ? $"Connected: {unclassified} visible shape(s) withheld because IPC v1 has no danger/safe classification"
                    : "Connected; no supported active geometry";
        }
        catch (Exception exception)
        {
            IsConnected = false;
            State = $"IPC unavailable: {exception.Message}";
        }
    }

    public void Disable()
    {
        hazards.Clear();
        IsConnected = false;
        State = "Disabled";
        nextPollTick = 0;
    }

    public void Dispose() => Disable();

    private bool DetectInstallation()
        => pluginInterface.InstalledPlugins.Any(plugin => plugin.IsLoaded
            && (plugin.InternalName.Equals("Splatoon", StringComparison.OrdinalIgnoreCase)
                || plugin.Name.Equals("Splatoon", StringComparison.OrdinalIgnoreCase)));

    private static bool TryConvert(SplatoonGeometryItem item, out DangerArea area)
    {
        area = default;
        var label = item.element.Length > 0 ? item.element
            : item.layout.Length > 0 ? item.layout : item.id;
        switch (item.kind.ToLowerInvariant())
        {
            case "circle" when item.radius is > 0f:
                area = DangerArea.Circle(item.center, item.radius.Value, "Splatoon", label);
                return true;
            case "donut" when item.outerRadius is > 0f && item.innerRadius is >= 0f:
                area = DangerArea.Donut(item.center, item.innerRadius.Value,
                    item.outerRadius.Value, "Splatoon", label);
                return true;
            case "cone" when item.outerRadius is > 0f
                                  && item.angleMinRad.HasValue && item.angleMaxRad.HasValue:
            {
                var minimum = item.angleMinRad.Value;
                var span = DangerArea.NormalizeAngle(item.angleMaxRad.Value - minimum);
                if (span < 0f)
                    span += MathF.Tau;
                area = DangerArea.Cone(item.center, item.outerRadius.Value,
                    minimum + (span * 0.5f), span * 0.5f, "Splatoon", label);
                return true;
            }
            case "line" when item.lineRadius is > 0f:
                area = DangerArea.Line(item.start, item.end, item.lineRadius.Value,
                    "Splatoon", label);
                return true;
            default:
                return false;
        }
    }
}
