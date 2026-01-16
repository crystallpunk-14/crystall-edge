using Content.Shared.Throwing;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellThrowUserTo : CESpellEffect
{
    [DataField]
    public float ThrowPower = 10f;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Position is null || args.User is null)
            return;

        var throwing = entManager.System<ThrowingSystem>();

        throwing.TryThrow(args.User.Value, args.Position.Value, ThrowPower);
    }
}
