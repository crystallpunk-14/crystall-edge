using Content.Shared.Gibbing;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Gibs the target. Only the server gibs; see <see cref="GibbingSystem.Gib"/>.
/// </summary>
public sealed partial class Gib : CEEntityEffectBase<Gib>
{
    [DataField]
    public bool DropGiblets = true;
}

public sealed partial class CEGibEffectSystem : CEEntityEffectSystem<Gib>
{
    [Dependency] private GibbingSystem _gibbing = default!;

    protected override void Effect(ref CEEntityEffectEvent<Gib> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } target)
            return;

        _gibbing.Gib(target, args.Effect.DropGiblets, args.Args.Source);
    }
}
