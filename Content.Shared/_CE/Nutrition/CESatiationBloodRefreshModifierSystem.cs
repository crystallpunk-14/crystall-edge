using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Nutrition;

/// <summary>
/// Maintains <see cref="CESatiationBloodRefreshModifierComponent"/> multipliers and applies them
/// to the natural blood regeneration of the bloodstream.
/// </summary>
public sealed partial class CESatiationBloodRefreshModifierSystem :
    BaseSatiationEffectSystem<CESatiationBloodRefreshModifierComponent, float>
{
    protected override Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<float>> GetThresholds(
        CESatiationBloodRefreshModifierComponent comp) => comp.Satiations;

    protected override float DefaultValue() => 1f;

    [SubscribeLocalEvent]
    private static void OnGetBloodRefreshModifier(
        Entity<CESatiationBloodRefreshModifierComponent> ent,
        ref CEGetBloodRefreshModifierEvent args)
    {
        foreach (var (_, thresholds) in ent.Comp.Satiations)
        {
            args.Multiplier *= thresholds.Current;
        }
    }
}

/// <summary>
/// Raised on an entity before its bloodstream naturally regenerates blood.
/// The regenerated amount is multiplied by <see cref="Multiplier"/>.
/// </summary>
[ByRefEvent]
public record struct CEGetBloodRefreshModifierEvent(float Multiplier);
