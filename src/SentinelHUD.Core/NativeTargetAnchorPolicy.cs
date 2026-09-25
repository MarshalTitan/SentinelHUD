using System.Numerics;

namespace SentinelHUD.Core;

public readonly record struct NativeTargetAnchor(
    Vector2 Position,
    Vector2 Size);

public static class NativeTargetAnchorPolicy
{
    public static bool TryCreate(
        Vector2 minimum,
        Vector2 maximum,
        float offsetX,
        float offsetY,
        out NativeTargetAnchor anchor)
    {
        var size = maximum - minimum;
        if (!float.IsFinite(minimum.X)
            || !float.IsFinite(minimum.Y)
            || !float.IsFinite(size.X)
            || !float.IsFinite(size.Y)
            || size.X < 40f
            || size.Y < 1f)
        {
            anchor = default;
            return false;
        }

        var position = new Vector2(minimum.X + offsetX, maximum.Y + offsetY);
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
        {
            anchor = default;
            return false;
        }

        anchor = new NativeTargetAnchor(position, size);
        return true;
    }
}
