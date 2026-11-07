using Content.Shared._CE.Animation.Core;
using Content.Shared._CE.Animation.Core.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Applies a status effect to the animated entity for the rest of the current animation.
/// The status is removed together with the animation, including when the animation is cancelled.
/// </summary>
public sealed partial class ApplyAnimationStatusEffect : CEEntityEffectBase<ApplyAnimationStatusEffect>
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var statusName = prototype.TryIndex(StatusEffect, out var statusProto) ? statusProto.Name : StatusEffect.Id;
        return Loc.GetString("ce-entity-effect-guidebook-apply-animation-status", ("status", statusName));
    }
}

public sealed partial class CEApplyAnimationStatusEffectEffectSystem : CEEntityEffectSystem<ApplyAnimationStatusEffect>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private StatusEffectsSystem _statusEffect = default!;

    protected override void Effect(ref CEEntityEffectEvent<ApplyAnimationStatusEffect> args)
    {
        var entity = args.Args.Source;
        if (!TryComp<CEActiveAnimationActionComponent>(entity, out var controller)
            || controller.StartAnimationTime is not { } start
            || !_proto.Resolve(controller.ActiveAnimation, out var animation))
            return;

        var end = start + animation.Duration / controller.AnimationSpeed;
        var remaining = end - _timing.CurTime;
        if (remaining <= TimeSpan.Zero)
            return;

        if (!_statusEffect.TrySetStatusEffectDuration(entity, args.Effect.StatusEffect, remaining))
            return;

        if (controller.LinkedStatusEffects.Contains(args.Effect.StatusEffect))
            return;

        controller.LinkedStatusEffects.Add(args.Effect.StatusEffect);
        Dirty(entity, controller);
    }

    [SubscribeLocalEvent]
    private void OnAnimationEnded(Entity<CEActiveAnimationActionComponent> ent, ref CEAnimationActionEndedEvent args)
    {
        foreach (var status in ent.Comp.LinkedStatusEffects)
        {
            _statusEffect.TryRemoveStatusEffect(ent, status);
        }
    }
}
