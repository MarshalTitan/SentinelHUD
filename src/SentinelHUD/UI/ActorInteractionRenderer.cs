using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelCore.UI;
using SentinelHUD.Services;

namespace SentinelHUD.UI;

/// <summary>
/// Adds input only over explicitly registered actor rows. The surrounding locked HUD stays click-through.
/// </summary>
public sealed class ActorInteractionRenderer(ActorTargetingService targeting)
{
    private const int MaximumInteractions = 16;
    private static readonly string[] WindowNames = Enumerable.Range(0, MaximumInteractions)
        .Select(index => $"Actor link {index}##SentinelHUD-ActorInteraction-{index}")
        .ToArray();
    private readonly ActorTargetingService targeting = targeting;
    private readonly List<ActorInteraction> interactions = new(MaximumInteractions);

    public string LastActionResult => targeting.LastActionResult;

    public void BeginFrame() => interactions.Clear();

    public void RegisterLastItem(ulong gameObjectId)
    {
        if (gameObjectId == 0 || gameObjectId == ulong.MaxValue || interactions.Count >= MaximumInteractions)
            return;
        var minimum = ImGui.GetItemRectMin();
        var maximum = ImGui.GetItemRectMax();
        if (maximum.X <= minimum.X || maximum.Y <= minimum.Y)
            return;
        interactions.Add(new ActorInteraction(minimum, maximum, gameObjectId));
    }

    public void Draw()
    {
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
                ImGui.InvisibleButton("##TargetActor", size);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    ImGui.GetForegroundDrawList().AddRect(
                        interaction.Minimum,
                        interaction.Maximum,
                        ImGui.ColorConvertFloat4ToU32(SentinelPalette.HeaderGold),
                        2f,
                        ImDrawFlags.None,
                        1f);
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    targeting.TryTarget(interaction.GameObjectId);
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
        ulong GameObjectId);
}
