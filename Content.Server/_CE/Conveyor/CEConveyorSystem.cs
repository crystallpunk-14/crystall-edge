using Content.Server.Physics.Controllers;
using Content.Server.Power.EntitySystems;
using Content.Shared.Conveyor;

namespace Content.Server.Physics.Controllers;

/// <inheritdoc/>
public sealed partial class ConveyorController
{
    private void InitCrystallEdge()
    {
        SubscribeLocalEvent<ConveyorComponent, PowerConsumerReceivedChanged>(OnPowerChange);
    }

    private void OnPowerChange(Entity<ConveyorComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (args.ReceivedPower >= args.DrawRate)
        {
            ent.Comp.Powered = true;
            SetState(ent, ConveyorState.Forward, ent.Comp);
        }
        else
        {
            ent.Comp.Powered = false;
            SetState(ent, ConveyorState.Off, ent.Comp);
        }
        UpdateAppearance(ent, ent.Comp);
        Dirty(ent);
    }
}
