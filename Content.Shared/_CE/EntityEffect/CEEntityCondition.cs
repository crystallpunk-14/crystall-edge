using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect;

/// <summary>
/// Abstract base for CE entity conditions — boolean checks on an entity.
/// Logic lives in systems subscribing to <see cref="CEEntityConditionEvent{T}"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEEntityCondition
{
    [DataField]
    public bool Inverted;

    [DataField]
    public CEEffectTarget ConditionTarget = CEEffectTarget.Target;

    /// <summary>
    /// Resolves the target entity from args, dispatches the check, and applies <see cref="Inverted"/>.
    /// Without an entity, a condition that <see cref="SupportsPosition"/> checks the targeted position instead;
    /// otherwise returns false (or true if Inverted).
    /// </summary>
    public bool Passes(CEEntityEffectArgs args)
    {
        bool result;
        if (args.Resolve(ConditionTarget) is { } entity)
            result = Check(entity, args);
        else if (SupportsPosition && ConditionTarget == CEEffectTarget.Target && args.Position is { } position)
            result = CheckPosition(position, args);
        else
            return Inverted;

        return Inverted ? !result : result;
    }

    protected abstract bool Check(EntityUid entity, CEEntityEffectArgs args);

    /// <summary>
    /// Whether this condition can check a bare position, e.g. the point of a world-targeted action.
    /// </summary>
    protected virtual bool SupportsPosition => false;

    protected virtual bool CheckPosition(EntityCoordinates position, CEEntityEffectArgs args) => false;

    /// <summary>
    /// Human-readable explanation of what this condition requires, for UI.
    /// </summary>
    public virtual string GetDescription(IEntityManager entityManager, IPrototypeManager prototype) => string.Empty;
}

/// <summary>
/// Generic base that provides automatic event dispatch for concrete condition types.
/// Each concrete condition should inherit from this instead of <see cref="CEEntityCondition"/> directly.
/// </summary>
public abstract partial class CEEntityConditionBase<T> : CEEntityCondition where T : CEEntityConditionBase<T>
{
    protected override bool Check(EntityUid entity, CEEntityEffectArgs args)
    {
        if (this is not T typed)
            return false;

        var ev = new CEEntityConditionEvent<T>(typed, entity, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
        return ev.Result;
    }
}

/// <summary>
/// Broadcast event raised when a CE entity condition is evaluated.
/// The handling system sets <see cref="Result"/> to indicate whether the condition passes.
/// </summary>
[ByRefEvent]
public record struct CEEntityConditionEvent<T>(T Condition, EntityUid Entity, CEEntityEffectArgs Args)
    where T : CEEntityConditionBase<T>
{
    public bool Result;
}

/// <summary>
/// Abstract base system for handling CE entity conditions.
/// Concrete systems inherit this and implement <see cref="Condition"/>.
/// </summary>
public abstract partial class CEEntityConditionSystem<TCondition> : EntitySystem
    where TCondition : CEEntityConditionBase<TCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEEntityConditionEvent<TCondition>>(OnCondition);
    }

    private void OnCondition(ref CEEntityConditionEvent<TCondition> args)
    {
        Condition(ref args);
    }

    protected abstract void Condition(ref CEEntityConditionEvent<TCondition> args);
}
