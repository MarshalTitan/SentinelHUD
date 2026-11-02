# Historical PvP observations — IPC v1

Sentinel HUD 0.8.9.0 retains a bounded, RAM-only history of the existing Targeting Me Counter observations. The counter's configuration, classification, scan cadence (125 ms), display and ownership are unchanged. There is no second object-table scan or new combat hook.

The tracker records only target-set, job/name or classification changes. Unchanged observations update one freshness timestamp. It keeps at most 256 changes for 30 seconds plus a boundary baseline. A death seals the last available live sample **before** Current becomes inactive. Respawn starts a new life; logout, territory changes, disabling the HUD/counter and visibility exclusions reset the history. At most four completed death snapshots remain in RAM. Nothing is written to disk by HUD.

## Contract

- `SentinelHUD.Deaths.v1.Version`: `Func<int>`, returns 1.
- `SentinelHUD.Deaths.v1.Snapshot`: `Func<string>`, immutable JSON, rebuilt only on death/reset. Returns `{ Version: 1, Session: Guid, Deaths: [...] }`.
- `SentinelHUD.Deaths.v1.Changed`: notification `Action<long>`. Subscribers enqueue framework-thread reads; they must tolerate unload/reload and unavailable/incompatible endpoints.

Each death has `Id`, `Territory`, transient `LocalEntityId` for same-character matching, `At`, `LastSampleAt`, `WindowStart`, `Changes`, `Mode`. Each change has `At` and `Targeters`; each targeter has an opaque per-life integer `Token`, `Name`, `JobId`, `Job`, and `Classification`. Tokens are not entity IDs. No enemy entity/object/content IDs, positions or distance are exported. Hub discards the transient local identifier before persistence. Subscriber reads must run on the Dalamud framework thread. Arrays are serialized into strings, so consumers cannot mutate tracker state.

This endpoint exposes completed deaths only. It does not expose the rolling live buffer, identify damage contributors, recommend targets, provide hidden observations, or change any combat/movement system. A hard target is an observation, not proof of an attack.

## Verification

Core fixtures cover change compression, unordered identical sets, focus/job changes, zero targeters, final pre-clear snapshot, one recap per life, multiple deaths, respawn/territory/session reset, 30-second/256-change/four-death bounds, JSON privacy and round-trip, and 10,000 unchanged observations with zero allocations in the added history code. The existing HUD suites also run in CI.

Required in-game acceptance: with the counter enabled in Frontline, die after visible target changes; compare final sample time/jobs to the visible pre-death counter and verify Hub retains the recap after respawn. Repeat two deaths, change territories, disable/reload each plugin, and confirm stale observations never become current. Test with unavailable classification and vanished enemy objects. Native game observations are not proven by synthetic fixtures.

The official Dalamud repository restrictions prohibit PvP plugins; this contract is distributed through the existing Sentinel custom repository, without a claim of official repository approval. See https://dalamud.dev/plugin-publishing/restrictions/ .
