using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

public sealed class SelfHighlightRenderer(
    IClientState clientState,
    ICondition condition,
    IGameGui gameGui,
    HudDataService data)
{
    private readonly IClientState clientState = clientState;
    private readonly ICondition condition = condition;
    private readonly IGameGui gameGui = gameGui;
    private readonly HudDataService data = data;

    public bool IsActive { get; private set; }
    public string StateReason { get; private set; } = "Off";

    public void Draw(SelfHighlightConfiguration configuration)
    {
        IsActive = false;
        var inCombat = condition[ConditionFlag.InCombat];
        var inDuty = condition.Any(
            ConditionFlag.BoundByDuty,
            ConditionFlag.BoundByDuty56,
            ConditionFlag.BoundByDuty95);

        if (!SelfHighlightPolicy.ShouldRender(configuration.Mode, clientState.IsLoggedIn, inCombat, inDuty))
        {
            StateReason = clientState.IsLoggedIn ? $"Mode condition not met ({configuration.Mode})" : "Not logged in";
            return;
        }
        if (gameGui.GameUiHidden)
        {
            StateReason = "Game UI hidden";
            return;
        }

        var player = data.LocalPlayer;
        if (player is null)
        {
            StateReason = "Local player unavailable";
            return;
        }

        var feetWorld = player.Position + new Vector3(0f, 0.05f, 0f);
        var headWorld = player.Position + new Vector3(0f, 1.8f, 0f);
        if (!gameGui.WorldToScreen(feetWorld, out var feet)
            || !gameGui.WorldToScreen(headWorld, out var head))
        {
            StateReason = "Player is outside the viewport";
            return;
        }

        var height = Math.Abs(feet.Y - head.Y);
        if (!float.IsFinite(height) || height < 12f || height > 900f)
        {
            StateReason = "Projected character size is unavailable";
            return;
        }

        var centreX = (feet.X + head.X) * 0.5f;
        var topY = Math.Min(head.Y, feet.Y);
        var bottomY = Math.Max(head.Y, feet.Y);
        DrawBodyAura(centreX, topY, bottomY, SelfHighlightPolicy.ResolveColour(configuration));
        IsActive = true;
        StateReason = "Sentinel-rendered body aura active";
    }

    private static void DrawBodyAura(float centreX, float topY, float bottomY, Vector4 colour)
    {
        var drawList = ImGui.GetBackgroundDrawList();
        var height = bottomY - topY;
        var headCentre = new Vector2(centreX, topY + (height * 0.09f));
        var headRadius = Math.Clamp(height * 0.075f, 4f, 24f);
        var shoulderY = topY + (height * 0.25f);
        var waistY = topY + (height * 0.58f);
        var kneeY = topY + (height * 0.77f);
        var shoulderHalf = Math.Clamp(height * 0.15f, 7f, 45f);
        var waistHalf = shoulderHalf * 0.58f;
        var footHalf = shoulderHalf * 0.42f;

        DrawCircleGlow(drawList, headCentre, headRadius, colour);
        DrawGlowLine(drawList, new Vector2(centreX - shoulderHalf, shoulderY), new Vector2(centreX - waistHalf, waistY), colour);
        DrawGlowLine(drawList, new Vector2(centreX + shoulderHalf, shoulderY), new Vector2(centreX + waistHalf, waistY), colour);
        DrawGlowLine(drawList, new Vector2(centreX - waistHalf, waistY), new Vector2(centreX - footHalf, bottomY), colour);
        DrawGlowLine(drawList, new Vector2(centreX + waistHalf, waistY), new Vector2(centreX + footHalf, bottomY), colour);
        DrawGlowLine(drawList, new Vector2(centreX - shoulderHalf, shoulderY), new Vector2(centreX - (shoulderHalf * 1.28f), kneeY), colour);
        DrawGlowLine(drawList, new Vector2(centreX + shoulderHalf, shoulderY), new Vector2(centreX + (shoulderHalf * 1.28f), kneeY), colour);
        DrawGlowLine(drawList, new Vector2(centreX - shoulderHalf, shoulderY), new Vector2(centreX + shoulderHalf, shoulderY), colour);
    }

    private static void DrawGlowLine(ImDrawListPtr drawList, Vector2 start, Vector2 end, Vector4 colour)
    {
        var outer = colour with { W = colour.W * 0.12f };
        var middle = colour with { W = colour.W * 0.28f };
        var edge = colour with { W = colour.W * 0.88f };
        drawList.AddLine(start, end, ImGui.ColorConvertFloat4ToU32(outer), 12f);
        drawList.AddLine(start, end, ImGui.ColorConvertFloat4ToU32(middle), 6f);
        drawList.AddLine(start, end, ImGui.ColorConvertFloat4ToU32(edge), 2f);
    }

    private static void DrawCircleGlow(ImDrawListPtr drawList, Vector2 centre, float radius, Vector4 colour)
    {
        drawList.AddCircle(centre, radius + 5f, ImGui.ColorConvertFloat4ToU32(colour with { W = colour.W * 0.12f }), 28, 10f);
        drawList.AddCircle(centre, radius + 2f, ImGui.ColorConvertFloat4ToU32(colour with { W = colour.W * 0.28f }), 28, 5f);
        drawList.AddCircle(centre, radius, ImGui.ColorConvertFloat4ToU32(colour with { W = colour.W * 0.9f }), 28, 2f);
    }
}
