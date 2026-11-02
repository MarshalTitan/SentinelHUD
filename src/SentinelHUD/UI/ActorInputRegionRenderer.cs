using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelCore.UI;
using SentinelHUD.Core;

namespace SentinelHUD.UI;

/// <summary>Native rectangular input islands; everything outside them remains input-free.</summary>
public static class ActorInputRegionRenderer
{
    private static readonly NativeUiInputPartition partition = new();
    public static (bool Left, bool Right) Draw(string name, Vector2 minimum, Vector2 maximum,
        bool allowTargeting, bool showOutline, NativeUiOcclusionMask? nativeUi = null)
    {
        var pieces = partition.Split(minimum, maximum, nativeUi);
        var blocked = nativeUi?.Contains(ImGui.GetMousePos()) == true;
        var left = false; var right = false;
        for (var i = 0; i < pieces.Count; i++)
        {
            var r = pieces[i];
            var click = DrawPart(partition.Name(name, i), new(r.Left, r.Top), new(r.Right, r.Bottom),
                allowTargeting, showOutline, blocked);
            left |= click.Left; right |= click.Right;
        }
        return blocked ? default : (left, right);
    }

    private static (bool Left, bool Right) DrawPart(string name, Vector2 minimum, Vector2 maximum,
        bool allowTargeting, bool showOutline, bool blocked)
    {
        var size = maximum - minimum;
        ImGui.SetNextWindowPos(minimum, ImGuiCond.Always);
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.One);
        try
        {
            var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoNav
                | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoBackground;
            if (blocked) flags |= ImGuiWindowFlags.NoInputs;
            var began = ImGui.Begin(name, flags);
            try
            {
                if (!began) return default;
                ImGui.InvisibleButton("##TargetActor", size,
                    ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);
                if (ImGui.IsItemHovered())
                {
                    if (allowTargeting) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    if (showOutline) ImGui.GetForegroundDrawList().AddRect(minimum, maximum,
                        ImGui.ColorConvertFloat4ToU32(SentinelPalette.HeaderGold), 2f, ImDrawFlags.None, 1f);
                }
                return (ImGui.IsItemClicked(ImGuiMouseButton.Left), ImGui.IsItemClicked(ImGuiMouseButton.Right));
            }
            finally { ImGui.End(); }
        }
        finally { ImGui.PopStyleVar(3); }
    }
}
