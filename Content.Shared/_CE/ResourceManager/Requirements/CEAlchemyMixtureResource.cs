using Content.Shared._CE.Alchemy.Data;
using Content.Shared._CE.Alchemy.Prototypes;
using Content.Shared._CE.Alchemy.World;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.ResourceManager.Requirements;

/// <summary>
/// Requires an alchemy mixture that works through the given zone strongly enough. The whole amount has to be
/// one portion (one recipe) in one container; other effects of that mixture do not matter.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CEAlchemyMixtureResource : CEResourceRequirement
{
    [DataField(required: true)]
    public ProtoId<CEAlchemyZonePrototype> Zone;

    [DataField(required: true)]
    public FixedPoint2 Amount;

    /// <summary>
    /// Minimal strength of the zone in the mixture, tuning included.
    /// </summary>
    [DataField]
    public float MinStrength = 0.3f;

    [DataField]
    public EntProtoId DummyEntityIcon = "CELiquidDropDummy";

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        return TryFindMixture(entManager, placedEntities, out _, out _);
    }

    public override void PostCraft(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        if (!TryFindMixture(entManager, placedEntities, out var soln, out var reagent))
            return;

        entManager.System<SharedSolutionContainerSystem>().RemoveReagent(soln, reagent, Amount);
    }

    private bool TryFindMixture(IEntityManager entManager,
        HashSet<EntityUid> placedEntities,
        out Entity<SolutionComponent> soln,
        out ReagentId reagent)
    {
        var solutionSys = entManager.System<SharedSolutionContainerSystem>();
        var alchemy = entManager.System<CEAlchemySystem>();

        foreach (var ent in placedEntities)
        {
            if (!solutionSys.TryGetDrawableSolution(ent, out var found, out var solution))
                continue;

            foreach (var (id, quantity) in solution.Contents)
            {
                if (quantity < Amount || !Matches(alchemy, id))
                    continue;

                soln = found.Value;
                reagent = id;
                return true;
            }
        }

        soln = default;
        reagent = default;
        return false;
    }

    private bool Matches(CEAlchemySystem alchemy, ReagentId reagent)
    {
        if (reagent.Data is not { } dataList)
            return false;

        foreach (var data in dataList)
        {
            if (data is not CEAlchemyData mixture)
                continue;

            foreach (var hit in alchemy.Analyze(mixture))
            {
                if (hit.Zone.ID == Zone && hit.Strength >= MinStrength)
                    return true;
            }
        }

        return false;
    }

    public override string GetRequirementAmount()
    {
        return $"{Amount}u";
    }

    public override string GetRequirementTitle(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Zone, out var zone))
            return string.Empty;

        return Loc.GetString("ce-workbench-alchemy-mixture-req",
            ("zone", Loc.GetString(zone.Name)),
            ("count", Amount),
            ("strength", CEAlchemySystem.FormatStrength(MinStrength)));
    }

    public override List<CEResourceIconLayer> GetRequirementIcon(IEntityManager entManager, IPrototypeManager protoManager)
    {
        var color = protoManager.TryIndex(Zone, out var zone) ? zone.Color : Color.White;
        return new List<CEResourceIconLayer> { CEResourceIconLayer.FromEntity(DummyEntityIcon, color) };
    }
}
