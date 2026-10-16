# Quest Convenience ownership — HUD 0.8.4.7 / Hub 0.3

The user reported Anti-AFK PASS on 2026-10-16 America/Toronto for Hub Active/HUD Managed, returning to the saved HUD setting, Hub reload and idle beyond the configured automatic-away interval. Anti-AFK is unchanged here; unreported scenarios are not newly certified.

HUD retains its Quest Convenience engine, schema and three saved preferences. Public primitive v1 IPC: SentinelHUD.Quest.ProtocolVersion (int=1), Preference (int: dialogue bit 0, cutscenes bit 1, reward mode bits 2–3), TryClaim(string)->bool and Release(string)->bool. Hub publishes SentinelHub.Quest.Enabled (bool). The existing lease implementation is reused as an independent instance, without altering Anti-AFK's lease.

Hub 0.3 requests ownership; HUD synchronously clears pending confirmations/reward selection before acknowledging. A per-update request guard covers initial/hot-reload order. Missing/old Hub keeps existing behavior; unknown/new Hub state yields. Unrenewed claims expire after 3 seconds, stale tokens cannot release another owner. All native work remains synchronous; disable/unload cannot leave a delayed continuation.

The accepted native dialogue/cutscene/reward algorithm is unchanged apart from the external-owner gate and clearing/status wrapper. No generic Yes/No confirmation, camera, visual or navigation changes. Hub imports only the packed public preferences on an explicit user click. No private configuration reading or rewriting. HUD cleanup remains separate after replacement acceptance.

See Hub docs/QUEST_CONVENIENCE_MIGRATION.md for the exact supervised gate. Disable Hub's Quest Convenience or unload Hub to restore retained HUD preferences.
