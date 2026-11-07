using Content.Server._CE.GameTicking;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CEAddMurkEffectSystem : CEEntityEffectSystem<AddMurk>
{
    [Dependency] private CEMurkConsumingRuleSystem _murkConsuming = default!;

    protected override void Effect(ref CEEntityEffectEvent<AddMurk> args)
    {
        _murkConsuming.AddMurk(args.Effect.Amount, args.Effect.ScaleWithPlayers);
    }
}
