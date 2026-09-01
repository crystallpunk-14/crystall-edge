using Content.Server.Drunk;
using Content.Shared._CE.Examine;
using Content.Shared.Drunk;
using Content.Shared.StatusEffectNew;

namespace Content.Server._CE.Movement.Intoxication;

/// <summary>
/// Connects opted-in entities to existing drowsiness/sleep and CE examine presentation.
/// Vanilla remains the owner of alcohol metabolism, drunkenness and forced sleeping.
/// </summary>
public sealed partial class CEIntoxicationConsequencesSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEIntoxicationConsequencesComponent, SharedDrunkSystem.DrunkEvent>(
            OnDrunk,
            after: new[] { typeof(DrunkSystem) });
        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectRemovedEvent>(OnDrunkRemoved);
        SubscribeLocalEvent<CEExamineAugmentEvent>(OnExamine);
    }

    private void OnDrunk(
        Entity<CEIntoxicationConsequencesComponent> entity,
        ref SharedDrunkSystem.DrunkEvent args)
    {
        if (args.Duration <= TimeSpan.Zero)
            return;

        _statusEffects.TryAddStatusEffectDuration(
            entity.Owner,
            entity.Comp.DrowsinessStatusEffect,
            args.Duration);
    }

    private void OnDrunkRemoved(Entity<DrunkStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if (MetaData(entity).EntityPrototype?.ID != SharedDrunkSystem.Drunk.Id ||
            !TryComp<CEIntoxicationConsequencesComponent>(args.Target, out var behavior))
        {
            return;
        }

        _statusEffects.TryRemoveStatusEffect(args.Target, behavior.DrowsinessStatusEffect);
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
}
