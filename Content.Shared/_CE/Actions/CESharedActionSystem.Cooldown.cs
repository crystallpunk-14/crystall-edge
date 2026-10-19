using Content.Shared._CE.Actions.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnRandomCooldownMapInit(Entity<CEActionRandomCooldownComponent> ent, ref MapInitEvent args)
    {
        RollCooldown(ent);
    }

    [SubscribeLocalEvent]
    private void OnRandomCooldownPerformed(Entity<CEActionRandomCooldownComponent> ent, ref ActionPerformedEvent args)
    {
        RollCooldown(ent);
    }

    private void RollCooldown(Entity<CEActionRandomCooldownComponent> ent)
    {
        if (_net.IsClient)
            return;

        var seconds = _random.NextFloat((float) ent.Comp.Min.TotalSeconds, (float) ent.Comp.Max.TotalSeconds);
        _actions.SetCooldown(ent.Owner, TimeSpan.FromSeconds(seconds));
    }
}
