using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// The target drops what it holds in its hands: the item just falls at its feet.
/// </summary>
public sealed partial class DropHeldItems : CEEntityEffectBase<DropHeldItems>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-drop-held-items");
}

public sealed partial class CEDropHeldItemsEffectSystem : CEEntityEffectSystem<DropHeldItems>
{
    [Dependency] private SharedHandsSystem _hands = default!;

    protected override void Effect(ref CEEntityEffectEvent<DropHeldItems> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } target)
            return;

        foreach (var held in _hands.EnumerateHeld(target))
        {
            _hands.TryDrop(target, held, checkActionBlocker: false);
        }
    }
}
