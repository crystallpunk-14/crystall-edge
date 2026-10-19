using Content.Shared._CE.GOAP.Components;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared._CE.GOAP.Sensors;
using Robust.Shared.Analyzers;

namespace Content.Server._CE.GOAP.Sensors;

/// <summary>
/// Base for sensor components: holds the entries of one sensor type on an agent. The component
/// also serves as the index event-driven sensors use to find which agents to re-evaluate.
/// </summary>
public abstract partial class CEGOAPSensorComponent<TEntry> : Component
    where TEntry : CEGOAPSensorEntryBase
{
    [DataField]
    [AlwaysPushInheritance]
    public List<TEntry> Entries = new();
}

/// <summary>
/// Base for sensor entries bound to their sensor component, so behavior packages can attach them.
/// </summary>
public abstract partial class CEGOAPSensorEntry<TSelf, TComp> : CEGOAPSensorEntryBase
    where TSelf : CEGOAPSensorEntry<TSelf, TComp>
    where TComp : CEGOAPSensorComponent<TSelf>, new()
{
    public override void AddTo(EntityUid uid, IEntityManager entMan)
    {
        entMan.EnsureComponent<TComp>(uid).Entries.Add((TSelf) this);
    }
}

/// <summary>
/// Base system for GOAP sensors. Re-evaluates all entries on sensor refresh and whenever the agent's
/// knowledge changes (target slots resolve from knowledge), resolves each entry's target slot and
/// writes the result to the agent's world state. Concrete sensors implement <see cref="Evaluate"/>
/// and add their own triggers by calling <see cref="EvaluateAll(Entity{TComp})"/>.
/// </summary>
public abstract partial class CEGOAPSensorSystem<TComp, TEntry> : EntitySystem
    where TComp : CEGOAPSensorComponent<TEntry>
    where TEntry : CEGOAPSensorEntryBase
{
    [Dependency] protected CEGOAPSystem Goap = default!;

    [Dependency] private EntityQuery<CEGOAPComponent> _goapQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TComp, CEGOAPSensorRefreshEvent>(OnRefresh);
        SubscribeLocalEvent<TComp, CEGOAPKnowledgeUpdatedEvent>(OnKnowledgeUpdated);
    }

    private void OnRefresh(Entity<TComp> ent, ref CEGOAPSensorRefreshEvent args)
    {
        EvaluateAll(ent);
    }

    private void OnKnowledgeUpdated(Entity<TComp> ent, ref CEGOAPKnowledgeUpdatedEvent args)
    {
        EvaluateAll(ent);
    }

    /// <summary>
    /// Evaluates every entry of the sensor and writes the results to the agent's world state.
    /// </summary>
    protected void EvaluateAll(Entity<TComp> ent)
    {
        if (_goapQuery.TryComp(ent, out var goap))
            EvaluateAll(ent, goap);
    }

    /// <inheritdoc cref="EvaluateAll(Entity{TComp})"/>
    protected void EvaluateAll(Entity<TComp> ent, CEGOAPComponent goap)
    {
        foreach (var entry in ent.Comp.Entries)
        {
            var target = Goap.ResolveTarget(ent, entry.Target);
            goap.WorldState[entry.ConditionKey] = Evaluate(ent, entry, target);
        }
    }

    /// <summary>
    /// Computes the condition value of one entry. <paramref name="target"/> is the entry's resolved
    /// target slot and may be empty.
    /// </summary>
    protected abstract bool Evaluate(EntityUid agent, TEntry entry, CEGOAPSelectorResult target);
}
