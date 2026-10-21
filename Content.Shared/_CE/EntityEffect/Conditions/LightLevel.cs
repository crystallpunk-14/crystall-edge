using Content.Shared._CE.Light;

namespace Content.Shared._CE.EntityEffect.Conditions;

/// <summary>
/// Passes when the light level at the entity (sky and lamps, see <see cref="CELightLevelSystem"/>)
/// is within [<see cref="Min"/>, <see cref="Max"/>]. Levels go from 0 (dark) to 1 (fully lit).
/// </summary>
public sealed partial class LightLevel : CEEntityConditionBase<LightLevel>
{
    [DataField]
    public float Min;

    [DataField]
    public float Max = 1f;
}

public sealed partial class CELightLevelConditionSystem : CEEntityConditionSystem<LightLevel>
{
    [Dependency] private CELightLevelSystem _lightLevel = default!;

    protected override void Condition(ref CEEntityConditionEvent<LightLevel> args)
    {
        if (!_lightLevel.TryGetLightLevel(args.Entity, out var level))
            return;

        args.Result = level >= args.Condition.Min && level <= args.Condition.Max;
    }
}
