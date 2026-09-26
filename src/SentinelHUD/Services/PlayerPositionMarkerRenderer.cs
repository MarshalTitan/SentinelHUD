using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

public sealed class PlayerPositionMarkerRenderer(IGameGui gameGui)
{
    private const int SegmentCount = 24;
    private readonly IGameGui gameGui = gameGui;
    private readonly Vector2[] projectedPoints = new Vector2[SegmentCount];
    private readonly Vector2[] innerProjectedPoints = new Vector2[SegmentCount];

    public bool IsActive { get; private set; }
    public bool UsedTerrainProjection { get; private set; }
    public string StateReason { get; private set; } = "Off";

    public unsafe void Draw(
        PlayerPositionMarkerConfiguration configuration,
        bool hudEnabled,
        IPlayerCharacter? player,
        bool isLoggedIn,
        bool isInCombat,
        bool isInDuty,
        bool isInDanger)
    {
        IsActive = false;
        UsedTerrainProjection = false;
        if (!hudEnabled
            || !SelfHighlightPolicy.ShouldRender(configuration.Mode, isLoggedIn, isInCombat, isInDuty))
        {
            StateReason = !hudEnabled
                ? "Sentinel HUD disabled"
                : !isLoggedIn
                    ? "Not logged in"
                    : configuration.Mode switch
                    {
                        SelfHighlightMode.Off => "Off",
                        SelfHighlightMode.CombatOnly => "Waiting for combat",
                        SelfHighlightMode.DutyOnly => "Waiting for duty",
                        _ => "Mode condition not met",
                    };
            return;
        }
        if (gameGui.GameUiHidden)
        {
            StateReason = "Game UI hidden";
            return;
        }

        if (player is null)
        {
            StateReason = "Local player unavailable";
            return;
        }

        var groundPoint = player.Position;
        var groundNormal = Vector3.UnitY;
        try
        {
            var rayOrigin = player.Position + new Vector3(0f, 2.5f, 0f);
            if (BGCollisionModule.RaycastMaterialFilter(
                    rayOrigin,
                    -Vector3.UnitY,
                    out var hit,
                    maxDistance: 12f)
                && IsFinite(hit.Point)
                && Vector2.Distance(
                    new Vector2(hit.Point.X, hit.Point.Z),
                    new Vector2(player.Position.X, player.Position.Z)) < 0.75f)
            {
                groundPoint = hit.Point;
                if (IsFinite(hit.Normal) && hit.Normal.LengthSquared() > 0.25f)
                    groundNormal = Vector3.Normalize(hit.Normal);
                UsedTerrainProjection = true;
            }
        }
        catch
        {
            // The actor origin is the fail-safe fallback when collision data is unavailable during transitions.
        }

        var centre = groundPoint + (groundNormal * 0.025f);
        if (!gameGui.WorldToScreen(centre, out var centreScreen))
        {
            StateReason = "Player origin is outside the viewport";
            return;
        }

        BuildTangentBasis(groundNormal, out var tangent, out var bitangent);
        for (var index = 0; index < SegmentCount; index++)
        {
            var angle = (MathF.Tau * index) / SegmentCount;
            var point = centre
                        + (tangent * (MathF.Cos(angle) * configuration.Radius))
                        + (bitangent * (MathF.Sin(angle) * configuration.Radius));
            if (!gameGui.WorldToScreen(point, out projectedPoints[index]))
            {
                StateReason = "Marker edge is outside the viewport";
                return;
            }
        }

        var colour = PlayerPositionMarkerPolicy.ResolveColour(configuration, isInDanger);
        var drawList = ImGui.GetBackgroundDrawList();
        if (configuration.ShowBorder)
        {
            var luminance = (colour.X * 0.2126f) + (colour.Y * 0.7152f) + (colour.Z * 0.0722f);
            var border = luminance > 0.55f
                ? new Vector4(0.02f, 0.02f, 0.02f, colour.W)
                : new Vector4(1f, 1f, 1f, colour.W);

            drawList.AddConvexPolyFilled(
                ref projectedPoints[0],
                SegmentCount,
                ImGui.ColorConvertFloat4ToU32(border));

            var smallestScreenRadius = float.MaxValue;
            for (var index = 0; index < SegmentCount; index++)
                smallestScreenRadius = Math.Min(smallestScreenRadius, Vector2.Distance(centreScreen, projectedPoints[index]));
            var inwardThickness = Math.Min(configuration.BorderThickness, smallestScreenRadius * 0.45f);
            for (var index = 0; index < SegmentCount; index++)
            {
                var fromCentre = projectedPoints[index] - centreScreen;
                var length = fromCentre.Length();
                innerProjectedPoints[index] = length <= 0.001f
                    ? centreScreen
                    : centreScreen + (fromCentre * Math.Max(0f, length - inwardThickness) / length);
            }

            drawList.AddConvexPolyFilled(
                ref innerProjectedPoints[0],
                SegmentCount,
                ImGui.ColorConvertFloat4ToU32(colour));
        }
        else
        {
            drawList.AddConvexPolyFilled(
                ref projectedPoints[0],
                SegmentCount,
                ImGui.ColorConvertFloat4ToU32(colour));
        }

        IsActive = true;
        StateReason = (UsedTerrainProjection
            ? "Active at collision-projected actor origin"
            : "Active at actor origin (terrain collision unavailable)")
            + (isInDanger && configuration.DangerDetectionEnabled
                ? " — danger colour active"
                : string.Empty);
    }

    private static void BuildTangentBasis(Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
    {
        var reference = Math.Abs(Vector3.Dot(normal, Vector3.UnitZ)) > 0.95f
            ? Vector3.UnitX
            : Vector3.UnitZ;
        tangent = Vector3.Normalize(Vector3.Cross(reference, normal));
        bitangent = Vector3.Normalize(Vector3.Cross(normal, tangent));
    }

    private static bool IsFinite(Vector3 value)
        => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
