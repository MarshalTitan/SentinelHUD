using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelHUD.Core;
using SentinelHUD.Services;

namespace SentinelHUD.UI;

/// <summary>
/// Adds input only over explicitly registered actor rows. The surrounding locked HUD stays click-through.
/// </summary>
public sealed class ActorInteractionRenderer(
    ActorTargetingService targeting,
    ActorContextMenuService contextMenus)
{
    private const int MaximumInteractions = 112;
    private static readonly string[] WindowNames = Enumerable.Range(0, MaximumInteractions)
        .Select(index => $"Actor link {index}##SentinelHUD-ActorInteraction-{index}")
        .ToArray();
    private readonly ActorTargetingService targeting = targeting;
    private readonly ActorContextMenuService contextMenus = contextMenus;
    private readonly List<ActorInteraction> interactions = new(MaximumInteractions);

    public string LastActionResult => targeting.LastActionResult;
    public string LastContextMenuResult => contextMenus.LastActionResult;
    public bool NativeContextMenuVisible => contextMenus.NativeMenuVisible;
    public bool ShouldSuspendActorInput => contextMenus.ShouldSuspendActorInput;
    public string ContextMenuPlacementState => contextMenus.PlacementState;
    public Vector2 ContextMenuPosition => contextMenus.LastMenuPosition;
    public Vector2 ContextMenuSize => contextMenus.LastMenuSize;

    public void BeginFrame() => interactions.Clear();

    public void RefreshContextMenuState() => contextMenus.RefreshVisibility();

    public void RegisterLastItem(ulong gameObjectId, bool allowTargeting = true,
        bool allowContextMenu = true, int priority = 10)
        => RegisterLastItem(ActorReference.Direct(gameObjectId), allowTargeting, allowContextMenu, priority);

    public void RegisterLastItem(ActorReference reference, bool allowTargeting = true,
        bool allowContextMenu = true, int priority = 10, bool expandToContentWidth = false)
    {
        var minimum = ImGui.GetItemRectMin();
        var maximum = ImGui.GetItemRectMax();
        if (expandToContentWidth)
        {
            maximum.X = Math.Max(maximum.X,
                ImGui.GetWindowPos().X + ImGui.GetWindowSize().X - ImGui.GetStyle().WindowPadding.X);
            minimum.Y -= 2f;
            maximum.Y += 2f;
        }
        Register(minimum, maximum, reference, allowTargeting, allowContextMenu, priority);
    }

    public void Register(Vector2 minimum, Vector2 maximum, ulong gameObjectId,
        bool allowTargeting = true, bool allowContextMenu = true, int priority = 0,
        Vector2? placementMinimum = null, Vector2? placementMaximum = null)
        => Register(minimum, maximum, ActorReference.Direct(gameObjectId),
            allowTargeting, allowContextMenu, priority, placementMinimum, placementMaximum);

    public void Register(Vector2 minimum, Vector2 maximum, ActorReference reference,
        bool allowTargeting = true, bool allowContextMenu = true, int priority = 0,
        Vector2? placementMinimum = null, Vector2? placementMaximum = null)
    {
        if (!reference.IsWellFormed || interactions.Count >= MaximumInteractions)
            return;
        if (maximum.X <= minimum.X || maximum.Y <= minimum.Y)
            return;
        if (!allowTargeting && !allowContextMenu)
            return;
        interactions.Add(new ActorInteraction(minimum, maximum,
            placementMinimum ?? minimum, placementMaximum ?? maximum,
            reference, allowTargeting, allowContextMenu, priority));
    }

    public void Draw()
    {
        interactions.Sort(static (left, right) => left.Priority.CompareTo(right.Priority));
        var mousePosition = ImGui.GetMousePos();
        var winningPriority = int.MinValue;
        foreach (var interaction in interactions)
        {
            if (interaction.Contains(mousePosition))
                winningPriority = Math.Max(winningPriority, interaction.Priority);
        }

        for (var index = 0; index < interactions.Count; index++)
        {
            var interaction = interactions[index];
            if (interaction.Contains(mousePosition) && interaction.Priority < winningPriority)
                continue;
            Draw(interaction, index);
        }
    }

    public void RegisterArc(ArcHudRenderer renderer, ulong gameObjectId, bool allowTargeting,
        bool allowContextMenu, Vector2 placementMinimum, Vector2 placementMaximum)
    {
        if (!ActorReference.Direct(gameObjectId).IsWellFormed || (!allowTargeting && !allowContextMenu)) return;
        for (var i = 0; i < renderer.HitBandCount && interactions.Count < MaximumInteractions; i++)
        {
            var band = renderer.GetHitBand(i);
            interactions.Add(new ActorInteraction(band.Minimum, band.Maximum, placementMinimum, placementMaximum,
                ActorReference.Direct(gameObjectId), allowTargeting, allowContextMenu, 0, false));
        }
    }

    private void Draw(ActorInteraction interaction, int index)
    {
        var click = ActorInputRegionRenderer.Draw(WindowNames[index], interaction.Minimum,
            interaction.Maximum, interaction.AllowTargeting, interaction.ShowOutline);
        if (interaction.AllowTargeting && click.Left) targeting.TryTarget(interaction.Reference);
        if (interaction.AllowContextMenu && click.Right)
        {
            var viewport = ImGui.GetMainViewport();
            contextMenus.TryOpen(interaction.Reference, new ScreenRectangle(interaction.PlacementMinimum.X,
                interaction.PlacementMinimum.Y, interaction.PlacementMaximum.X, interaction.PlacementMaximum.Y),
                ScreenRectangle.FromPositionSize(viewport.WorkPos, viewport.WorkSize));
        }
    }

    private readonly record struct ActorInteraction(
        Vector2 Minimum,
        Vector2 Maximum,
        Vector2 PlacementMinimum,
        Vector2 PlacementMaximum,
        ActorReference Reference,
        bool AllowTargeting,
        bool AllowContextMenu,
        int Priority,
        bool ShowOutline = true)
    {
        public bool Contains(Vector2 point)
            => point.X >= Minimum.X && point.X <= Maximum.X
               && point.Y >= Minimum.Y && point.Y <= Maximum.Y;
    }
}

