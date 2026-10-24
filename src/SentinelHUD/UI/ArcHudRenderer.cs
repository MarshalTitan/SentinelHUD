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
    public ArcTextBounds MpPercentageBounds { get; private set; }
    public Vector2 ArcOrigin { get; private set; }
    public int HitBandCount => ArcHitRegions.BandsPerTrack * (mpVisible ? 2 : 1);

    public ArcHitBand GetHitBand(int index)
    {
        var mp = index >= ArcHitRegions.BandsPerTrack;
        return ArcHitRegions.Band(mp ? geometry.Mp : geometry.Hp,
            index % ArcHitRegions.BandsPerTrack,
            (mp ? geometry.Thickness * 0.5f : geometry.Thickness) + 4f, ArcOrigin);
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
        var bow = Math.Abs(geometry.Hp[0].X - geometry.Hp[ArcBarGeometry.Segments / 2].X);
        var outsideWidth = Math.Max(12f, half - bow - geometry.Thickness * 0.5f - gap - 2f);
        var outsideX = right ? width - outsideWidth : 0f;
        var spacing = ImGui.GetStyle().ItemSpacing.Y;
        var innerBarEdge = showMp ? geometry.Thickness + 5f + geometry.Thickness * 0.25f
            : geometry.Thickness * 0.5f;
        var headerWidth = Math.Max(12f, half - innerBarEdge - gap - 1f);
        var headerX = right ? 0f : width - headerWidth;
        var insideX = headerX;
        var headerHeight = Measure(text.Name, headerWidth) + Measure(text.Metadata, headerWidth);
        var hpHeight = Math.Max(Measure(text.Hp, outsideWidth), Measure(text.HpPercentage, headerWidth));
        var mpHeight = Math.Max(Measure(text.Mp, outsideWidth), Measure(text.MpPercentage, headerWidth));
        var extraHeight = Math.Max(Measure(text.Shield, outsideWidth), Measure(text.Distance, headerWidth));
        var resourcesHeight = hpHeight + mpHeight + extraHeight;
        var bodyHeight = Math.Max(arcHeight, headerHeight + gap + resourcesHeight);
        var resourcesY = Math.Max(headerHeight + gap, (bodyHeight - resourcesHeight) * 0.6f);
        var size = new Vector2(width, bodyHeight);
        ImGui.Dummy(size);
        var after = ImGui.GetCursorScreenPos();
        ArcOrigin = origin + new Vector2(half - geometry.Hp[0].X, (bodyHeight - arcHeight) * 0.5f);
        var draw = ImGui.GetWindowDrawList();
        var savedFlags = draw.Flags;
        var background = new Vector4(0.025f, 0.03f, 0.045f, 0.94f);
        var border = new Vector4(0.38f, 0.40f, 0.46f, module.BorderOpacity);
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
            Fill(draw, ArcOrigin, geometry.Hp, 0f, fill.HealthEnd, Colour(hpColour, opacity), geometry.Thickness, true);
            if (fill.HasOverlay) Fill(draw, ArcOrigin, geometry.Hp, fill.ShieldOverlayStart,
                fill.HealthEnd, Colour(shieldColour, opacity), geometry.Thickness, true);
            if (fill.HasExtension) Fill(draw, ArcOrigin, geometry.Hp, fill.HealthEnd,
                fill.ShieldExtensionEnd, Colour(shieldColour, opacity), geometry.Thickness, true);
            var gloss = Colour(new Vector4(1f, 1f, 1f, 0.10f), opacity);
            Fill(draw, ArcOrigin, geometry.Hp, 0f, 1f, gloss, geometry.Thickness * 0.12f, false, -geometry.Thickness * 0.22f);
            if (showMp)
            {
                Fill(draw, ArcOrigin, geometry.Mp, 0f, 1f, Colour(background, opacity), geometry.Thickness * 0.5f);
                Fill(draw, ArcOrigin, geometry.Mp, 0f, Math.Clamp(mp, 0f, 1f),
                    Colour(mpColour, opacity), geometry.Thickness * 0.5f, true);
                Fill(draw, ArcOrigin, geometry.Mp, 0f, 1f, gloss, geometry.Thickness * 0.06f, false, -geometry.Thickness * 0.11f);
            }
        }
        finally { draw.Flags = savedFlags; }

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
        try
        {
            var y = origin.Y + resourcesY - headerHeight - gap;
            var name = Line(text.Name, SentinelPalette.HeaderGold, headerX, headerWidth, right, ref y);
            var metadata = Line(text.Metadata, new Vector4(0.72f, 0.74f, 0.79f, 1f),
                headerX, headerWidth, right, ref y);
            HeaderBounds = name.Visible || metadata.Visible ? new ArcTextBounds(
                name.Visible && metadata.Visible ? Vector2.Min(name.Minimum, metadata.Minimum)
                    : name.Visible ? name.Minimum : metadata.Minimum,
                new Vector2(origin.X + headerX + headerWidth,
                    metadata.Visible ? metadata.Maximum.Y : name.Maximum.Y)) : default;
            y = origin.Y + resourcesY;
            HpTextBounds = Line(text.Hp, hpColour, outsideX, outsideWidth, !right, ref y);
            y = origin.Y + resourcesY + hpHeight;
            MpTextBounds = Line(text.Mp, mpColour, outsideX, outsideWidth, !right, ref y);
            y = origin.Y + resourcesY + hpHeight + mpHeight;
            Line(text.Shield, shieldColour, outsideX, outsideWidth, !right, ref y);
            y = origin.Y + resourcesY + (hpHeight - Measure(text.HpPercentage, headerWidth)) * 0.5f;
            HpPercentageBounds = Line(text.HpPercentage, hpColour, insideX, headerWidth, right, ref y);
            y = origin.Y + resourcesY + hpHeight + (mpHeight - Measure(text.MpPercentage, headerWidth)) * 0.5f;
            MpPercentageBounds = Line(text.MpPercentage, mpColour, insideX, headerWidth, right, ref y);
            y = origin.Y + resourcesY + hpHeight + mpHeight;
            Line(text.Distance, new Vector4(0.72f, 0.74f, 0.79f, 1f),
                insideX, headerWidth, right, ref y);
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
        float start, float end, uint colour, float thickness, bool liquid = false, float offset = 0f)
    {
        start = Math.Clamp(start, 0f, 1f);
        end = Math.Clamp(end, 0f, 1f);
        if (end <= start) return;
        var first = start * ArcBarGeometry.Segments;
        var last = end * ArcBarGeometry.Segments;
        var left = liquid ? Shade(colour, 0.62f) : colour;
        var middle = liquid ? Shade(colour, 1.08f) : colour;
        var right = liquid ? Shade(colour, 0.75f) : colour;
        var half = thickness * 0.5f;
        for (var i = (int)first; i < last; i++)
        {
            var bottom = origin + Vector2.Lerp(points[i], points[i + 1], Math.Max(first, i) - i) + new Vector2(offset, 0f);
            var top = origin + Vector2.Lerp(points[i], points[i + 1], Math.Min(last, i + 1) - i) + new Vector2(offset, 0f);
            Band(draw, bottom, top, -half - 1f, -half, left & 0x00FFFFFF, left);
            if (liquid)
            {
                Band(draw, bottom, top, -half, -thickness * 0.12f, left, middle);
                Band(draw, bottom, top, -thickness * 0.12f, thickness * 0.16f, middle, middle);
                Band(draw, bottom, top, thickness * 0.16f, half, middle, right);
            }
            else Band(draw, bottom, top, -half, half, colour, colour);
            Band(draw, bottom, top, half, half + 1f, right, right & 0x00FFFFFF);
        }
    }

    private static uint Shade(uint colour, float factor)
        => (colour & 0xFF000000) | (uint)Math.Min(255f, (colour & 255) * factor)
           | ((uint)Math.Min(255f, ((colour >> 8) & 255) * factor) << 8)
           | ((uint)Math.Min(255f, ((colour >> 16) & 255) * factor) << 16);

    private static unsafe void Band(ImDrawListPtr draw, Vector2 bottom, Vector2 top, float left, float right, uint a, uint b)
    {
        if (((a | b) & 0xFF000000) == 0) return;
        var index = draw.VtxBuffer.Size;
        draw.AddQuadFilled(bottom + new Vector2(left, 0f), bottom + new Vector2(right, 0f),
            top + new Vector2(right, 0f), top + new Vector2(left, 0f), a | 0xFF000000);
        if (draw.VtxBuffer.Size != index + 4) return;
        // AddQuadFilled may grow the native buffer, so obtain its pointer only after adding the quad.
        var vertices = (ImDrawVert*)draw.VtxBuffer.Data;
        vertices[index].Col = a;
        vertices[index + 1].Col = b;
        vertices[index + 2].Col = b;
        vertices[index + 3].Col = a;
    }
}
