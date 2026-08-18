using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Applies a status effect, refreshing or setting its duration without adding stacks.
/// Use <see cref="ApplyStatusEffectStack"/> if you need stack accumulation.
/// </summary>
public sealed partial class ApplyStatusEffect : CEEntityEffectBase<ApplyStatusEffect>
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    [DataField(required: true)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1f);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var statusName = prototype.TryIndex(StatusEffect, out var statusProto) ? statusProto.Name : StatusEffect.Id;
        return Loc.GetString("ce-entity-effect-guidebook-apply-status", ("status", statusName), ("duration", Duration.TotalSeconds));
    }
}

public sealed partial class CEApplyStatusEffectEffectSystem : CEEntityEffectSystem<ApplyStatusEffect>
{
    [Dependency] private StatusEffectsSystem _statusEffect = default!;

    protected override void Effect(ref CEEntityEffectEvent<ApplyStatusEffect> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        // Raise on the source so that status effects on the attacker (e.g. pacifism) get a chance
        // to cancel the application via StatusEffectRelayedEvent.
        var attempt = new CEAttemptApplyStatusEffectEvent(entity, args.Effect.StatusEffect, args.Effect.Duration);
        if (Exists(args.Args.Source))
            RaiseLocalEvent(args.Args.Source, attempt);

        if (attempt.Cancelled)
            return;

        // Raise on the TARGET so that target-side status effects (e.g. CEStatusEffectImmunity) can cancel.
        var receiveAttempt = new CEAttemptReceiveStatusEffectEvent(entity, args.Effect.StatusEffect, args.Effect.Duration);
        RaiseLocalEvent(entity, receiveAttempt);
        if (receiveAttempt.Cancelled)
            return;

        if (!_statusEffect.TrySetStatusEffectDuration(entity, args.Effect.StatusEffect, out var statusEnt, args.Effect.Duration))
            return;

        if (Exists(args.Args.Source))
            RaiseLocalEvent(args.Args.Source, new CEAfterApplyStatusEffectEvent(entity, args.Effect.StatusEffect, used: args.Args.Used));
    }
}

/// <summary>
/// Raised on the source (attacker) before <see cref="ApplyStatusEffect"/> applies a status to a
/// target. Cancelling prevents the effect from being applied. Relayed to the source's active
/// status effects via <c>StatusEffectRelayedEvent</c>.
/// </summary>
public sealed partial class CEAttemptApplyStatusEffectEvent(EntityUid target, EntProtoId statusEffect, TimeSpan duration) : EntityEventArgs
{
    public readonly EntityUid Target = target;
    public readonly EntProtoId StatusEffect = statusEffect;
    public readonly TimeSpan Duration = duration;
    public bool Cancelled;
}

/// <summary>
/// Raised on the TARGET entity before <see cref="ApplyStatusEffect"/> applies a status effect.
/// Cancelling prevents the effect from being applied.
/// Relayed to the target's active status effects via <c>StatusEffectRelayedEvent</c>.
/// </summary>
public sealed partial class CEAttemptReceiveStatusEffectEvent(EntityUid target, EntProtoId statusEffect, TimeSpan duration) : EntityEventArgs
{
    public readonly EntityUid Target = target;
    public readonly EntProtoId StatusEffect = statusEffect;
    public readonly TimeSpan Duration = duration;
    public bool Cancelled;
}

/// <summary>
/// Raised on the source (attacker/caster) after <see cref="ApplyStatusEffect"/> or
/// <see cref="ApplyStatusEffectStack"/> successfully applies a status effect to a target.
/// Relayed to the source's active status effects via <c>StatusEffectRelayedEvent</c>.
/// </summary>
public sealed partial class CEAfterApplyStatusEffectEvent(EntityUid target, EntProtoId statusEffect, int amount = 1, EntityUid? used = null) : EntityEventArgs
{
    public readonly EntityUid Target = target;
    public readonly EntProtoId StatusEffect = statusEffect;
    public readonly int Amount = amount;
    public readonly EntityUid? Used = used;
}

/// <summary>
/// Raised on the source (attacker/caster) when a status effect they applied to a target is removed.
/// Relayed to the source's active status effects via <c>StatusEffectRelayedEvent</c>.
/// </summary>
public sealed partial class CEAfterRemoveStatusEffectEvent(EntityUid target, EntProtoId statusEffect) : EntityEventArgs
{
    public readonly EntityUid Target = target;
    public readonly EntProtoId StatusEffect = statusEffect;
}
