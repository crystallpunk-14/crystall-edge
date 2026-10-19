using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the checked entity (<see cref="CEEntityCondition.ConditionTarget"/>) belongs to a faction
/// that <see cref="RelativeTo"/> is hostile to. Both entities need <see cref="NpcFactionMemberComponent"/>;
/// friendly factions and the entity itself never count as enemies.
/// </summary>
public sealed partial class InEnemyFaction : CEEntityConditionBase<InEnemyFaction>
{
    /// <summary>
    /// Whose point of view hostility is checked from.
    /// </summary>
    [DataField]
    public CEEffectTarget RelativeTo = CEEffectTarget.User;
}

public sealed partial class CEInEnemyFactionConditionSystem : CEEntityConditionSystem<InEnemyFaction>
{
    [Dependency] private NpcFactionSystem _faction = default!;

    [Dependency] private EntityQuery<NpcFactionMemberComponent> _factionQuery = default!;

    protected override void Condition(ref CEEntityConditionEvent<InEnemyFaction> args)
    {
        if (args.Args.Resolve(args.Condition.RelativeTo) is not { } relative || relative == args.Entity)
            return;

        if (!_factionQuery.TryComp(relative, out var relativeFaction) ||
            !_factionQuery.TryComp(args.Entity, out var entityFaction))
            return;

        if (_faction.IsEntityFriendly((relative, relativeFaction), (args.Entity, entityFaction)))
            return;

        args.Result = _faction.IsMemberOfAny((args.Entity, entityFaction), relativeFaction.HostileFactions);
    }
}
