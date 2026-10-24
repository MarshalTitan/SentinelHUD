using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelHUD.Core;
using SentinelCore.UI;

namespace SentinelHUD.UI;

public readonly record struct ArcTextBounds(Vector2 Minimum, Vector2 Maximum)
{
    public bool Visible => Maximum.X > Minimum.X && Maximum.Y > Minimum.Y;
}

/// <summary>Flat strip slices share cached bows; readouts follow their resource colours.</summary>
public sealed class ArcHudRenderer
{
    private readonly ArcBarGeometry geometry = new();
    public ArcTextBounds HeaderBounds { get; private set; }
    public ArcTextBounds HpTextBounds { get; private set; }
    public ArcTextBounds MpTextBounds { get; private set; }

    public void Draw(HudModuleConfiguration module, HudModuleKind kind, float scale, float opacity,
        float hp, float shield, float mp, bool showMp, ArcReadout text,
        Vector4 hpColour, Vector4 mpColour, Vector4 shieldColour)
    {
        var origin = ImGui.GetCursorScreenPos();
        var width = Math.Max(80f, ImGui.GetContentRegionAvail().X);
        var right = ArcBarGeometry.IsRight(module.ArcOrientation, kind);
        var arcHeight = module.ArcHeight * scale;
        geometry.Update(width, arcHeight, module.BarHeight * scale, right);
        var spacing = ImGui.GetStyle().ItemSpacing.Y;
        var groupGap = 5f * scale;
        var height = Measure(text.Name) + Measure(text.Metadata) + Measure(text.Hp)
            + Measure(text.Mp) + Measure(text.Shield) + Measure(text.Distance);
        if (text.Hp.Length > 0 && (text.Name.Length > 0 || text.Metadata.Length > 0)) height += groupGap;
        if (text.Mp.Length > 0) height += groupGap;
        var size = new Vector2(width, Math.Max(arcHeight, height));
        ImGui.Dummy(size);
        var after = ImGui.GetCursorScreenPos();
        var arcOrigin = origin + new Vector2(0f, (size.Y - arcHeight) * 0.5f);
        var draw = ImGui.GetWindowDrawList();
        var background = new Vector4(0.045f, 0.05f, 0.065f, 0.92f);
        var border = new Vector4(0.76f, 0.64f, 0.38f, module.BorderOpacity);
        var savedFlags = draw.Flags;
        // Neighbouring slices share exact edges. Per-quad AA would leave interior seams.
        draw.Flags &= ~ImDrawListFlags.AntiAliasedFill;
        try
        {
            if (module.BorderEnabled)
            {
                Fill(draw, arcOrigin, geometry.Hp, 0f, 1f, Colour(border, opacity), geometry.Thickness + 2f);
                if (showMp) Fill(draw, arcOrigin, geometry.Mp, 0f, 1f,
                    Colour(border, opacity), geometry.Thickness * 0.5f + 2f);
            }
            Fill(draw, arcOrigin, geometry.Hp, 0f, 1f, Colour(background, opacity), geometry.Thickness);
            var fill = ShieldBarPolicy.Calculate(hp, shield);
            Fill(draw, arcOrigin, geometry.Hp, 0f, fill.HealthEnd, Colour(hpColour, opacity), geometry.Thickness);
            if (fill.HasOverlay) Fill(draw, arcOrigin, geometry.Hp, fill.ShieldOverlayStart,
                fill.HealthEnd, Colour(shieldColour, opacity), geometry.Thickness);
            if (fill.HasExtension) Fill(draw, arcOrigin, geometry.Hp, fill.HealthEnd,
                fill.ShieldExtensionEnd, Colour(shieldColour, opacity), geometry.Thickness);
            if (showMp)
            {
                Fill(draw, arcOrigin, geometry.Mp, 0f, 1f, Colour(background, opacity), geometry.Thickness * 0.5f);
                Fill(draw, arcOrigin, geometry.Mp, 0f, Math.Clamp(mp, 0f, 1f),
                    Colour(mpColour, opacity), geometry.Thickness * 0.5f);
            }
        }
        finally { draw.Flags = savedFlags; }

        HeaderBounds = default;
        var y = origin.Y + (size.Y - height) * 0.5f;
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
        try
        {
            var name = Line(text.Name, SentinelPalette.HeaderGold);
            var metadata = Line(text.Metadata, new Vector4(0.72f, 0.74f, 0.79f, 1f));
            if (name.Visible || metadata.Visible)
                HeaderBounds = new ArcTextBounds(
                    name.Visible && metadata.Visible ? Vector2.Min(name.Minimum, metadata.Minimum)
                        : name.Visible ? name.Minimum : metadata.Minimum,
                    new Vector2(origin.X + geometry.TextLeft + geometry.TextWidth,
                        metadata.Visible ? metadata.Maximum.Y : name.Maximum.Y));
            if (text.Hp.Length > 0 && HeaderBounds.Visible) y += groupGap;
            HpTextBounds = Line(text.Hp, hpColour);
            if (text.Mp.Length > 0) y += groupGap;
            MpTextBounds = Line(text.Mp, mpColour);
            Line(text.Shield, shieldColour);
            Line(text.Distance, new Vector4(0.72f, 0.74f, 0.79f, 1f));
        }
        finally { ImGui.PopStyleVar(); }
        ImGui.SetCursorScreenPos(new Vector2(after.X, Math.Max(after.Y, y)));
        ImGui.Dummy(Vector2.Zero);

        float Measure(string value) => value.Length == 0 ? 0f
            : ImGui.CalcTextSize(value, false, geometry.TextWidth).Y + spacing;

        ArcTextBounds Line(string value, Vector4 colour)
        {
            if (value.Length == 0) return default;
            var textWidth = Math.Min(geometry.TextWidth, ImGui.CalcTextSize(value, false, geometry.TextWidth).X);
            var x = origin.X + geometry.TextLeft + (right ? geometry.TextWidth - textWidth : 0f);
            ImGui.SetCursorScreenPos(new Vector2(x, y));
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + geometry.TextWidth);
            try { ImGui.TextColored(colour, value); }
            finally { ImGui.PopTextWrapPos(); }
            var bounds = new ArcTextBounds(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            y = ImGui.GetCursorScreenPos().Y;
            return bounds;
        }
    }

    private static uint Colour(Vector4 colour, float opacity)
    {
        colour.W *= opacity;
        return ImGui.ColorConvertFloat4ToU32(colour);
    }

    private static void Fill(ImDrawListPtr draw, Vector2 origin, ReadOnlySpan<Vector2> points,
        float start, float end, uint colour, float thickness)
    {
        start = Math.Clamp(start, 0f, 1f);
        end = Math.Clamp(end, 0f, 1f);
        if (end <= start) return;
        var first = start * ArcBarGeometry.Segments;
        var last = end * ArcBarGeometry.Segments;
        for (var i = (int)first; i < last; i++)
        {
            var bottom = Vector2.Lerp(points[i], points[i + 1], Math.Max(first, i) - i);
            var top = Vector2.Lerp(points[i], points[i + 1], Math.Min(last, i + 1) - i);
            var quad = ArcStripGeometry.Section(origin + bottom, origin + top, thickness);
            draw.AddQuadFilled(quad.A, quad.B, quad.C, quad.D, colour);
        }
    }
}
