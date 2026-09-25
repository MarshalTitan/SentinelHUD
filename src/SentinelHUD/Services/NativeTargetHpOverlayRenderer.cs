using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

/// <summary>
/// Read-only overlay anchored to the native target-info addon. No native nodes are created or modified.
/// </summary>
public sealed class NativeTargetHpOverlayRenderer(IGameGui gameGui)
{
    private readonly IGameGui gameGui = gameGui;

    public bool IsActive { get; private set; }
    public string StateReason { get; private set; } = "Off";
    public Vector2 AnchorPosition { get; private set; }

    public unsafe void Draw(
        NativeTargetOverlayConfiguration configuration,
        bool hudEnabled,
        bool isLoggedIn,
        bool isInCombat,
        IGameObject? target)
    {
        IsActive = false;
        AnchorPosition = Vector2.Zero;
        if (!hudEnabled
            || !HudVisibilityPolicy.ShouldShowNativeTargetOverlay(configuration.Mode, isLoggedIn, isInCombat))
        {
            StateReason = !hudEnabled
                ? "Sentinel HUD disabled"
                : !isLoggedIn
                    ? "Not logged in"
                    : configuration.Mode switch
                    {
                        NativeTargetOverlayMode.Off => "Off",
                        NativeTargetOverlayMode.CombatOnly => "Waiting for combat",
                        _ => "Mode condition not met",
                    };
            return;
        }

        if (gameGui.GameUiHidden)
        {
            StateReason = "Game UI hidden";
            return;
        }

        if (target is not ICharacter { MaxHp: > 0 } character)
        {
            StateReason = "No valid target HP";
            return;
        }

        var addon = gameGui.GetAddonByName<AtkUnitBase>("_TargetInfo");
        if (addon is null || !addon->IsVisible || addon->RootNode is null)
        {
            StateReason = "Native target addon unavailable or hidden";
            return;
        }

        var root = addon->RootNode;
        var scaleX = Math.Abs(root->ScaleX * addon->Scale);
        var scaleY = Math.Abs(root->ScaleY * addon->Scale);
        var width = root->Width * scaleX;
        var height = root->Height * scaleY;
        if (!float.IsFinite(width) || !float.IsFinite(height) || width < 40f || height < 1f)
        {
            StateReason = "Native target addon bounds unavailable";
            return;
        }

        var left = addon->X + (root->X * addon->Scale) + configuration.OffsetX;
        var top = addon->Y + (root->Y * addon->Scale);
        var anchor = new Vector2(left, top + height + configuration.OffsetY);
        if (!float.IsFinite(anchor.X) || !float.IsFinite(anchor.Y))
        {
            StateReason = "Native target addon position unavailable";
            return;
        }

        var text = HudFormatting.NativeTargetHitPoints(
            character.CurrentHp,
            character.MaxHp,
            configuration.HpFormat,
            configuration.NumberFormat);
        var textSize = ImGui.CalcTextSize(text);
        var textPosition = new Vector2(
            anchor.X + Math.Max(0f, (width - textSize.X) * 0.5f),
            anchor.Y);
        var drawList = ImGui.GetForegroundDrawList();
        drawList.AddText(textPosition + new Vector2(1f, 1f), 0xE0000000, text);
        drawList.AddText(textPosition, 0xFFF4F4F4, text);

        AnchorPosition = anchor;
        IsActive = true;
        StateReason = "Active and anchored to _TargetInfo root bounds";
    }
}
