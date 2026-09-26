using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelCore.UI;
using SentinelHUD.Services;

namespace SentinelHUD.UI;

/// <summary>
/// Adds input only over explicitly registered actor rows. The surrounding locked HUD stays click-through.
/// </summary>
public sealed class ActorInteractionRenderer(
    ActorTargetingService targeting,
    ActorContextMenuService contextMenus)
{
    private const int MaximumInteractions = 24;
    private static readonly string[] WindowNames = Enumerable.Range(0, MaximumInteractions)
        .Select(index => $"Actor link {index}##SentinelHUD-ActorInteraction-{index}")
        .ToArray();
    private readonly ActorTargetingService targeting = targeting;
    private readonly ActorContextMenuService contextMenus = contextMenus;
    private readonly List<ActorInteraction> interactions = new(MaximumInteractions);

    public string LastActionResult => targeting.LastActionResult;
    public string LastContextMenuResult => contextMenus.LastActionResult;

    public void BeginFrame() => interactions.Clear();

    public void RegisterLastItem(ulong gameObjectId, bool allowTargeting = true,
        bool allowContextMenu = true, int priority = 10)
    {
        Register(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), gameObjectId,
            allowTargeting, allowContextMenu, priority);
    }

    public void Register(Vector2 minimum, Vector2 maximum, ulong gameObjectId,
        bool allowTargeting = true, bool allowContextMenu = true, int priority = 0)
    {
        if (gameObjectId == 0 || gameObjectId == ulong.MaxValue || interactions.Count >= MaximumInteractions)
            return;
        if (maximum.X <= minimum.X || maximum.Y <= minimum.Y)
            return;
        if (!allowTargeting && !allowContextMenu)
            return;
        interactions.Add(new ActorInteraction(minimum, maximum, gameObjectId,
            allowTargeting, allowContextMenu, priority));
    }

    public void Draw()
    {
        interactions.Sort(static (left, right) => left.Priority.CompareTo(right.Priority));
        for (var index = 0; index < interactions.Count; index++)
            Draw(interactions[index], index);
    }

    private void Draw(ActorInteraction interaction, int index)
    {
        var size = interaction.Maximum - interaction.Minimum;
        ImGui.SetNextWindowPos(interaction.Minimum, ImGuiCond.Always);
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.One);
        try
        {
            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration
                                           | ImGuiWindowFlags.NoMove
                                           | ImGuiWindowFlags.NoSavedSettings
                                           | ImGuiWindowFlags.NoDocking
                                           | ImGuiWindowFlags.NoNav
                                           | ImGuiWindowFlags.NoFocusOnAppearing
                                           | ImGuiWindowFlags.NoBringToFrontOnFocus
                                           | ImGuiWindowFlags.NoBackground;
            var began = ImGui.Begin(WindowNames[index], flags);
            try
            {
                if (!began)
                    return;
                ImGui.InvisibleButton("##TargetActor", size,
                    ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);
                if (ImGui.IsItemHovered())
                {
                    if (interaction.AllowTargeting)
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    ImGui.GetForegroundDrawList().AddRect(
                        interaction.Minimum,
                        interaction.Maximum,
                        ImGui.ColorConvertFloat4ToU32(SentinelPalette.HeaderGold),
                        2f,
                        ImDrawFlags.None,
                        1f);
                }
                if (interaction.AllowTargeting && ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    targeting.TryTarget(interaction.GameObjectId);
                if (interaction.AllowContextMenu && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    contextMenus.TryOpen(interaction.GameObjectId);
            }
            finally
            {
                ImGui.End();
            }
        }
        finally
        {
            ImGui.PopStyleVar(3);
        }
    }

    private readonly record struct ActorInteraction(
        Vector2 Minimum,
        Vector2 Maximum,
        ulong GameObjectId,
        bool AllowTargeting,
        bool AllowContextMenu,
        int Priority);
}
