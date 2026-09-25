using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

public interface IExtendedCameraZoomService : IDisposable
{
    bool IsActive { get; }
    string StateReason { get; }
    string? ConflictingPluginName { get; }
    float CurrentMaximum { get; }
    void Update(ExtendedCameraZoomConfiguration configuration, bool hudEnabled);
    void Restore();
    void RetryAfterConflict();
}

/// <summary>
/// Owns the narrow, reversible camera-limit write used for extended third-person zoom. The layout mirrors
/// the current API 15 Cammy technique but intentionally does not hook camera functions or alter FoV/collision.
/// </summary>
public sealed unsafe class ExtendedCameraZoomService : IExtendedCameraZoomService
{
    private static readonly string[] KnownCameraControllers =
    [
        "Cammy",
        "EasyZoom",
        "EasyZoomReborn",
        "ZoomTilt",
        "PyonCam",
    ];

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IClientState clientState;
    private readonly ICondition condition;
    private bool conflictScanPending = true;
    private bool externalOverrideDetected;
    private bool wasRequested;
    private nint ownedCameraAddress;
    private float originalMaximum;
    private float appliedMaximum;
    private string? reportedConflictName;

    public ExtendedCameraZoomService(
        IDalamudPluginInterface pluginInterface,
        IClientState clientState,
        ICondition condition)
    {
        this.pluginInterface = pluginInterface;
        this.clientState = clientState;
        this.condition = condition;
        pluginInterface.ActivePluginsChanged += OnActivePluginsChanged;
    }

    public bool IsActive { get; private set; }
    public string StateReason { get; private set; } = "Off";
    public string? ConflictingPluginName { get; private set; }
    public float CurrentMaximum { get; private set; } = CameraZoomPolicy.StockMaximum;

    public void Update(ExtendedCameraZoomConfiguration configuration, bool hudEnabled)
    {
        var requested = hudEnabled && configuration.Enabled;
        if (!requested)
        {
            Restore();
            if (wasRequested)
                externalOverrideDetected = false;
            wasRequested = false;
            reportedConflictName = null;
            StateReason = hudEnabled ? "Off" : "Sentinel HUD disabled";
            return;
        }
        wasRequested = true;

        if (conflictScanPending)
            RefreshKnownConflicts();
        if (ConflictingPluginName is not null)
        {
            Restore();
            if (!string.Equals(reportedConflictName, ConflictingPluginName, StringComparison.Ordinal))
            {
                reportedConflictName = ConflictingPluginName;
                StateReason = $"Paused because {ConflictingPluginName} is controlling the camera";
            }
            return;
        }
        reportedConflictName = null;
        if (externalOverrideDetected)
        {
            IsActive = false;
            StateReason = "Paused after detecting another camera writer; disable/re-enable or press Retry";
            return;
        }
        if (!clientState.IsLoggedIn)
        {
            AbandonOwnership();
            StateReason = "Not logged in";
            return;
        }
        if (IsSpecialCameraState())
        {
            Restore();
            StateReason = GetSpecialStateReason();
            return;
        }

        var manager = CameraManager.Instance();
        var camera = manager is null ? null : (WorldCameraZoomState*)manager->Camera;
        if (camera is null)
        {
            AbandonOwnership();
            StateReason = "World camera unavailable";
            return;
        }
        if (manager->ActiveCameraIndex != 0 || camera->Mode != 1)
        {
            Restore();
            StateReason = camera->Mode == 0 ? "Paused in first-person mode" : "Paused for a non-standard camera";
            return;
        }

        var cameraAddress = (nint)camera;
        if (ownedCameraAddress != nint.Zero && ownedCameraAddress != cameraAddress)
            AbandonOwnership();

        if (ownedCameraAddress == nint.Zero)
        {
            if (!IsSaneMaximum(camera->MaximumZoom))
            {
                StateReason = "Camera maximum failed validation";
                return;
            }
            ownedCameraAddress = cameraAddress;
            originalMaximum = camera->MaximumZoom;
        }
        else if (!NearlyEqual(camera->MaximumZoom, appliedMaximum)
                 && !NearlyEqual(camera->MaximumZoom, originalMaximum))
        {
            CurrentMaximum = camera->MaximumZoom;
            AbandonOwnership();
            externalOverrideDetected = true;
            StateReason = "Paused after detecting another camera writer; disable/re-enable or press Retry";
            return;
        }

        var desired = CameraZoomPolicy.NormalizeMaximum(configuration.MaximumZoomDistance);
        var stateChanged = !IsActive || !NearlyEqual(appliedMaximum, desired);
        camera->MaximumZoom = desired;
        if (camera->CurrentZoom > desired)
        {
            camera->CurrentZoom = desired;
            camera->InterpolatedZoom = Math.Min(camera->InterpolatedZoom, desired);
        }
        appliedMaximum = desired;
        CurrentMaximum = desired;
        IsActive = true;
        if (stateChanged)
            StateReason = $"Active (maximum {desired:0.0} yalms)";
    }

