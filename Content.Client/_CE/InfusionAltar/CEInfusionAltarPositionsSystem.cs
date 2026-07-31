using Content.Client.Examine;
using Content.Shared._CE.InfusionAltar.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.InfusionAltar;

/// <summary>
/// Shows temporary indicators around an infusion altar (on examine) at every tile offset listed in
/// <see cref="CEInfusionAltarComponent.PossiblePedestalsPositions"/>, so players know where sub-pedestals
/// can be placed. Mirrors <see cref="Content.Client.Machines.EntitySystems.MultipartMachineSystem"/>.
/// </summary>
public sealed partial class CEInfusionAltarPositionsSystem : EntitySystem
{
    private readonly EntProtoId _indicatorEntity = "CEInfusionAltarPositionIndicator";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEInfusionAltarComponent, ClientExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<CEInfusionAltarComponent> ent, ref ClientExaminedEvent args)
    {
        foreach (var offset in ent.Comp.PossiblePedestalsPositions)
        {
            Spawn(_indicatorEntity, new EntityCoordinates(ent.Owner, offset));
        }
    }
}
