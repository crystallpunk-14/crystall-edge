using Content.Shared._CE.Knowledge.Components;
using Content.Shared._CE.Knowledge.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Knowledge;

/// <summary>
/// Manages what characters know (workbench recipes, infusion altar recipes, science
/// achievements, ...) and the shared "read a book to learn / write a book with a pen" flow.
/// Fully shared and predicted: mutations are simple, idempotent set operations, so the client's
/// speculative copy self-corrects on the next authoritative state sync, same as any other
/// predicted component.
/// </summary>
public sealed partial class CEKnowledgeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeRead();
        InitializePen();
    }

    /// <summary>
    /// Checks whether the entity knows a specific piece of knowledge.
    /// Returns false if the entity has no <see cref="CEKnowledgeComponent"/> (no knowledge = no access).
    /// </summary>
    public bool Knows(EntityUid target,
        ProtoId<CEKnowledgePrototype> knowledge,
        CEKnowledgeComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return false;

        return component.Known.Contains(knowledge);
    }

    /// <summary>
    /// Teaches the entity a piece of knowledge, networking the change and raising
    /// <see cref="CEKnowledgeLearnedEvent"/>. Returns false if it was already known.
    /// </summary>
    public bool TryLearn(EntityUid target,
        ProtoId<CEKnowledgePrototype> knowledge,
        CEKnowledgeComponent? component = null)
    {
        component ??= EnsureComp<CEKnowledgeComponent>(target);

        if (!component.Known.Add(knowledge))
            return false;

        Dirty(target, component);

        var ev = new CEKnowledgeLearnedEvent(target, knowledge);
        EntityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);

        return true;
    }

    /// <summary>
    /// Removes a piece of knowledge from the entity. Returns false if it wasn't known or the
    /// entity has no <see cref="CEKnowledgeComponent"/>.
    /// </summary>
    public bool TryForget(EntityUid target,
        ProtoId<CEKnowledgePrototype> knowledge,
        CEKnowledgeComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return false;

        if (!component.Known.Remove(knowledge))
            return false;

        Dirty(target, component);
        return true;
    }

    /// <summary>
    /// Returns everything known by the entity, or null if it has no <see cref="CEKnowledgeComponent"/>.
    /// </summary>
    public HashSet<ProtoId<CEKnowledgePrototype>>? GetKnown(EntityUid target,
        CEKnowledgeComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return null;

        return component.Known;
    }
}
