namespace SentinelHUD.Core;

/// <summary>
/// Keeps only Sentinel's actor-input overlays dormant while a native context menu is becoming visible.
/// Visual HUD modules continue drawing, and input suspension ends immediately when the menu closes.
/// </summary>
public sealed class ContextMenuSuppressionPolicy(long openGraceMilliseconds = 2_000)
{
    private readonly long openGraceMilliseconds = Math.Max(0, openGraceMilliseconds);
    private bool openRequested;
    private bool nativeMenuObserved;
    private long graceDeadline;

    public void NotifyOpenRequested(long nowMilliseconds)
    {
        openRequested = true;
        nativeMenuObserved = false;
        graceDeadline = nowMilliseconds + openGraceMilliseconds;
    }

    public bool Update(bool nativeMenuVisible, long nowMilliseconds)
    {
        // Give every native ContextMenu visual/input priority, even when it was opened by stock UI.
        if (nativeMenuVisible)
        {
            if (openRequested)
                nativeMenuObserved = true;
            return true;
        }

        if (!openRequested)
            return false;

        if (nativeMenuObserved || nowMilliseconds >= graceDeadline)
        {
            Reset();
            return false;
        }

        // FFXIV normally creates the addon on the next frame. Cover that short transition too.
        return true;
    }

    public void Reset()
    {
        openRequested = false;
        nativeMenuObserved = false;
        graceDeadline = 0;
    }
}
