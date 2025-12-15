using Content.Shared._CE.Actions.Spells;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Weapons.MeleeEnergyEffect;

public abstract class CESharedMeleeEnergyEffectSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMeleeEnergyEffectComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CEMeleeEnergyEffectComponent, MeleeHitEvent>(OnMeleeAttack);
        SubscribeLocalEvent<CEMeleeEnergyEffectComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEMeleeEnergyEffectComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (!comp.Active)
                continue;

            if (comp.DeactivateTime == TimeSpan.Zero)
                continue;

            if (Timing.CurTime < comp.DeactivateTime)
                continue;

            SetActiveStatus((ent, comp), false, null);
        }
    }

    protected virtual void OnUseInHand(Entity<CEMeleeEnergyEffectComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Active)
            return;

        if (TryComp<UseDelayComponent>(ent, out var delay))
        {
            if (_useDelay.IsDelayed((ent.Owner, delay)))
                return;

            _useDelay.TryResetDelay((ent.Owner, delay));
        }
    }

    private void OnMeleeAttack(Entity<CEMeleeEnergyEffectComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.Active)
            return;

        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            foreach (var effect in ent.Comp.Effects)
            {
                effect.Effect(EntityManager, new CESpellEffectBaseArgs(args.User, ent, target, Transform(target).Coordinates));
            }
        }

        SetActiveStatus(ent, false, args.User);
    }

    private void OnGetMeleeDamage(Entity<CEMeleeEnergyEffectComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (!ent.Comp.RemoveBaseDamage)
            return;

        if (ent.Comp.Active)
            args.Damage *= 0;
    }

    public void SetActiveStatus(Entity<CEMeleeEnergyEffectComponent> ent, bool active, EntityUid? user)
    {
        ent.Comp.Active = active;
        DirtyField(ent, ent.Comp, nameof(CEMeleeEnergyEffectComponent.Active));

        ent.Comp.DeactivateTime = active
            ? Timing.CurTime + ent.Comp.ActiveDuration
            : TimeSpan.Zero;
        DirtyField(ent, ent.Comp, nameof(CEMeleeEnergyEffectComponent.DeactivateTime));

        Appearance.SetData(ent.Owner, CEMeleeEnergyState.Active, active);
        Appearance.SetData(ent, ToggleableVisuals.Enabled, active);

        Audio.PlayPredicted(active ? ent.Comp.ActivateSound : ent.Comp.DeactivateSound, ent, user);
    }
}
