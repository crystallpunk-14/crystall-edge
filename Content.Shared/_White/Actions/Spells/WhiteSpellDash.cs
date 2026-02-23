using Content.Shared._CE.Actions.Spells;
using Content.Shared._White.Dash;

namespace Content.Shared._White.Actions.Spells;

public sealed partial class WhiteSpellDash : CESpellEffect
{
    [DataField]
    public float Speed = 10f;

    [DataField]
    public float Range = 3.5f;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.User is null || args.Position is null)
            return;

        entManager.System<WhiteDashSystem>().PerformDash(args.User.Value, args.Position.Value, Speed, Range);
    }
}
