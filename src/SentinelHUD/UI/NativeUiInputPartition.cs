using System.Numerics;
using SentinelHUD.Core;

namespace SentinelHUD.UI;

/// <summary>Reusable input islands; internal cut edges never become editor resize edges.</summary>
public sealed class NativeUiInputPartition
{
    private readonly List<ScreenRectangle> pieces = new(8);
    private readonly List<ScreenRectangle> scratch = new(8);
    private readonly Dictionary<(string, int), string> names = new();
    public IReadOnlyList<ScreenRectangle> Split(Vector2 minimum, Vector2 maximum, NativeUiOcclusionMask? mask)
    {
        var source = new ScreenRectangle(minimum.X, minimum.Y, maximum.X, maximum.Y);
        if (mask is null || !mask.Intersects(source))
        { pieces.Clear(); if (NativeUiOcclusionMask.Valid(source)) pieces.Add(source); }
        else
        {
            mask.Clip(source, pieces, scratch);
            // ImGui rounds/clamps input windows to at least one pixel. Round cutouts inward so a
            // subpixel sliver cannot grow back into the native panel or become an auto-fit window.
            for (var i = pieces.Count - 1; i >= 0; i--)
            {
                var r = pieces[i];
                r = new(MathF.Ceiling(r.Left), MathF.Ceiling(r.Top), MathF.Floor(r.Right), MathF.Floor(r.Bottom));
                if (NativeUiOcclusionMask.Valid(r)) pieces[i] = r;
                else pieces.RemoveAt(i);
            }
        }
        return pieces;
    }
    public string Name(string original, int index)
    {
        if (index == 0) return original;
        if (!names.TryGetValue((original, index), out var name))
            names[(original, index)] = name = original + ".VisiblePart" + index;
        return name;
    }
}
