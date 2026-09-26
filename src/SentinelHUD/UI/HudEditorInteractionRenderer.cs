using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelCore.Configuration;
using SentinelCore.UI;
using SentinelHUD.Core;

namespace SentinelHUD.UI;

/// <summary>
/// Owns all edit-mode pointer input. HUD windows use the same titleless, input-free geometry in both
/// modes, so editor chrome can never change their saved origin or reintroduce lock/unlock drift.
/// </summary>
public sealed class HudEditorInteractionRenderer(ConfigurationCoordinator<Configuration> configuration)
{
    private const int MaximumRegions = 8;
    private const float ResizeHandleSize = 12f;
    private static readonly string[] WindowNames = Enumerable.Range(0, MaximumRegions)
        .Select(index => $"HUD editor {index}##SentinelHUD-Editor-{index}")
        .ToArray();
    private static readonly string[] Labels = ["Player", "Target", "Focus", "Target of Target"];
    private readonly ConfigurationCoordinator<Configuration> configuration = configuration;
    private readonly List<EditRegion> regions = new(MaximumRegions);
    private HudModuleKind? selectedKind;
    private HudModuleKind? activeKind;
    private Vector2 dragStartMouse;
    private Vector2 dragStartPosition;
    private Vector2 dragStartSize;
    private float dragStartWidth;
    private float dragStartBarHeight;
    private int dragStartBarCount;
    private EditorResizeEdges resizeEdges;

    public void BeginFrame() => regions.Clear();

    public void Register(HudModuleKind kind, Vector2 minimum, Vector2 maximum, float configuredWidth,
        float configuredBarHeight, int renderedBarCount)
    {
        if (regions.Count >= MaximumRegions || maximum.X <= minimum.X || maximum.Y <= minimum.Y)
            return;
        regions.Add(new EditRegion(kind, minimum, maximum, configuredWidth, configuredBarHeight,
            renderedBarCount));
    }

    public void Draw()
    {
        for (var index = 0; index < regions.Count; index++)
            Draw(regions[index], index);
    }

