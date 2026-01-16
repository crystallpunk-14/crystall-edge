using Content.Shared._CE.Power;
using Robust.Shared.Map;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellIrradiate : CESpellEffect
{
    [DataField]
    public float Charge = 10f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3f);

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        EntityCoordinates? targetPoint = null;

        if (args.Target is not null &&
            entManager.TryGetComponent<TransformComponent>(args.Target.Value, out var transformComponent))
            targetPoint = transformComponent.Coordinates;
        else if (args.Position is not null)
            targetPoint = args.Position;

        if (targetPoint is null)
            return;

        var power = entManager.System<CESharedPowerSystem>();

        power.Irradiate(targetPoint.Value, Charge, Duration);
    }
}
