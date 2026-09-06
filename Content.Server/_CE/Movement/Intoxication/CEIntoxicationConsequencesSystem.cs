using Content.Shared._CE.Examine;
using Content.Shared.Drunk;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Server._CE.Movement.Intoxication;

/// <summary>
/// Connects opted-in entities to existing drowsiness/sleep and CE examine presentation.
/// Vanilla remains the owner of alcohol metabolism, drunkenness and forced sleeping.
/// </summary>
public sealed partial class CEIntoxicationConsequencesSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectAppliedEvent>(OnDrunkApplied);
        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectEndTimeUpdatedEvent>(OnDrunkEndTimeUpdated);
        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectRemovedEvent>(OnDrunkRemoved);
        SubscribeLocalEvent<CEIntoxicationConsequencesComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CESynchronizeIntoxicationEvent>(OnSynchronize);
        SubscribeLocalEvent<CEExamineAugmentEvent>(OnExamine);
    }

    private void OnDrunkApplied(Entity<DrunkStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        QueueSynchronization(entity, args.Target);
    }

    private void OnDrunkEndTimeUpdated(
        Entity<DrunkStatusEffectComponent> entity,
        ref StatusEffectEndTimeUpdatedEvent args)
    {
        // The first end-time update precedes application. Delayed drunkenness
        // must not create its companion before the canonical status applies.
        if (!TryComp<StatusEffectComponent>(entity, out var effect) || !effect.Applied)
            return;

        QueueSynchronization(entity, args.Target);
    }

    private void OnDrunkRemoved(Entity<DrunkStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        QueueSynchronization(entity, args.Target);
    }

    private void OnShutdown(Entity<CEIntoxicationConsequencesComponent> entity, ref ComponentShutdown args)
    {
        _statusEffects.TryRemoveStatusEffect(entity, entity.Comp.DrowsinessStatusEffect);
    }

    private void QueueSynchronization(EntityUid effect, EntityUid target)
    {
        if (MetaData(effect).EntityPrototype?.ID != SharedDrunkSystem.Drunk.Id ||
            !HasComp<CEIntoxicationConsequencesComponent>(target))
        {
            return;
        }

        // Applied events can run inside the canonical status-component enumeration.
        // Synchronize at the end of the tick, with at most one tick of companion lag.
        QueueLocalEvent(new CESynchronizeIntoxicationEvent(target));
    }

    private void OnSynchronize(CESynchronizeIntoxicationEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || EntityManager.IsQueuedForDeletion(args.Target) ||
            !TryComp<CEIntoxicationConsequencesComponent>(args.Target, out var behavior) ||
            behavior.LifeStage != ComponentLifeStage.Running)
            return;

        // Queue processing precedes entity deletion. A queued removal must not
        // resurrect a companion from the status still present in its container.
        if (!_statusEffects.TryGetStatusEffect(args.Target, SharedDrunkSystem.Drunk, out var drunk) ||
            EntityManager.IsQueuedForDeletion(drunk.Value) ||
            !TryComp<StatusEffectComponent>(drunk.Value, out var status) ||
            status.LifeStage != ComponentLifeStage.Running || !status.Applied ||
            status.EndEffectTime <= _timing.CurTime)
        {
            _statusEffects.TryRemoveStatusEffect(args.Target, behavior.DrowsinessStatusEffect);
            return;
        }

        _statusEffects.TrySetStatusEffectDuration(
            args.Target,
            behavior.DrowsinessStatusEffect,
            status.EndEffectTime - _timing.CurTime);
    }

    private void OnExamine(CEExamineAugmentEvent args)
    {
        if (!TryComp<CEIntoxicationConsequencesComponent>(args.Examined, out var behavior) ||
            !_statusEffects.HasStatusEffect(args.Examined, SharedDrunkSystem.Drunk))
        {
            return;
        }

        args.AddMarkup(Loc.GetString(behavior.ExamineMessage));
    }

    private sealed class CESynchronizeIntoxicationEvent(EntityUid target) : EntityEventArgs
    {
        public readonly EntityUid Target = target;
    }
}
