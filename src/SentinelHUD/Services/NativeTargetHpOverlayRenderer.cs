using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SentinelHUD.Core;
using NativeBounds = FFXIVClientStructs.FFXIV.Common.Math.Bounds;

namespace SentinelHUD.Services;

/// <summary>
/// Read-only overlay anchored to the visible stock target HP gauge. No native nodes are created or modified.
/// </summary>
public sealed class NativeTargetHpOverlayRenderer(IGameGui gameGui)
{
    private const string SplitAddonName = "_TargetInfoMainTarget";
    private const string CombinedAddonName = "_TargetInfo";
    private const uint SplitHpGaugeNodeId = 13;
    private const uint CombinedHpGaugeNodeId = 19;

    private readonly IGameGui gameGui = gameGui;

    public bool IsActive { get; private set; }
    public bool TargetExists { get; private set; }
    public bool SplitAddonAvailable { get; private set; }
    public bool SplitAddonVisible { get; private set; }
    public bool CombinedAddonAvailable { get; private set; }
    public bool CombinedAddonVisible { get; private set; }
    public string DetectedAddonName { get; private set; } = "None";
    public string DetectedLayout { get; private set; } = "None";
    public string AnchorSource { get; private set; } = "None";
    public string StateReason { get; private set; } = "Off";
    public Vector2 AnchorPosition { get; private set; }
    public Vector2 AnchorSize { get; private set; }

    public unsafe void Draw(
        NativeTargetOverlayConfiguration configuration,
        bool hudEnabled,
        bool isLoggedIn,
        bool isInCombat,
        IGameObject? target)
    {
        ResetRuntimeState(target is not null);
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
            StateReason = target is null ? "No target" : "Target has no valid HP data";
            return;
        }

        var split = ReadCandidate(SplitAddonName, SplitHpGaugeNodeId, configuration);
        SplitAddonAvailable = split.Available;
        SplitAddonVisible = split.Visible;
        var combined = ReadCandidate(CombinedAddonName, CombinedHpGaugeNodeId, configuration);
        CombinedAddonAvailable = combined.Available;
        CombinedAddonVisible = combined.Visible;

        var selected = split.HasAnchor ? split : combined;
        if (!selected.HasAnchor)
        {
            StateReason = BuildFailureReason(split, combined);
            return;
        }

        var text = HudFormatting.NativeTargetHitPoints(
            character.CurrentHp,
            character.MaxHp,
            configuration.HpFormat,
            configuration.NumberFormat);
        var textSize = ImGui.CalcTextSize(text);
        var textPosition = new Vector2(
            selected.Anchor.Position.X + Math.Max(0f, (selected.Anchor.Size.X - textSize.X) * 0.5f),
            selected.Anchor.Position.Y);
        var drawList = ImGui.GetForegroundDrawList();
        drawList.AddText(textPosition + new Vector2(1f, 1f), 0xE0000000, text);
        drawList.AddText(textPosition, 0xFFF4F4F4, text);

        AnchorPosition = selected.Anchor.Position;
        AnchorSize = selected.Anchor.Size;
        DetectedAddonName = selected.AddonName;
        DetectedLayout = selected.LayoutName;
        AnchorSource = selected.Source;
        IsActive = true;
        StateReason = "Active";
    }

    private unsafe AddonCandidate ReadCandidate(
        string addonName,
        uint gaugeNodeId,
        NativeTargetOverlayConfiguration configuration)
    {
        var addon = gameGui.GetAddonByName<AtkUnitBase>(addonName);
        if (addon is null)
            return AddonCandidate.Unavailable(addonName, LayoutName(addonName));
        if (!addon->IsVisible || addon->RootNode is null)
            return AddonCandidate.Hidden(addonName, LayoutName(addonName));

        var gauge = addon->GetNodeById(gaugeNodeId);
        if (gauge is not null && gauge->IsVisible())
        {
            NativeBounds bounds = default;
            gauge->GetBounds(&bounds);
            if (NativeTargetAnchorPolicy.TryCreate(
                    new Vector2(bounds.Pos1.X, bounds.Pos1.Y),
                    new Vector2(bounds.Pos2.X, bounds.Pos2.Y),
                    configuration.OffsetX, configuration.OffsetY, out var gaugeAnchor))
            {
                return AddonCandidate.Anchored(addonName, LayoutName(addonName),
                    $"HP gauge node {gaugeNodeId}", gaugeAnchor);
            }
        }

        NativeBounds rootBounds = default;
        addon->GetRootBounds(&rootBounds);
        if (NativeTargetAnchorPolicy.TryCreate(
                new Vector2(rootBounds.Pos1.X, rootBounds.Pos1.Y),
                new Vector2(rootBounds.Pos2.X, rootBounds.Pos2.Y),
                configuration.OffsetX, configuration.OffsetY, out var rootAnchor))
        {
            return AddonCandidate.Anchored(addonName, LayoutName(addonName),
                "addon root fallback", rootAnchor);
        }

        return AddonCandidate.InvalidBounds(addonName, LayoutName(addonName));
    }

    private static string BuildFailureReason(AddonCandidate split, AddonCandidate combined)
    {
        if (!split.Available && !combined.Available)
            return "Neither split nor combined target addon exists";
        if (!split.Visible && !combined.Visible)
            return "Supported target addons exist but are hidden";
        return "A target addon is visible, but its HP gauge and root bounds are unavailable";
    }

    private static string LayoutName(string addonName)
        => addonName == SplitAddonName ? "Split Target Info" : "Combined Target Info";

    private void ResetRuntimeState(bool targetExists)
    {
        IsActive = false;
        TargetExists = targetExists;
        SplitAddonAvailable = false;
        SplitAddonVisible = false;
        CombinedAddonAvailable = false;
        CombinedAddonVisible = false;
        DetectedAddonName = "None";
        DetectedLayout = "None";
        AnchorSource = "None";
        AnchorPosition = Vector2.Zero;
        AnchorSize = Vector2.Zero;
    }

    private readonly record struct AddonCandidate(
        string AddonName,
        string LayoutName,
        bool Available,
        bool Visible,
        bool HasAnchor,
        string Source,
        NativeTargetAnchor Anchor)
    {
        public static AddonCandidate Unavailable(string addonName, string layoutName)
            => new(addonName, layoutName, false, false, false, "None", default);

        public static AddonCandidate Hidden(string addonName, string layoutName)
            => new(addonName, layoutName, true, false, false, "None", default);

        public static AddonCandidate InvalidBounds(string addonName, string layoutName)
            => new(addonName, layoutName, true, true, false, "Invalid bounds", default);

        public static AddonCandidate Anchored(string addonName, string layoutName,
            string source, NativeTargetAnchor anchor)
            => new(addonName, layoutName, true, true, true, source, anchor);
    }
}