    public void Restore()
    {
        if (ownedCameraAddress != nint.Zero)
        {
            var manager = CameraManager.Instance();
            var current = manager is null ? nint.Zero : (nint)manager->Camera;
            if (current == ownedCameraAddress)
            {
                var camera = (WorldCameraZoomState*)ownedCameraAddress;
                camera->MaximumZoom = originalMaximum;
                if (camera->CurrentZoom > originalMaximum)
                {
                    camera->CurrentZoom = originalMaximum;
                    camera->InterpolatedZoom = Math.Min(camera->InterpolatedZoom, originalMaximum);
                }
                CurrentMaximum = originalMaximum;
            }
        }
        AbandonOwnership();
    }

    public void RetryAfterConflict()
    {
        externalOverrideDetected = false;
        conflictScanPending = true;
        reportedConflictName = null;
        StateReason = "Retry requested";
    }

    public void Dispose()
    {
        pluginInterface.ActivePluginsChanged -= OnActivePluginsChanged;
        Restore();
    }

    private void OnActivePluginsChanged(IActivePluginsChangedEventArgs _) => conflictScanPending = true;

    private void RefreshKnownConflicts()
    {
        conflictScanPending = false;
        ConflictingPluginName = pluginInterface.InstalledPlugins
            .FirstOrDefault(plugin => plugin.IsLoaded
                                      && KnownCameraControllers.Any(name =>
                                          plugin.InternalName.Equals(name, StringComparison.OrdinalIgnoreCase)
                                          || plugin.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            ?.Name;
    }

    private bool IsSpecialCameraState()
        => clientState.IsGPosing
           || condition.Any(
               ConditionFlag.BetweenAreas,
               ConditionFlag.BetweenAreas51,
               ConditionFlag.WatchingCutscene,
               ConditionFlag.WatchingCutscene78,
               ConditionFlag.OccupiedInCutSceneEvent);

    private string GetSpecialStateReason()
        => clientState.IsGPosing
            ? "Paused in GPose"
            : condition.Any(ConditionFlag.BetweenAreas, ConditionFlag.BetweenAreas51)
                ? "Paused during territory transition"
                : "Paused during a cutscene";

    private void AbandonOwnership()
    {
        ownedCameraAddress = nint.Zero;
        originalMaximum = 0f;
        appliedMaximum = 0f;
        IsActive = false;
    }

    private static bool IsSaneMaximum(float value)
        => float.IsFinite(value) && value >= 1f && value <= 200f;

    private static bool NearlyEqual(float left, float right) => Math.Abs(left - right) < 0.01f;

    [StructLayout(LayoutKind.Explicit)]
    private struct WorldCameraZoomState
    {
        [FieldOffset(0x124)] public float CurrentZoom;
        [FieldOffset(0x12C)] public float MaximumZoom;
        [FieldOffset(0x180)] public int Mode;
        [FieldOffset(0x18C)] public float InterpolatedZoom;
    }
}
