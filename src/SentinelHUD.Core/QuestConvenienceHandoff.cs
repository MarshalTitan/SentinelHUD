namespace SentinelHUD.Core;
/// <summary>Independent Quest Convenience wire contract; Anti-AFK ownership is unaffected.</summary>
public static class QuestConvenienceHandoff
{
    public static int Encode(bool dialogue, bool cutscenes, int rewardMode) =>
        (dialogue ? 1 : 0) | (cutscenes ? 2 : 0) | ((rewardMode is >= 0 and <= 3 ? rewardMode : 0) << 2);
    public static bool ShouldYield(bool leaseActive, bool hubLoaded, string? hubVersion, bool? requested)
    {
        if (leaseActive) return true;
        if (!hubLoaded) return false;
        if (Version.TryParse(hubVersion, out var version) && version < new Version(0,3,0,0)) return false;
        return requested != false;
    }
}
