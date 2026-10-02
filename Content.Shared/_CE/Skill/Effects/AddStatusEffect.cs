using System.Linq;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill.Effects;

/// <summary>
/// Applies a permanent status effect for as long as the skill is known - see
/// <see cref="PermanentStatusEffectsSystem"/> for the always-on-component equivalent of this.
/// </summary>
public sealed partial class AddStatusEffect : CESkillEffect
{
    [DataField(required: true)]
    public EntProtoId Effect;

    /// <summary>
    /// Other status effects removed together with <see cref="Effect"/> when the skill is lost, e.g. effects
    /// that <see cref="Effect"/> can be swapped for while the skill is known.
    /// </summary>
    [DataField]
    public List<EntProtoId> RemoveAlso = new();

    public override void AddSkill(IEntityManager entManager, EntityUid target)
    {
        var statusEffects = entManager.System<StatusEffectsSystem>();
        statusEffects.TrySetStatusEffectDuration(target, Effect);
    }

    public override void RemoveSkill(IEntityManager entManager, EntityUid target)
    {
        var statusEffects = entManager.System<StatusEffectsSystem>();
        statusEffects.TryRemoveStatusEffect(target, Effect);

        foreach (var other in RemoveAlso)
        {
            statusEffects.TryRemoveStatusEffect(target, other);
        }
    }

    public override string? GetName(IEntityManager entManager, IPrototypeManager protoManager)
    {
        return !protoManager.TryIndex(Effect, out var indexedEffect) ? string.Empty : indexedEffect.Name;
    }

    public override string? GetDescription(IEntityManager entManager, IPrototypeManager protoManager, ProtoId<CESkillPrototype> skill)
    {
        return !protoManager.TryIndex(Effect, out var indexedEffect) ? string.Empty : indexedEffect.Description;
    }

    public override SpriteSpecifier? GetIcon(IEntityManager entManager, IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Effect, out var effectProto))
            return null;

        var compFactory = entManager.ComponentFactory;

        if (!effectProto.TryGetComponent<StatusEffectAlertComponent>(out var effectAlertComp, compFactory))
            return null;

        if (!protoManager.TryIndex(effectAlertComp.Alert, out var alertProto))
            return null;

        return alertProto.Icons.First();
    }
}
