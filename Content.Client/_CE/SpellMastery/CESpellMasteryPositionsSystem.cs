using Content.Client.Examine;
using Content.Shared._CE.MagicVision.Components;
using Content.Shared._CE.SpellMastery.Components;
using Content.Shared.Ghost;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.SpellMastery;

// CrystallEdge: rough copy of Content.Client._CE.InfusionAltar.CEInfusionAltarPositionsSystem, not yet
// cleaned up/unified. Reuses the existing CEInfusionAltarPositionIndicator entity for the marker itself.

/// <summary>
/// Shows temporary indicators around a spell mastery altar (on examine) at every tile offset listed in
/// <see cref="CESpellMasteryAltarComponent.PossiblePedestalsPositions"/>, so players know where
/// sub-pedestals can be placed. Only shown to ghosts or examiners currently perceiving with magic
/// vision (e.g. thaumaturgy goggles).
/// </summary>
public sealed partial class CESpellMasteryPositionsSystem : EntitySystem
{
    private readonly EntProtoId _indicatorEntity = "CEInfusionAltarPositionIndicator";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESpellMasteryAltarComponent, ClientExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<CESpellMasteryAltarComponent> ent, ref ClientExaminedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner) && !HasComp<CEMagicVisionComponent>(args.Examiner))
            return;

        foreach (var offset in ent.Comp.PossiblePedestalsPositions)
        {
            Spawn(_indicatorEntity, new EntityCoordinates(ent.Owner, offset));
        }
    }
}
