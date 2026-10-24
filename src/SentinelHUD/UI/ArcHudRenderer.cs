using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelHUD.Core;

namespace SentinelHUD.UI;

/// <summary>Draws cached paths; HP, MP and shields share the rectangular bars' fill policy.</summary>
public sealed class ArcHudRenderer
{
    private readonly ArcBarGeometry geometry = new();

    public void Draw(HudModuleConfiguration module, HudModuleKind kind, float scale, float opacity,
        float hp, float shield, float mp, bool showMp, string text,
        Vector4 hpColour, Vector4 mpColour, Vector4 shieldColour)
    {
        var origin = ImGui.GetCursorScreenPos();
        var size = new Vector2(Math.Max(80f, ImGui.GetContentRegionAvail().X), module.ArcHeight * scale);
        geometry.Update(size.X, size.Y, module.BarHeight * scale,
            ArcBarGeometry.IsRight(module.ArcOrientation, kind));
        ImGui.Dummy(size);
        var after = ImGui.GetCursorScreenPos();
        var draw = ImGui.GetWindowDrawList();
        var background = new Vector4(0.045f, 0.05f, 0.065f, 0.92f);
        var border = new Vector4(0.76f, 0.64f, 0.38f, module.BorderOpacity);
        if (module.BorderEnabled)
        {
            Stroke(draw, origin, geometry.Hp, 0f, 1f, Colour(border, opacity), geometry.Thickness + 2f);
            if (showMp) Stroke(draw, origin, geometry.Mp, 0f, 1f,
                Colour(border, opacity), geometry.Thickness * 0.5f + 2f);
        }
        Stroke(draw, origin, geometry.Hp, 0f, 1f, Colour(background, opacity), geometry.Thickness);
        var fill = ShieldBarPolicy.Calculate(hp, shield);
        Stroke(draw, origin, geometry.Hp, 0f, fill.HealthEnd, Colour(hpColour, opacity), geometry.Thickness);
        if (fill.HasOverlay) Stroke(draw, origin, geometry.Hp, fill.ShieldOverlayStart,
            fill.HealthEnd, Colour(shieldColour, opacity), geometry.Thickness);
        if (fill.HasExtension) Stroke(draw, origin, geometry.Hp, fill.HealthEnd,
            fill.ShieldExtensionEnd, Colour(shieldColour, opacity), geometry.Thickness);
        if (showMp)
        {
            Stroke(draw, origin, geometry.Mp, 0f, 1f, Colour(background, opacity), geometry.Thickness * 0.5f);
            Stroke(draw, origin, geometry.Mp, 0f, Math.Clamp(mp, 0f, 1f),
                Colour(mpColour, opacity), geometry.Thickness * 0.5f);
        }

        var textHeight = ImGui.CalcTextSize(text, false, geometry.TextWidth).Y;
        var position = origin + new Vector2(geometry.TextLeft, Math.Max(0f, (size.Y - textHeight) * 0.5f));
        ImGui.SetCursorScreenPos(position);
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + geometry.TextWidth);
        try { ImGui.TextUnformatted(text); }
        finally { ImGui.PopTextWrapPos(); ImGui.PopStyleVar(); }
        // Text may outgrow the requested arc at extreme font scales. Keep later rows reachable.
        ImGui.SetCursorScreenPos(new Vector2(after.X, Math.Max(after.Y, ImGui.GetCursorScreenPos().Y)));
        ImGui.Dummy(Vector2.Zero);
    }

    private static uint Colour(Vector4 colour, float opacity)
    {
        colour.W *= opacity;
        return ImGui.ColorConvertFloat4ToU32(colour);
    }

    private static void Stroke(ImDrawListPtr draw, Vector2 origin, ReadOnlySpan<Vector2> points,
        float start, float end, uint colour, float thickness)
    {
        start = Math.Clamp(start, 0f, 1f);
        end = Math.Clamp(end, 0f, 1f);
        if (end <= start) return;
        var first = start * ArcBarGeometry.Segments;
        var last = end * ArcBarGeometry.Segments;
        var index = Math.Min((int)first, ArcBarGeometry.Segments - 1);
        draw.PathClear();
        draw.PathLineTo(origin + Vector2.Lerp(points[index], points[index + 1], first - index));
        for (var i = (int)first + 1; i < last; i++) draw.PathLineTo(origin + points[i]);
        index = Math.Min((int)last, ArcBarGeometry.Segments - 1);
        draw.PathLineTo(origin + Vector2.Lerp(points[index], points[index + 1], last - index));
        draw.PathStroke(colour, ImDrawFlags.None, thickness);
    }
}
