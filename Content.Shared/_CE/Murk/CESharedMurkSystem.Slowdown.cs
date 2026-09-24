using Content.Shared._CE.Murk.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared._CE.Murk;

public abstract partial class CESharedMurkSystem
{
    [SubscribeLocalEvent]
    private void OnSlowdownRefreshMovementSpeed(Entity<CEMurkDissolvingSlowdownComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<CEMurkDissolvingStatusComponent>(ent.Owner, out var status) || status.Dissolved <= 0f)
            return;

        var modifier = 1f - status.Dissolved * ent.Comp.MaxSlowdown;
        args.ModifySpeed(modifier);
    }
}
