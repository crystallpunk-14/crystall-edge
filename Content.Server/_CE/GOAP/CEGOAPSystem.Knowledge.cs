using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Robust.Shared.Map;

namespace Content.Server._CE.GOAP;

/// <summary>
/// Partial: knowledge store API. Perceptors call <see cref="Remember"/>;
/// the orchestrator drives expiration via <see cref="PurgeExpiredKnowledge"/> and flushes
/// changes via <see cref="FlushKnowledgeUpdate"/>.
/// </summary>
public sealed partial class CEGOAPSystem
{
    /// <summary>
    /// Adds or refreshes a knowledge entry. Marks knowledge dirty only when a new entity is added:
    /// a position change alone doesn't alter the known set.
    /// </summary>
    public void Remember(
        Entity<CEGOAPComponent?> ent,
        EntityUid target,
        EntityCoordinates coords)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        var now = _timing.CurTime;
        var expires = now + ent.Comp.MemoryDuration;
        var added = !ent.Comp.Knowledge.ContainsKey(target);

        ent.Comp.Knowledge[target] = new CEGOAPKnowledgeEntry
        {
            LastSeenCoords = coords,
            LastSeenTime = now,
            ExpiresAt = expires,
        };

        if (added)
            ent.Comp.KnowledgeDirty = true;
    }

    /// <summary>
    /// Removes a knowledge entry, marking knowledge dirty if anything was removed.
    /// </summary>
    public bool Forget(Entity<CEGOAPComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.Knowledge.Remove(target))
            return false;

        ent.Comp.KnowledgeDirty = true;
        return true;
    }

    /// <summary>
    /// Drops entries whose ExpiresAt has elapsed or whose target entity no longer exists.
    /// Called by the GOAP orchestrator each agent tick.
    /// </summary>
    public void PurgeExpiredKnowledge(Entity<CEGOAPComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Knowledge.Count == 0)
            return;

        var now = _timing.CurTime;
        List<EntityUid>? dead = null;

        foreach (var (target, entry) in ent.Comp.Knowledge)
        {
            if (entry.ExpiresAt > now && Exists(target) && !Terminating(target))
                continue;

            dead ??= new List<EntityUid>();
            dead.Add(target);
        }

        if (dead == null)
            return;

        foreach (var uid in dead)
        {
            ent.Comp.Knowledge.Remove(uid);
        }

        ent.Comp.KnowledgeDirty = true;
    }

    /// <summary>
    /// Raises <see cref="CEGOAPKnowledgeUpdatedEvent"/> once if knowledge changed since the last call,
    /// so any number of Remember/Forget calls within a tick cost a single sensor re-evaluation.
    /// Called by the GOAP orchestrator each agent tick.
    /// </summary>
    private void FlushKnowledgeUpdate(Entity<CEGOAPComponent> ent)
    {
        if (!ent.Comp.KnowledgeDirty)
            return;

        ent.Comp.KnowledgeDirty = false;
        var ev = new CEGOAPKnowledgeUpdatedEvent();
        RaiseLocalEvent(ent, ref ev);
    }
}

/// <summary>
/// Raised on a GOAP entity at most once per agent tick when its <see cref="CEGOAPComponent.Knowledge"/>
/// set changed (entries added or removed). Sensors and selectors that depend
/// on knowledge should listen to this event instead of polling.
/// </summary>
[ByRefEvent]
public readonly record struct CEGOAPKnowledgeUpdatedEvent;
