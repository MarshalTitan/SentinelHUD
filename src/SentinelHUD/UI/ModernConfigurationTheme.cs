using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelHUD.UI;

/// <summary>
/// Experimental Sentinel Modern presentation tokens. This deliberately lives in Sentinel HUD
/// until the visual language has been live-tested; approved primitives can then move into
/// Sentinel Core without changing every consumer at once.
/// </summary>
internal static class ModernConfigurationTheme
{
    public static readonly Vector4 Canvas = new(0.027f, 0.035f, 0.075f, 0.985f);
    public static readonly Vector4 Surface = new(0.047f, 0.063f, 0.118f, 0.94f);
    public static readonly Vector4 SurfaceRaised = new(0.071f, 0.092f, 0.165f, 0.98f);
    public static readonly Vector4 SurfaceHover = new(0.095f, 0.130f, 0.235f, 1f);
    public static readonly Vector4 Border = new(0.180f, 0.239f, 0.388f, 0.92f);
    public static readonly Vector4 BorderBright = new(0.275f, 0.505f, 0.925f, 0.95f);
    public static readonly Vector4 Accent = new(0.275f, 0.565f, 1f, 1f);
    public static readonly Vector4 AccentStrong = new(0.360f, 0.650f, 1f, 1f);
    public static readonly Vector4 Violet = new(0.635f, 0.355f, 0.940f, 1f);
    public static readonly Vector4 Rose = new(0.960f, 0.330f, 0.515f, 1f);
    public static readonly Vector4 Text = new(0.925f, 0.941f, 0.985f, 1f);
    public static readonly Vector4 Muted = new(0.590f, 0.635f, 0.740f, 1f);
    public static readonly Vector4 Subtle = new(0.390f, 0.435f, 0.550f, 1f);

    public static ModernConfigurationThemeScope Push() => new();

    public static void DrawBackdrop()
    {
        var drawList = ImGui.GetWindowDrawList();
        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();

        DrawGlow(drawList,
            position + new Vector2(size.X * 0.18f, size.Y * 0.18f),
            MathF.Max(105f, size.X * 0.20f),
            new Vector3(0.07f, 0.28f, 0.63f), 0.15f);
        DrawGlow(drawList,
            position + new Vector2(size.X * 0.80f, size.Y * 0.27f),
            MathF.Max(95f, size.X * 0.17f),
            new Vector3(0.57f, 0.12f, 0.58f), 0.13f);
        DrawGlow(drawList,
            position + new Vector2(size.X * 0.59f, size.Y * 0.88f),
            MathF.Max(115f, size.X * 0.21f),
            new Vector3(0.12f, 0.48f, 0.49f), 0.10f);
    }

    private static void DrawGlow(ImDrawListPtr drawList, Vector2 centre, float radius,
        Vector3 colour, float opacity)
    {
        drawList.AddCircleFilled(centre, radius,
            ImGui.ColorConvertFloat4ToU32(new Vector4(colour, opacity * 0.38f)), 64);
        drawList.AddCircleFilled(centre, radius * 0.67f,
            ImGui.ColorConvertFloat4ToU32(new Vector4(colour, opacity * 0.62f)), 64);
        drawList.AddCircleFilled(centre, radius * 0.36f,
            ImGui.ColorConvertFloat4ToU32(new Vector4(colour, opacity)), 64);
    }
}

internal sealed class ModernConfigurationThemeScope : IDisposable
{
    private int colourCount;
    private int variableCount;
    private bool disposed;

