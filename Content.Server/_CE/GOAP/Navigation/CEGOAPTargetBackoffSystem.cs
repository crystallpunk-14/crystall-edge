using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Navigation;

/// <summary>
/// Opt-in policy and runtime state for temporarily excluding failed GOAP targets.
/// Selection and movement systems remain responsible for their own domain behavior.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPTargetBackoffComponent : Component
{
    [DataField(required: true)]
    public TimeSpan Duration;

    /// <summary>
    /// Hard bound for per-agent target history. Oldest deadlines are evicted first.
    /// </summary>
    [DataField]
    public int MaxEntries = 32;

    public readonly Dictionary<EntityUid, TimeSpan> UntilByTarget = new();
}

/// <summary>
/// Owns bounded per-agent target exclusions without taking ownership of target
/// selection, movement, or pathfinding.
/// </summary>
public sealed partial class CEGOAPTargetBackoffSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public void Prune(EntityUid agent)
    {
        if (!TryComp<CEGOAPTargetBackoffComponent>(agent, out var backoff))
            return;

        Prune(backoff);
    }

    public bool IsRejected(EntityUid agent, EntityUid target)
    {
        if (!TryComp<CEGOAPTargetBackoffComponent>(agent, out var backoff) ||
            !backoff.UntilByTarget.TryGetValue(target, out var until))
            return false;

        if (Exists(target) && _timing.CurTime < until)
            return true;

        backoff.UntilByTarget.Remove(target);
        return false;
    }

    public bool Reject(EntityUid agent, EntityUid target)
    {
        if (!Exists(target) ||
            !TryComp<CEGOAPTargetBackoffComponent>(agent, out var backoff) ||
            backoff.Duration <= TimeSpan.Zero ||
            backoff.MaxEntries <= 0)
            return false;

        Prune(backoff);

        var until = _timing.CurTime + backoff.Duration;
        if (backoff.UntilByTarget.TryGetValue(target, out var current))
        {
            if (current < until)
                backoff.UntilByTarget[target] = until;

            return true;
        }

        while (backoff.UntilByTarget.Count >= backoff.MaxEntries)
            RemoveEarliest(backoff);

        backoff.UntilByTarget.Add(target, until);
        return true;
    }

    private void Prune(CEGOAPTargetBackoffComponent backoff)
    {
        while (TryFindStale(backoff, out var stale))
            backoff.UntilByTarget.Remove(stale);
    }

    private bool TryFindStale(CEGOAPTargetBackoffComponent backoff, out EntityUid stale)
    {
        var now = _timing.CurTime;
        foreach (var (target, until) in backoff.UntilByTarget)
        {
            if (Exists(target) && now < until)
                continue;

            stale = target;
            return true;
        }

        stale = default;
        return false;
    }

    private static void RemoveEarliest(CEGOAPTargetBackoffComponent backoff)
    {
        EntityUid? earliestTarget = null;
        TimeSpan? earliestUntil = null;
        foreach (var (target, until) in backoff.UntilByTarget)
        {
            if (earliestUntil is { } currentUntil &&
                (until > currentUntil ||
                 until == currentUntil && earliestTarget is { } current && target.Id > current.Id))
                continue;

            earliestTarget = target;
            earliestUntil = until;
        }

        if (earliestTarget is { } earliest)
            backoff.UntilByTarget.Remove(earliest);
    }
}
