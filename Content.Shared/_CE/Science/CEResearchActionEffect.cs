using Content.Shared._CE.Science.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science;

/// <summary>
/// Data-only base class for the effect a <see cref="CEResearchActionPrototype"/> performs once
/// validated. Logic is handled by systems subscribing to <see cref="CEResearchActionEffectEvent{T}"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEResearchActionEffect
{
    public abstract void Effect(CEResearchActionEffectArgs args);
}

/// <summary>
/// Generic base that provides automatic event dispatch for concrete action effect types.
/// Each concrete effect should inherit from this instead of <see cref="CEResearchActionEffect"/> directly.
/// </summary>
public abstract partial class CEResearchActionEffectBase<T> : CEResearchActionEffect where T : CEResearchActionEffectBase<T>
{
    public override void Effect(CEResearchActionEffectArgs args)
    {
        if (this is not T typed)
            return;

        var ev = new CEResearchActionEffectEvent<T>(typed, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
    }
}

/// <summary>
/// Context passed to a research action's effects once the action has been validated server-side.
/// </summary>
public record struct CEResearchActionEffectArgs(
    IEntityManager EntityManager,
    EntityUid Table,
    EntityUid Actor,
    ProtoId<CEScienceAreaPrototype> Area,
    Vector2i Coordinate);

[ByRefEvent]
public record struct CEResearchActionEffectEvent<T>(T Effect, CEResearchActionEffectArgs Args)
    where T : CEResearchActionEffectBase<T>;

/// <summary>
/// Abstract base system for handling research action effects.
/// Concrete systems inherit this and implement <see cref="Effect"/>.
/// </summary>
public abstract partial class CEResearchActionEffectSystem<TEffect> : EntitySystem where TEffect : CEResearchActionEffectBase<TEffect>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEResearchActionEffectEvent<TEffect>>(OnEffect);
    }

    private void OnEffect(ref CEResearchActionEffectEvent<TEffect> args)
    {
        Effect(ref args);
    }

    protected abstract void Effect(ref CEResearchActionEffectEvent<TEffect> args);
}
