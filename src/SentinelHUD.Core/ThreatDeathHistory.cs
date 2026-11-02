namespace SentinelHUD.Core;

public interface IThreatTargeter
{
    ulong GameObjectId { get; }
    string Name { get; }
    uint JobId { get; }
    string JobAbbreviation { get; }
}
public sealed record ThreatActor(int Token, string Name, uint JobId, string Job, string Classification);
public sealed record ThreatFrame(DateTimeOffset At, ThreatActor[] Targeters);
public sealed record ThreatDeath(Guid Id, uint Territory, uint LocalEntityId, DateTimeOffset At,
    DateTimeOffset LastSampleAt, DateTimeOffset WindowStart, ThreatFrame[] Changes, string Mode);
public sealed record ThreatEnvelope(int Version, Guid Session, ThreatDeath[] Deaths);

// Receives the existing counter's results: no object-table access, timers, native hooks or I/O.
public sealed class ThreatDeathHistory
{
    private readonly List<ThreatFrame> changes = new(256);
    private readonly Dictionary<ulong, int> tokens = new(72);
    private readonly List<ThreatDeath> deaths = new(4);
    private readonly Dictionary<ulong, (uint Job, string Name, string Classification)> previous = new(72);
    private DateTimeOffset first, last;
    private bool dead;
    public Guid Session { get; private set; } = Guid.NewGuid();
    public event Action? Changed;
    public IReadOnlyList<ThreatDeath> Deaths => deaths;
    public bool HasSamples => first != default;
    public void Observe<T>(DateTimeOffset now, IReadOnlyList<T> targeters, string classification) where T : IThreatTargeter
    {
        if (dead) { ClearLife(); dead = false; }
        if (first == default) first = now;
        last = now;
        var changed = changes.Count == 0 || targeters.Count != previous.Count;
        for (var i = 0; i < targeters.Count; i++) {
            var t = targeters[i];
            if (!previous.TryGetValue(t.GameObjectId, out var p) || p.Job != t.JobId || p.Name != t.Name || p.Classification != classification) { changed = true; break; }
        }
        if (changed) {
            if (tokens.Count > 512) foreach (var id in tokens.Keys.Where(id => !previous.ContainsKey(id)).ToArray()) tokens.Remove(id);
            previous.Clear();
            var actors = new ThreatActor[Math.Min(72, targeters.Count)];
            for (var i = 0; i < actors.Length; i++) {
                var t = targeters[i];
                if (!tokens.TryGetValue(t.GameObjectId, out var token)) { token = ++nextToken; tokens[t.GameObjectId] = token; }
                actors[i] = new(token, t.Name, t.JobId, t.JobAbbreviation, classification);
                previous[t.GameObjectId] = (t.JobId, t.Name, classification);
            }
            changes.Add(new(now, actors));
        }
        var cutoff = now.AddSeconds(-30);
        // Keep one baseline at the boundary, so long unchanged focus intervals remain represented.
        while (changes.Count > 1 && changes[1].At <= cutoff) changes.RemoveAt(0);
        while (changes.Count > 256) changes.RemoveAt(0);
    }
    public ThreatDeath? Finish(DateTimeOffset now, uint territory, uint localEntityId, string mode)
    {
        if (dead || first == default || last == default) return null;
        dead = true;
        if (now - last > TimeSpan.FromSeconds(30)) return null;
        var cutoff = now.AddSeconds(-30);
        var start = first > cutoff ? first : cutoff;
        var frames = changes.Where(c => c.At <= now).Select((c, i) => i == 0 && c.At < start ? c with { At = start } : c).ToArray();
        var d = new ThreatDeath(Guid.NewGuid(), territory, localEntityId, now, last, start, frames, mode);
        deaths.Insert(0, d); if (deaths.Count > 4) deaths.RemoveAt(4);
        Changed?.Invoke(); return d;
    }
    public void Reset()
    {
        ClearLife(); deaths.Clear(); dead = false; Session = Guid.NewGuid(); Changed?.Invoke();
    }
    private int nextToken;
    private void ClearLife() { changes.Clear(); tokens.Clear(); previous.Clear(); nextToken = 0; first = last = default; }
}
