namespace SentinelHUD.Core;
/// <summary>Optional Hub handoff; no config changes, game access or required Hub dependency.</summary>
public sealed class AntiAfkLease
{
    public const long DurationMilliseconds = 3000;
    private string? owner;
    private long expires;
    public bool IsClaimed(long now) { Expire(now); return owner is not null; }
    public bool TryClaim(string token, long now, bool hubRequested)
    {
        Expire(now);
        if (!hubRequested || !Guid.TryParseExact(token, "N", out _) || owner is not null && owner != token) return false;
        owner = token; expires = now + DurationMilliseconds; return true;
    }
    public bool Release(string token)
    {
        if (owner != token) return false;
        owner = null; expires = 0; return true;
    }
    public void Clear() { owner = null; expires = 0; }
    private void Expire(long now) { if (owner is not null && now >= expires) Clear(); }
    public static bool ShouldYield(bool leaseActive, bool hubLoaded, string? hubVersion, bool? requested)
    {
        if (leaseActive) return true;
        if (!hubLoaded) return false;
        // Old Hub 0.1 has no engine. Unknown/new Hub with unavailable status fails closed.
        if (Version.TryParse(hubVersion, out var version) && version < new Version(0, 2, 0, 0)) return false;
        return requested != false;
    }
}
