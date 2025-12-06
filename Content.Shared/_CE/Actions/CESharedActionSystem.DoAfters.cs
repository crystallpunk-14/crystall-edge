using Content.Shared._CE.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Movement.Systems;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem
{
    private void InitializeDoAfter()
    {
        SubscribeLocalEvent<CEActionDoAfterSlowdownComponent, CEActionStartDoAfterEvent>(OnStartDoAfter);
        SubscribeLocalEvent<CEActionDoAfterSlowdownComponent, ActionDoAfterEvent>(OnEndDoAfter);
        SubscribeLocalEvent<CESlowdownFromActionsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnStartDoAfter(Entity<CEActionDoAfterSlowdownComponent> ent, ref CEActionStartDoAfterEvent args)
    {
        var performer = GetEntity(args.Performer);
        EnsureComp<CESlowdownFromActionsComponent>(performer, out var slowdown);

        slowdown.SpeedAffectors.TryAdd(GetNetEntity(ent), ent.Comp.SpeedMultiplier);
        Dirty(performer, slowdown);
        _movement.RefreshMovementSpeedModifiers(performer);
    }

    private void OnEndDoAfter(Entity<CEActionDoAfterSlowdownComponent> ent, ref ActionDoAfterEvent args)
    {
        if (args.Repeat)
            return;

        var performer = GetEntity(args.Performer);
        if (!TryComp<CESlowdownFromActionsComponent>(performer, out var slowdown))
            return;

        slowdown.SpeedAffectors.Remove(GetNetEntity(ent));
        Dirty(performer, slowdown);

        _movement.RefreshMovementSpeedModifiers(performer);

        if (slowdown.SpeedAffectors.Count == 0)
            RemCompDeferred<CESlowdownFromActionsComponent>(performer);
    }

    private void OnRefreshMovespeed(Entity<CESlowdownFromActionsComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var targetSpeedModifier = 1f;

        foreach (var (_, affector) in ent.Comp.SpeedAffectors)
        {
            targetSpeedModifier *= affector;
        }

        args.ModifySpeed(targetSpeedModifier);
    }
}
