using Content.Server._CE.Conveyor;
using Content.Server.Physics.Controllers;
using Content.Server.Power.EntitySystems;
using Content.Shared.Conveyor;
using Content.Shared.Power;

namespace Content.Server.Physics.Controllers;

/// <inheritdoc/>
public sealed partial class ConveyorController
{
    private void InitCrystallEdge()
    {
        SubscribeLocalEvent<CEConveyorComponent, PowerChangedEvent>(OnPowerChange);
    }

    private void OnPowerChange(Entity<CEConveyorComponent> ent, ref PowerChangedEvent args)
    {
        if (!TryComp<ConveyorComponent>(ent, out var conv))
            return;

        if (args.Powered)
        {
            conv.Powered = true;
            SetState(ent, ConveyorState.Forward, conv);
        }
        else
        {
            conv.Powered = false;
            SetState(ent, ConveyorState.Off, conv);
        }
        UpdateAppearance(ent, conv);
        Dirty(ent);
    }
}
