using Content.Shared._CE.Actions.Spells;
using Content.Shared.Beam;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Actions.Spells;

public sealed partial class WhiteSpellCreateBeam : CESpellEffect
{
    [DataField(required: true)]
    public EntProtoId BeamProto;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null || args.User is null)
            return;

        var beamSys = entManager.System<SharedBeamSystem>();

        beamSys.TryCreateBeam(args.User.Value, args.Target.Value, BeamProto);
    }
}
