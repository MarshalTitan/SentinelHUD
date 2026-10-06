using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SentinelCore.UI;
using SentinelHUD.Core;

namespace SentinelHUD.UI;

/// <summary>Consumer-owned top-level sizing around the unchanged canonical Core shell.</summary>
internal static class ModernWindowPresentation
{
    private static readonly Action DrawNothing = static () => { };

    public static void Prepare(Window window, SentinelHudModernWindowState state, bool modern,
        ImGuiWindowFlags classicFlags, Vector2 classicMinimum)
    {
        window.Collapsed = state.ConsumeNativeExpansionRequest();
        window.CollapsedCondition = ImGuiCond.Always;
        window.Flags = modern
            ? SentinelModernWindowChrome.UseCustomHeader(classicFlags)
                | (state.IsMinimized ? ImGuiWindowFlags.NoResize : ImGuiWindowFlags.None)
            : classicFlags;
        var header = SentinelModernAppLayoutOptions.Default.HeaderHeight;
        // Dalamud's WindowHost scales these logical constraints once when it applies them.
        var minimum = modern
            ? SentinelHudModernWindowPolicy.MinimumSize(1f,
                SentinelModernAppLayout.MinimumWindowSize(1f, true, true))
            : classicMinimum;
        window.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = modern && state.IsMinimized ? new Vector2(minimum.X, header) : minimum,
            MaximumSize = new Vector2(float.MaxValue, modern && state.IsMinimized ? header : float.MaxValue),
        };
        if (state.ConsumeSizeRequest() is { } requestedSize)
        {
            window.Size = requestedSize;
            window.SizeCondition = ImGuiCond.Always;
        }
        else if (window.SizeCondition == ImGuiCond.Always)
        {
            window.Size = null;
            window.SizeCondition = ImGuiCond.FirstUseEver;
        }
    }

    public static void DrawShell(SentinelModernAppShellOptions options, SentinelHudModernWindowState state,
        SentinelModernAppShellState shell, IReadOnlyList<SentinelModernNavItem> navigation,
        Action<string> selectPage, Action drawPage, Action? drawSecondary, Action? drawDock)
    {
        state.ObserveFrame(ImGui.GetWindowSize() / options.Scale);
        SentinelModernAppShell.Draw(options with { CollapseTooltip = state.IsMinimized ? "Expand" : "Minimize" },
            shell, navigation, selectPage, state.IsMinimized ? DrawNothing : drawPage,
            state.IsMinimized ? null : drawSecondary, state.IsMinimized ? null : drawDock);
    }
}
