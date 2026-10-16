# Optional Anti-AFK handoff — HUD 0.8.4.6 / Hub 0.2

HUD retains its engine, checkbox, schema and saved preference. No cleanup/removal is performed before supervised Hub acceptance.

Public primitive IPC: SentinelHUD.AntiAfk.ProtocolVersion (int=1), Preference (bool), TryClaim(string session token)->bool and Release(string token)->bool. A valid 32-digit GUID claim from a loaded requesting Hub expires after 3 seconds without renewal. Claims suspend HUD synchronously. Mismatched/stale tokens cannot steal or release another owner.

Before its timer update HUD also observes SentinelHub.AntiAfk.Enabled. A loaded compatible Hub requesting ownership suspends HUD before the initial claim, including HUD hot reload. Unknown/new Hub status fails closed; absent Hub and known old 0.1 Hub preserve normal legacy operation. Explicit disable/release or unload returns the retained preference after a bounded lease if necessary.

Hub must block when legacy HUD is installed and require v1 claim acknowledgment when compatible HUD is loaded. Native writes are synchronous and permitted only after current ownership. No preference is silently copied or enabled.

Tests cover contention, renewal, expiry, stale release, old/absent peers, unknown peers, initial/hot-reload guard and unchanged config. Live native timing, plugin unload/reload and handoff remain supervised.
