using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelHUD.Core;
using SentinelCore.UI;

namespace SentinelHUD.UI;

public readonly record struct ArcTextBounds(Vector2 Minimum, Vector2 Maximum)
{
    public bool Visible => Maximum.X > Minimum.X && Maximum.Y > Minimum.Y;
}

/// <summary>Only the narrow visible tracks accept gameplay input; readouts remain click-through.</summary>
public sealed class ArcHudRenderer
{
    private readonly ArcBarGeometry geometry = new();
    private bool mpVisible;
    public ArcTextBounds HeaderBounds { get; private set; }
    public ArcTextBounds HpTextBounds { get; private set; }
    public ArcTextBounds HpPercentageBounds { get; private set; }
    public ArcTextBounds MpTextBounds { get; private set; }
    public Vector2 ArcOrigin { get; private set; }
    public int HitBandCount => ArcHitRegions.BandsPerTrack * (mpVisible ? 2 : 1);

    public ArcHitBand GetHitBand(int index)
    {
        var mp = index >= ArcHitRegions.BandsPerTrack;
        return ArcHitRegions.Band(mp ? geometry.Mp : geometry.Hp,
            index % ArcHitRegions.BandsPerTrack,
            (mp ? geometry.Thickness * 0.5f : geometry.Thickness) + 2f, ArcOrigin);
    }

    public void Draw(HudModuleConfiguration module, HudModuleKind kind, float scale, float opacity,
        float hp, float shield, float mp, bool showMp, ArcReadout text,
        Vector4 hpColour, Vector4 mpColour, Vector4 shieldColour)
    {
        var origin = ImGui.GetCursorScreenPos();
        var width = Math.Max(80f, ImGui.GetContentRegionAvail().X);
        var right = ArcBarGeometry.IsRight(module.ArcOrientation, kind);
        var arcHeight = module.ArcHeight * scale;
        geometry.Update(width, arcHeight, module.BarHeight * scale, right);
        mpVisible = showMp;
        var half = width * 0.5f;
        var gap = Math.Clamp(6f * scale, 3f, width * 0.05f);
        var columnWidth = half - gap;
        var outsideX = right ? half + gap : 0f;
        var insideX = right ? 0f : half + gap;
        var spacing = ImGui.GetStyle().ItemSpacing.Y;
        var innerBarEdge = showMp ? geometry.Thickness + 5f + geometry.Thickness * 0.25f
            : geometry.Thickness * 0.5f;
        var headerWidth = Math.Max(12f, half - innerBarEdge - gap - 1f);
        var headerX = right ? 0f : width - headerWidth;
        var headerHeight = Measure(text.Name, headerWidth) + Measure(text.Metadata, headerWidth);
        var bodyHeight = Math.Max(arcHeight, headerHeight);
        var outsideHeight = Measure(text.Hp, columnWidth) + Measure(text.Mp, columnWidth)
            + Measure(text.Shield, columnWidth);
        var insideHeight = Measure(text.HpPercentage, columnWidth) + Measure(text.Distance, columnWidth);
        var footerHeight = Math.Max(outsideHeight, insideHeight);
        var footerY = bodyHeight + (footerHeight > 0f ? gap : 0f);
        var size = new Vector2(width, footerY + footerHeight);
        ImGui.Dummy(size);
        var after = ImGui.GetCursorScreenPos();
        // The lower HP endpoint is centred between the outer values and inner percentage.
        ArcOrigin = origin + new Vector2(half - geometry.Hp[0].X, bodyHeight - arcHeight);
        var draw = ImGui.GetWindowDrawList();
        var savedFlags = draw.Flags;
        var background = new Vector4(0.045f, 0.05f, 0.065f, 0.92f);
        var border = new Vector4(0.76f, 0.64f, 0.38f, module.BorderOpacity);
        draw.Flags &= ~ImDrawListFlags.AntiAliasedFill;
        try
        {
            if (module.BorderEnabled)
            {
                Fill(draw, ArcOrigin, geometry.Hp, 0f, 1f, Colour(border, opacity), geometry.Thickness + 2f);
                if (showMp) Fill(draw, ArcOrigin, geometry.Mp, 0f, 1f,
                    Colour(border, opacity), geometry.Thickness * 0.5f + 2f);
            }
            Fill(draw, ArcOrigin, geometry.Hp, 0f, 1f, Colour(background, opacity), geometry.Thickness);
            var fill = ShieldBarPolicy.Calculate(hp, shield);
            Fill(draw, ArcOrigin, geometry.Hp, 0f, fill.HealthEnd, Colour(hpColour, opacity), geometry.Thickness);
            if (fill.HasOverlay) Fill(draw, ArcOrigin, geometry.Hp, fill.ShieldOverlayStart,
                fill.HealthEnd, Colour(shieldColour, opacity), geometry.Thickness);
            if (fill.HasExtension) Fill(draw, ArcOrigin, geometry.Hp, fill.HealthEnd,
                fill.ShieldExtensionEnd, Colour(shieldColour, opacity), geometry.Thickness);
            if (showMp)
            {
                Fill(draw, ArcOrigin, geometry.Mp, 0f, 1f, Colour(background, opacity), geometry.Thickness * 0.5f);
                Fill(draw, ArcOrigin, geometry.Mp, 0f, Math.Clamp(mp, 0f, 1f),
                    Colour(mpColour, opacity), geometry.Thickness * 0.5f);
            }
        }
        finally { draw.Flags = savedFlags; }

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
        try
        {
            var y = origin.Y + bodyHeight - headerHeight;
            var name = Line(text.Name, SentinelPalette.HeaderGold, headerX, headerWidth, right, ref y);
            var metadata = Line(text.Metadata, new Vector4(0.72f, 0.74f, 0.79f, 1f),
                headerX, headerWidth, right, ref y);
            HeaderBounds = name.Visible || metadata.Visible ? new ArcTextBounds(
                name.Visible && metadata.Visible ? Vector2.Min(name.Minimum, metadata.Minimum)
                    : name.Visible ? name.Minimum : metadata.Minimum,
                new Vector2(origin.X + headerX + headerWidth,
                    metadata.Visible ? metadata.Maximum.Y : name.Maximum.Y)) : default;
            y = origin.Y + footerY;
            HpTextBounds = Line(text.Hp, hpColour, outsideX, columnWidth, !right, ref y);
            MpTextBounds = Line(text.Mp, mpColour, outsideX, columnWidth, !right, ref y);
            Line(text.Shield, shieldColour, outsideX, columnWidth, !right, ref y);
            y = origin.Y + footerY;
            HpPercentageBounds = Line(text.HpPercentage, hpColour, insideX, columnWidth, right, ref y);
            Line(text.Distance, new Vector4(0.72f, 0.74f, 0.79f, 1f),
                insideX, columnWidth, right, ref y);
        }
        finally { ImGui.PopStyleVar(); }
        ImGui.SetCursorScreenPos(after);
        ImGui.Dummy(Vector2.Zero);

        float Measure(string value, float available) => value.Length == 0 ? 0f
            : ImGui.CalcTextSize(value, false, available).Y + spacing;

        ArcTextBounds Line(string value, Vector4 colour, float x, float available, bool alignRight, ref float y)
        {
            if (value.Length == 0) return default;
            var textWidth = Math.Min(available, ImGui.CalcTextSize(value, false, available).X);
            ImGui.SetCursorScreenPos(new Vector2(origin.X + x + (alignRight ? available - textWidth : 0f), y));
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + available);
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
