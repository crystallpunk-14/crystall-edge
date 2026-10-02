using Content.Shared.Interaction;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Simulates a click with the item resolved by <see cref="CEEntityEffect.EffectTarget"/> on the entity resolved by
/// <see cref="InteractTarget"/>, as if the user used the item on it by hand. The user is always the effect source.
/// Lets melee hits trigger item interactions that vanilla melee would (e.g. cuffing with a rope).
/// </summary>
public sealed partial class UseItem : CEEntityEffectBase<UseItem>
{
    public UseItem()
    {
        EffectTarget = CEEffectTarget.Used;
    }

    [DataField]
    public CEEffectTarget InteractTarget = CEEffectTarget.Target;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-use-item");
}

public sealed partial class CEUseItemEffectSystem : CEEntityEffectSystem<UseItem>
{
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private INetManager _net = default!;

    protected override void Effect(ref CEEntityEffectEvent<UseItem> args)
    {
        if (!_net.IsServer)
            return;

        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } item)
            return;

        if (ResolveEffectEntity(args.Args, args.Effect.InteractTarget) is not { } target)
            return;

        _interaction.InteractDoAfter(args.Args.Source, item, target, Transform(target).Coordinates, canReach: true);
    }
}
