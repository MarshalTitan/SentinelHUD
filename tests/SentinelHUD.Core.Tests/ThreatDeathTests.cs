using SentinelHUD.Core;
using System.Text.Json;
internal static class ThreatDeathTests
{
    private sealed record Actor(ulong GameObjectId, string Name, uint JobId, string JobAbbreviation) : IThreatTargeter;
    private static void Check(bool value, string why) { if (!value) throw new Exception(why); }
    public static void Run()
    {
        var h = new ThreatDeathHistory(); var now = DateTimeOffset.UtcNow; var notices = 0;
        h.Changed += () => notices++;
        Actor[] a = [new(1, "Observed A", 32, "DRK"), new(2, "Observed B", 22, "DRG")];
        h.Observe(now, a, "BattalionTeam");
        h.Observe(now.AddSeconds(2), a.Reverse().ToArray(), "BattalionTeam");
        for (var i = 0; i < 1000; i++) h.Observe(now.AddSeconds(2), a, "BattalionTeam");
        Measure(h, now.AddSeconds(2), a);
        var delta = Measure(h, now.AddSeconds(2), a);
        Check(delta == 0, "Unchanged HUD samples allocate " + delta + " bytes");
        Actor[] b = [a[0], a[1], new(3, "Observed C", 22, "DRG")];
        h.Observe(now.AddSeconds(4), b, "BattalionTeam");
        h.Observe(now.AddSeconds(6), b[1..], "BattalionTeam");
        var d = h.Finish(now.AddSeconds(6.1), 123, 42, "Frontline")!;
        Check(d.Changes.Length == 3 && d.Changes[0].Targeters.Length == 2 && d.Changes[1].Targeters.Length == 3 && d.Changes[2].Targeters.Length == 2, "Targeting changes lost");
        Check(d.LastSampleAt == now.AddSeconds(6) && notices == 1, "Final sample not preserved before clear");
        Check(h.Finish(now.AddSeconds(6.2), 123, 42, "Frontline") is null, "One death generated duplicate recaps");
        h.Observe(now.AddSeconds(10), Array.Empty<Actor>(), "BattalionTeam");
        var second = h.Finish(now.AddSeconds(11), 123, 42, "Frontline")!;
        Check(second.Id != d.Id && second.Changes.Length == 1 && second.Changes[0].Targeters.Length == 0, "Respawn reused previous targeters");
        for (var i = 0; i < 10; i++) { h.Observe(now.AddSeconds(12 + i), a, "Fallback"); h.Finish(now.AddSeconds(12.1 + i), 123, 42, "Frontline"); }
        Check(h.Deaths.Count == 4, "IPC archive is not bounded");
        h.Reset(); Check(h.Deaths.Count == 0 && !h.HasSamples && h.Finish(now, 123, 42, "Frontline") is null, "Territory/logout reset leaked history");
        var session = h.Session; h.Reset(); Check(h.Session != session, "Session identity not rotated");
        for (var i = 0; i < 1000; i++) h.Observe(now.AddMilliseconds(i * 125), i % 2 == 0 ? a : b, "BattalionTeam");
        var bounded = h.Finish(now.AddSeconds(125), 123, 42, "Frontline")!;
        Check(bounded.Changes.Length <= 256 && bounded.Changes[0].At >= bounded.At.AddSeconds(-30), "Rolling window exceeded 30 seconds");
        var json = JsonSerializer.Serialize(new ThreatEnvelope(1, h.Session, h.Deaths.ToArray()));
        Check(!json.Contains("GameObjectId") && !json.Contains("ContentId") && !json.Contains("Distance"), "IPC exposed unnecessary live actor identifiers");
        var read = JsonSerializer.Deserialize<ThreatEnvelope>(json)!;
        Check(read.Deaths[0].Changes[^1].Targeters.Length == bounded.Changes[^1].Targeters.Length, "Immutable contract round-trip failed");
        Console.WriteLine("PASS HUD death history: change compression, 30s/256 bounds, final snapshot, repeated deaths, respawn/territory reset, IPC privacy and 10,000 unchanged samples with zero allocations.");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static long Measure(ThreatDeathHistory h, DateTimeOffset now, Actor[] a)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++) h.Observe(now, a, "BattalionTeam");
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