    private void Draw(EditRegion region, int index)
    {
        var size = region.Maximum - region.Minimum;
        ImGui.SetNextWindowPos(region.Minimum, ImGuiCond.Always);
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

                ImGui.InvisibleButton("##EditModule", size);
                var hovered = ImGui.IsItemHovered();
                var mouse = ImGui.GetMousePos();
                var hoveredEdges = GetResizeEdges(region, mouse);
                if (hovered)
                    ImGui.SetMouseCursor(GetCursor(hoveredEdges));

                if (ImGui.IsItemActivated())
                {
                    selectedKind = region.Kind;
                    activeKind = region.Kind;
                    resizeEdges = hoveredEdges;
                    dragStartMouse = mouse;
                    dragStartPosition = region.Minimum;
                    dragStartSize = size;
                    dragStartWidth = region.ConfiguredWidth;
                    dragStartBarHeight = region.ConfiguredBarHeight;
                    dragStartBarCount = region.RenderedBarCount;
                }

                if (activeKind == region.Kind && ImGui.IsItemActive())
                    ApplyDrag(region, mouse - dragStartMouse);
                else if (activeKind == region.Kind && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    activeKind = null;

                DrawAffordances(region, hovered || activeKind == region.Kind);
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

    private void ApplyDrag(EditRegion region, Vector2 delta)
    {
        if (delta.LengthSquared() < 0.01f)
            return;

        var viewport = ImGui.GetMainViewport();
        if (resizeEdges != EditorResizeEdges.None)
        {
            var result = LayoutPolicy.ResizeFromEdges(dragStartWidth, dragStartBarHeight,
                dragStartBarCount, resizeEdges, delta, dragStartPosition, dragStartSize,
                viewport.WorkPos, viewport.WorkSize);
            configuration.Update(config =>
            {
                var module = GetModule(config, region.Kind);
                module.Width = result.Width;
                module.BarHeight = result.BarHeight;
                module.Layout.AnchorX = result.Layout.AnchorX;
                module.Layout.AnchorY = result.Layout.AnchorY;
            }, TimeSpan.FromMilliseconds(650));
            return;
        }

        var desired = dragStartPosition + delta;
        var reachable = LayoutPolicy.KeepReachable(desired,
            viewport.WorkPos, viewport.WorkSize, dragStartSize);
        var normalized = LayoutPolicy.ToNormalizedPosition(reachable,
            viewport.WorkPos, viewport.WorkSize, dragStartSize);
        configuration.Update(config =>
        {
            var layout = GetModule(config, region.Kind).Layout;
            layout.AnchorX = normalized.AnchorX;
            layout.AnchorY = normalized.AnchorY;
        }, TimeSpan.FromMilliseconds(650));
    }

    private void DrawAffordances(EditRegion region, bool activeOrHovered)
    {
        var selected = selectedKind == region.Kind;
        var colour = selected || activeOrHovered
            ? ImGui.ColorConvertFloat4ToU32(SentinelPalette.HeaderGold)
            : 0x88909090;
        var drawList = ImGui.GetForegroundDrawList();
        drawList.AddRect(region.Minimum, region.Maximum, colour, 3f,
            ImDrawFlags.None, selected ? 2f : 1f);
        drawList.AddRectFilled(
            new Vector2(region.Maximum.X - 5f, region.Minimum.Y + 5f),
            new Vector2(region.Maximum.X - 2f, region.Maximum.Y - 5f),
            colour,
            1f);
        drawList.AddRectFilled(
            new Vector2(region.Minimum.X + 5f, region.Maximum.Y - 5f),
            new Vector2(region.Maximum.X - 5f, region.Maximum.Y - 2f),
            colour,
            1f);
        drawList.AddTriangleFilled(region.Maximum - new Vector2(11f, 2f),
            region.Maximum - new Vector2(2f, 11f), region.Maximum - new Vector2(2f, 2f), colour);
        drawList.AddText(region.Minimum + new Vector2(5f, 3f), colour, Labels[(int)region.Kind]);
    }

    private static EditorResizeEdges GetResizeEdges(EditRegion region, Vector2 mouse)
    {
        var edges = EditorResizeEdges.None;
        if (mouse.X <= region.Minimum.X + ResizeHandleSize)
            edges |= EditorResizeEdges.Left;
        else if (mouse.X >= region.Maximum.X - ResizeHandleSize)
            edges |= EditorResizeEdges.Right;
        if (mouse.Y <= region.Minimum.Y + ResizeHandleSize)
            edges |= EditorResizeEdges.Top;
        else if (mouse.Y >= region.Maximum.Y - ResizeHandleSize)
            edges |= EditorResizeEdges.Bottom;
        return edges;
    }

    private static ImGuiMouseCursor GetCursor(EditorResizeEdges edges)
    {
        var horizontal = edges.HasFlag(EditorResizeEdges.Left)
                         || edges.HasFlag(EditorResizeEdges.Right);
        var vertical = edges.HasFlag(EditorResizeEdges.Top)
                       || edges.HasFlag(EditorResizeEdges.Bottom);
        if (horizontal && vertical)
        {
            var northWestSouthEast = edges.HasFlag(EditorResizeEdges.Left)
                                     && edges.HasFlag(EditorResizeEdges.Top)
                                     || edges.HasFlag(EditorResizeEdges.Right)
                                     && edges.HasFlag(EditorResizeEdges.Bottom);
            return northWestSouthEast ? ImGuiMouseCursor.ResizeNwse : ImGuiMouseCursor.ResizeNesw;
        }
        if (horizontal)
            return ImGuiMouseCursor.ResizeEw;
        if (vertical)
            return ImGuiMouseCursor.ResizeNs;
        return ImGuiMouseCursor.Hand;
    }

    private static HudModuleConfiguration GetModule(Configuration config, HudModuleKind kind)
        => kind switch
        {
            HudModuleKind.Player => config.Player,
            HudModuleKind.Target => config.Target,
            HudModuleKind.FocusTarget => config.FocusTarget,
            _ => config.TargetOfTarget,
        };

    private readonly record struct EditRegion(
        HudModuleKind Kind,
        Vector2 Minimum,
        Vector2 Maximum,
        float ConfiguredWidth,
        float ConfiguredBarHeight,
        int RenderedBarCount);
}
