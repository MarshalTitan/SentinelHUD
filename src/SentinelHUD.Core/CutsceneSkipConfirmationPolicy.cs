namespace SentinelHUD.Core;

public enum CutsceneSkipConfirmationDecision
{
    Wait,
    Confirm,
    Dismissed,
    TimedOut,
}

public readonly record struct CutsceneSkipConfirmationState(
    bool Pending,
    bool PromptObserved,
    long DeadlineTick)
{
    public static CutsceneSkipConfirmationState Begin(long now, long timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

        return new CutsceneSkipConfirmationState(true, false, now + timeoutMilliseconds);
    }
}

public static class CutsceneSkipConfirmationPolicy
{
    public static CutsceneSkipConfirmationDecision Evaluate(
        ref CutsceneSkipConfirmationState state,
        long now,
        bool promptVisible,
        bool confirmationAvailable)
    {
        if (!state.Pending)
            return CutsceneSkipConfirmationDecision.Wait;

        if (now >= state.DeadlineTick)
            return CutsceneSkipConfirmationDecision.TimedOut;

        if (promptVisible)
        {
            state = state with { PromptObserved = true };
            return confirmationAvailable
                ? CutsceneSkipConfirmationDecision.Confirm
                : CutsceneSkipConfirmationDecision.Wait;
        }

        if (state.PromptObserved)
            return CutsceneSkipConfirmationDecision.Dismissed;

        return CutsceneSkipConfirmationDecision.Wait;
    }
}
