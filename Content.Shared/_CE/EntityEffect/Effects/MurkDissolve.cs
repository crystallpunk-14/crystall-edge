using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Shifts the murk dissolution level of the target. Negative values restore it.
/// Does nothing to targets without <see cref="CEMurkDissolvingComponent"/>.
/// </summary>
public sealed partial class MurkDissolve : CEEntityEffectBase<MurkDissolve>
{
    /// <summary>
    /// Dissolution level added to the target, in the 0..1 range. Negative removes dissolution.
    /// </summary>
    [DataField]
    public float Amount = 0.1f;
}

public sealed partial class CEMurkDissolveEffectSystem : CEEntityEffectSystem<MurkDissolve>
{
    [Dependency] private CESharedMurkSystem _murk = default!;

    protected override void Effect(ref CEEntityEffectEvent<MurkDissolve> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _murk.AddDissolved(entity, args.Effect.Amount * args.Args.Power);
    }
}