    public ModernConfigurationThemeScope()
    {
        Push(ImGuiCol.WindowBg, ModernConfigurationTheme.Canvas);
        Push(ImGuiCol.ChildBg, new Vector4(
            ModernConfigurationTheme.Surface.X,
            ModernConfigurationTheme.Surface.Y,
            ModernConfigurationTheme.Surface.Z,
            0.76f));
        Push(ImGuiCol.PopupBg, ModernConfigurationTheme.SurfaceRaised);
        Push(ImGuiCol.Border, ModernConfigurationTheme.Border);
        Push(ImGuiCol.BorderShadow, Vector4.Zero);
        Push(ImGuiCol.Text, ModernConfigurationTheme.Text);
        Push(ImGuiCol.TextDisabled, ModernConfigurationTheme.Muted);
        Push(ImGuiCol.TitleBg, new Vector4(0.035f, 0.045f, 0.090f, 1f));
        Push(ImGuiCol.TitleBgActive, new Vector4(0.045f, 0.057f, 0.110f, 1f));
        Push(ImGuiCol.TitleBgCollapsed, new Vector4(0.035f, 0.045f, 0.090f, 1f));
        Push(ImGuiCol.FrameBg, ModernConfigurationTheme.SurfaceRaised);
        Push(ImGuiCol.FrameBgHovered, ModernConfigurationTheme.SurfaceHover);
        Push(ImGuiCol.FrameBgActive, new Vector4(0.105f, 0.150f, 0.275f, 1f));
        Push(ImGuiCol.CheckMark, ModernConfigurationTheme.AccentStrong);
        Push(ImGuiCol.SliderGrab, ModernConfigurationTheme.Accent);
        Push(ImGuiCol.SliderGrabActive, ModernConfigurationTheme.AccentStrong);
        Push(ImGuiCol.Button, ModernConfigurationTheme.SurfaceRaised);
        Push(ImGuiCol.ButtonHovered, ModernConfigurationTheme.SurfaceHover);
        Push(ImGuiCol.ButtonActive, new Vector4(0.145f, 0.260f, 0.485f, 1f));
        Push(ImGuiCol.Header, ModernConfigurationTheme.SurfaceRaised);
        Push(ImGuiCol.HeaderHovered, ModernConfigurationTheme.SurfaceHover);
        Push(ImGuiCol.HeaderActive, new Vector4(0.120f, 0.205f, 0.375f, 1f));
        Push(ImGuiCol.Separator, ModernConfigurationTheme.Border);
        Push(ImGuiCol.SeparatorHovered, ModernConfigurationTheme.BorderBright);
        Push(ImGuiCol.SeparatorActive, ModernConfigurationTheme.Accent);
        Push(ImGuiCol.ScrollbarBg, new Vector4(0.020f, 0.026f, 0.055f, 0.55f));
        Push(ImGuiCol.ScrollbarGrab, new Vector4(0.175f, 0.225f, 0.365f, 0.95f));
        Push(ImGuiCol.ScrollbarGrabHovered, ModernConfigurationTheme.BorderBright);
        Push(ImGuiCol.ScrollbarGrabActive, ModernConfigurationTheme.Accent);
        Push(ImGuiCol.ResizeGrip, new Vector4(0.275f, 0.565f, 1f, 0.25f));
        Push(ImGuiCol.ResizeGripHovered, new Vector4(0.275f, 0.565f, 1f, 0.65f));
        Push(ImGuiCol.ResizeGripActive, ModernConfigurationTheme.Accent);

        Push(ImGuiStyleVar.WindowRounding, 10f);
        Push(ImGuiStyleVar.ChildRounding, 9f);
        Push(ImGuiStyleVar.PopupRounding, 8f);
        Push(ImGuiStyleVar.FrameRounding, 7f);
        Push(ImGuiStyleVar.GrabRounding, 8f);
        Push(ImGuiStyleVar.ScrollbarRounding, 8f);
        Push(ImGuiStyleVar.WindowBorderSize, 1f);
        Push(ImGuiStyleVar.ChildBorderSize, 1f);
        Push(ImGuiStyleVar.FrameBorderSize, 1f);
        Push(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        Push(ImGuiStyleVar.FramePadding, new Vector2(10f, 6f));
        Push(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 7f));
        Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(7f, 5f));
        Push(ImGuiStyleVar.ScrollbarSize, 11f);
        Push(ImGuiStyleVar.GrabMinSize, 12f);
    }

    private void Push(ImGuiCol colour, Vector4 value)
    {
        ImGui.PushStyleColor(colour, value);
        colourCount++;
    }

    private void Push(ImGuiStyleVar variable, float value)
    {
        ImGui.PushStyleVar(variable, value);
        variableCount++;
    }

    private void Push(ImGuiStyleVar variable, Vector2 value)
    {
        ImGui.PushStyleVar(variable, value);
        variableCount++;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        if (variableCount > 0)
            ImGui.PopStyleVar(variableCount);
        if (colourCount > 0)
            ImGui.PopStyleColor(colourCount);
    }
}
