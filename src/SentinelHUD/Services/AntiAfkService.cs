using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace SentinelHUD.Services;

/// <summary>
/// Prevents inactivity timers from reaching their configured limits. It does not synthesize input,
/// move the character, send chat, or alter controller/keyboard state. Disabling simply stops resets,
/// after which the client resumes ordinary timer accumulation.
/// </summary>
public sealed unsafe class AntiAfkService
{
    private const int ResetIntervalMilliseconds = 10_000;
    private const float ResetThresholdSeconds = 30f;
    private long nextResetTick;

    public bool IsActive { get; private set; }
    public string State { get; private set; } = "Off";
    public long ResetCount { get; private set; }

    public void Update(bool enabled, bool isLoggedIn)
    {
        if (!enabled)
        {
            IsActive = false;
            State = "Off";
            nextResetTick = 0;
            return;
        }
        if (!isLoggedIn)
        {
            IsActive = false;
            State = "Waiting for login";
            nextResetTick = 0;
            return;
        }

        var now = Environment.TickCount64;
        if (now < nextResetTick)
            return;
        nextResetTick = now + ResetIntervalMilliseconds;

        var timers = InputTimerModule.Instance();
        if (timers is null)
        {
            IsActive = false;
            State = "Client inactivity timer module is unavailable";
            return;
        }

        if (timers->AfkTimer > ResetThresholdSeconds)
            timers->AfkTimer = 0f;
        if (timers->ContentInputTimer > ResetThresholdSeconds)
            timers->ContentInputTimer = 0f;
        if (timers->InputTimer > ResetThresholdSeconds)
            timers->InputTimer = 0f;
        ResetCount++;
        IsActive = true;
        State = $"Active; inactivity timers reset safely ({ResetCount} cycle(s))";
    }

    public void Disable()
    {
        IsActive = false;
        State = "Off";
        nextResetTick = 0;
    }
}
