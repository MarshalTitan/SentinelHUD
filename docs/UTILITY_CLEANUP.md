# Utility UI cleanup — HUD 0.8.5.2

Both users have installed Hub and copied their utility preferences (user confirmation 2026-10-17).
Remove the retired Questing / Convenience navigation in Modern Systems and Classic tabs,
its read-only migration page and the old saved-import diagnostic text. Systems retains Camera.
The hidden legacy content enum remains compatible and falls back to Camera.

No native utility engines are reintroduced or modified. Retain serialized Convenience fields,
schema 12 and compatibility import/claim IPC for old consumers and rollback. Existing Hub
Anti-AFK, dialogue, cutscene and reward settings, HUD layouts/camera and Core 0.3.1 stay unchanged.

Update HUD normally. Open /shud in each theme: no Convenience page; Camera, visuals and
saved layout remain available. No new gameplay test is needed for a navigation/text cleanup.
If an interface is missing, return version/theme/screenshot; rollback to HUD 0.8.5.1 with
settings retained. Never restore pre-cleanup native utility engines alongside Hub.
