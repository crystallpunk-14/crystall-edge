using Content.Shared._CE.Placeable;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Farming.Placement;

/// <summary>
/// The tile is a soil the placed plant can grow in.
/// </summary>
public sealed partial class PlantSoil : CEPlacementCondition
{
    protected override LocId DefaultPopup => "ce-farming-soil-wrong";

    public override bool Check(IEntityManager entManager, CEPlacementTarget target)
    {
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        return protoManager.Resolve(target.Prototype, out var plant)
               && entManager.System<CESharedFarmingSystem>().IsSoilSuitable(plant, target.Grid, target.Tile);
    }

    public override string GetPopup(IEntityManager entManager, CEPlacementTarget target)
    {
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        var name = protoManager.Resolve(target.Prototype, out var plant) ? plant.Name : target.Prototype.Id;
        return Loc.GetString(Popup ?? DefaultPopup, ("seed", FormattedMessage.RemoveMarkupPermissive(name)));
    }
}
