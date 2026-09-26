using System.Numerics;

namespace SentinelHUD.Core;

public enum DangerShapeKind
{
    Circle,
    Donut,
    Rectangle,
    Cone,
    Line,
    Cross,
}

/// <summary>
/// A compact world-space hazard representation. FFXIV's horizontal plane is X/Z; Y is used only
/// to reject hazards on another floor. DirectionRadians follows the game convention where zero
/// faces +Z and positive angles rotate toward +X.
/// </summary>
public readonly record struct DangerArea(
    DangerShapeKind Kind,
    Vector3 Origin,
    Vector3 End,
    float OuterRadius,
    float InnerRadius,
    float HalfWidth,
    float DirectionRadians,
    float HalfAngleRadians,
    float VerticalTolerance,
    string Source,
    string Label)
{
    private const float Epsilon = 0.0001f;

    public bool Contains(Vector3 point)
    {
        if (!IsFinite(point) || !IsFinite(Origin) || MathF.Abs(point.Y - Origin.Y) > VerticalTolerance)
            return false;

        return Kind switch
        {
            DangerShapeKind.Circle => ContainsCircle(point),
            DangerShapeKind.Donut => ContainsDonut(point),
            DangerShapeKind.Rectangle => ContainsRectangle(point, DirectionRadians),
            DangerShapeKind.Cone => ContainsCone(point),
            DangerShapeKind.Line => ContainsLine(point),
            DangerShapeKind.Cross => ContainsRectangle(point, DirectionRadians)
                                     || ContainsRectangle(point, DirectionRadians + (MathF.PI * 0.5f)),
            _ => false,
        };
    }

    public static DangerArea Circle(Vector3 origin, float radius, string source, string label,
        float verticalTolerance = 5f)
        => new(DangerShapeKind.Circle, origin, origin, radius, 0f, 0f, 0f, 0f,
            verticalTolerance, source, label);

    public static DangerArea Donut(Vector3 origin, float innerRadius, float outerRadius,
        string source, string label, float verticalTolerance = 5f)
        => new(DangerShapeKind.Donut, origin, origin, outerRadius, innerRadius, 0f, 0f, 0f,
            verticalTolerance, source, label);

    public static DangerArea Rectangle(Vector3 origin, float length, float halfWidth,
        float directionRadians, string source, string label, float verticalTolerance = 5f)
        => new(DangerShapeKind.Rectangle, origin, origin, length, 0f, halfWidth,
            directionRadians, 0f, verticalTolerance, source, label);

    public static DangerArea Cone(Vector3 origin, float radius, float directionRadians,
        float halfAngleRadians, string source, string label, float verticalTolerance = 5f)
        => new(DangerShapeKind.Cone, origin, origin, radius, 0f, 0f, directionRadians,
            halfAngleRadians, verticalTolerance, source, label);

    public static DangerArea Line(Vector3 start, Vector3 end, float halfWidth, string source,
        string label, float verticalTolerance = 5f)
        => new(DangerShapeKind.Line, start, end, 0f, 0f, halfWidth, 0f, 0f,
            verticalTolerance, source, label);

    public static DangerArea Cross(Vector3 origin, float armLength, float halfWidth,
        float directionRadians, string source, string label, float verticalTolerance = 5f)
        => new(DangerShapeKind.Cross, origin, origin, armLength, 0f, halfWidth,
            directionRadians, 0f, verticalTolerance, source, label);

    private bool ContainsCircle(Vector3 point)
        => DistanceSquaredXZ(point, Origin) <= Square(MathF.Max(0f, OuterRadius));

    private bool ContainsDonut(Vector3 point)
    {
        var distanceSquared = DistanceSquaredXZ(point, Origin);
        var inner = MathF.Max(0f, InnerRadius);
        var outer = MathF.Max(inner, OuterRadius);
        return distanceSquared + Epsilon >= Square(inner)
               && distanceSquared <= Square(outer) + Epsilon;
    }

    private bool ContainsRectangle(Vector3 point, float direction)
    {
        var relative = new Vector2(point.X - Origin.X, point.Z - Origin.Z);
        var forward = new Vector2(MathF.Sin(direction), MathF.Cos(direction));
        var right = new Vector2(forward.Y, -forward.X);
        var along = Vector2.Dot(relative, forward);
        var across = MathF.Abs(Vector2.Dot(relative, right));
        return along >= -Epsilon && along <= MathF.Max(0f, OuterRadius) + Epsilon
               && across <= MathF.Max(0f, HalfWidth) + Epsilon;
    }

    private bool ContainsCone(Vector3 point)
    {
        var relative = new Vector2(point.X - Origin.X, point.Z - Origin.Z);
        var distanceSquared = relative.LengthSquared();
        if (distanceSquared > Square(MathF.Max(0f, OuterRadius)) + Epsilon)
            return false;
        if (distanceSquared <= Epsilon)
            return true;

        var pointAngle = MathF.Atan2(relative.X, relative.Y);
        return MathF.Abs(NormalizeAngle(pointAngle - DirectionRadians))
               <= Math.Clamp(HalfAngleRadians, 0f, MathF.PI) + Epsilon;
    }

    private bool ContainsLine(Vector3 point)
    {
        if (!IsFinite(End) || MathF.Abs(point.Y - End.Y) > VerticalTolerance)
            return false;
        var start = new Vector2(Origin.X, Origin.Z);
        var end = new Vector2(End.X, End.Z);
        var candidate = new Vector2(point.X, point.Z);
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared <= Epsilon)
            return Vector2.DistanceSquared(candidate, start) <= Square(MathF.Max(0f, HalfWidth));
        var progress = Math.Clamp(Vector2.Dot(candidate - start, segment) / lengthSquared, 0f, 1f);
        var closest = start + (segment * progress);
        return Vector2.DistanceSquared(candidate, closest) <= Square(MathF.Max(0f, HalfWidth)) + Epsilon;
    }

    public static float DirectionTo(Vector3 from, Vector3 to, float fallback)
    {
        var deltaX = to.X - from.X;
        var deltaZ = to.Z - from.Z;
        return MathF.Abs(deltaX) + MathF.Abs(deltaZ) <= Epsilon
            ? fallback
            : MathF.Atan2(deltaX, deltaZ);
    }

    public static float NormalizeAngle(float value)
    {
        while (value > MathF.PI)
            value -= MathF.Tau;
        while (value < -MathF.PI)
            value += MathF.Tau;
        return value;
    }

    private static float DistanceSquaredXZ(Vector3 left, Vector3 right)
    {
        var x = left.X - right.X;
        var z = left.Z - right.Z;
        return (x * x) + (z * z);
    }

    private static float Square(float value) => value * value;

    private static bool IsFinite(Vector3 value)
        => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
