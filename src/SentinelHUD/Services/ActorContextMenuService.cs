using System.Numerics;
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
    private PlacementRequest? placementRequest;
    private bool exactPositionApplied;

    public string LastActionResult { get; private set; } = "No actor context menu requested";
    public bool NativeMenuVisible { get; private set; }
    public bool ShouldSuspendActorInput { get; private set; }
    public string PlacementState { get; private set; } = "No Sentinel context menu positioned";
    public Vector2 LastMenuPosition { get; private set; }
    public Vector2 LastMenuSize { get; private set; }

    public void RefreshVisibility()
    {
        AtkUnitBase* addon = null;
        try
        {
            addon = gameGui.GetAddonByName<AtkUnitBase>(ContextMenuAddonName);
            NativeMenuVisible = addon is not null && addon->IsVisible;
        }
        catch
        {
            NativeMenuVisible = false;
        }

        ShouldSuspendActorInput = suppression.Update(NativeMenuVisible, Environment.TickCount64);
        if (NativeMenuVisible && addon is not null && placementRequest is { } request && !exactPositionApplied)
        {
            var size = ReadMenuSize(addon);
            var placement = ContextMenuPlacementPolicy.Place(request.Origin, size, request.Viewport);
            addon->SetPosition(ToNativeCoordinate(placement.Position.X), ToNativeCoordinate(placement.Position.Y));
            exactPositionApplied = true;
            LastMenuPosition = placement.Position;
            LastMenuSize = size;
            PlacementState = $"Native menu placed {placement.Side} of its Sentinel interaction region";
        }

        if (!NativeMenuVisible && !ShouldSuspendActorInput)
        {
            placementRequest = null;
            exactPositionApplied = false;
        }
    }

    public bool TryOpen(ActorReference reference, ScreenRectangle origin, ScreenRectangle viewport)
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

        placementRequest = new PlacementRequest(origin, viewport);
        exactPositionApplied = false;
        var initialPlacement = ContextMenuPlacementPolicy.Place(
            origin, ContextMenuPlacementPolicy.EstimatedMenuSize, viewport);
        var contextAgent = AgentContext.Instance();
        if (contextAgent is not null)
        {
            contextAgent->SetPosition(
                (int)MathF.Round(initialPlacement.Position.X),
                (int)MathF.Round(initialPlacement.Position.Y));
        }

        agent->OpenContextMenuFromTarget((GameObject*)actor.Address);
        suppression.NotifyOpenRequested(Environment.TickCount64);
        ShouldSuspendActorInput = true;
        LastMenuPosition = initialPlacement.Position;
        LastMenuSize = ContextMenuPlacementPolicy.EstimatedMenuSize;
        PlacementState = $"Requested native menu {initialPlacement.Side} of its Sentinel interaction region";
        LastActionResult = $"Opened native context menu for {actor.Name.TextValue}";
        return true;
    }

    private static Vector2 ReadMenuSize(AtkUnitBase* addon)
    {
        ushort width = 0;
        ushort height = 0;
        addon->GetSize(&width, &height, true);
        if (width > 0 && height > 0)
            return new Vector2(width, height);
        return ContextMenuPlacementPolicy.EstimatedMenuSize;
    }

    private static short ToNativeCoordinate(float value)
        => (short)Math.Clamp((int)MathF.Round(value), short.MinValue, short.MaxValue);

    private readonly record struct PlacementRequest(ScreenRectangle Origin, ScreenRectangle Viewport);
}
