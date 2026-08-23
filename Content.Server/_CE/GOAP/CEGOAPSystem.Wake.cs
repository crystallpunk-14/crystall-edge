using Content.Shared._CE.GOAP.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Robust.Shared.Player;

namespace Content.Server._CE.GOAP;

public sealed partial class CEGOAPSystem
{
    // CrystallEdge: Rogue used a CE-only CEMobStateSystem (Alive/Critical only). This fork has no CE health
    // stack, so GOAP hooks into vanilla mob state instead, treating Critical/Dead as "down".
    [Dependency] private MobStateSystem _mobState = default!;

    private void InitWake()
    {
        SubscribeLocalEvent<CEGOAPComponent, CECheckGOAPAwakeEvent>(OnCheckAwake);

        SubscribeLocalEvent<CEGOAPComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CEGOAPComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CEGOAPComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerDetached(Entity<CEGOAPComponent> ent, ref PlayerDetachedEvent args)
    {
        UpdateAwakeStatus(ent.Owner);
    }

    private void OnPlayerAttached(Entity<CEGOAPComponent> ent, ref PlayerAttachedEvent args)
    {
        UpdateAwakeStatus(ent.Owner);
    }

    private void OnMobStateChanged(Entity<CEGOAPComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateAwakeStatus(ent.Owner);
    }

    private void OnCheckAwake(Entity<CEGOAPComponent> ent, ref CECheckGOAPAwakeEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ActorComponent>(ent))
            return;

        if (TryComp<MobStateComponent>(ent, out var mobState))
        {
            if (_mobState.IsIncapacitated(ent, mobState))
                return;
        }

        // Sleeping entities are blocked from waking via normal checks.
        // They must be woken explicitly by CEGOAPSleepingSystem.
        if (HasComp<CEGOAPSleepingComponent>(ent))
            return;

        args.WakeUp();
    }

    public void UpdateAwakeStatus(Entity<CEGOAPComponent?> ent)
    {
        var ev = new CECheckGOAPAwakeEvent();
        RaiseLocalEvent(ent, ev);

        if (ev.Awake)
            Wake(ent);
        else
            Sleep(ent);
    }

    /// <summary>
    /// Activates GOAP processing for this entity.
    /// </summary>
    private void Wake(Entity<CEGOAPComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        EnsureComp<CEActiveGOAPComponent>(ent);
        EnsureComp<ActiveNPCComponent>(ent);
    }

    /// <summary>
    /// Deactivates GOAP processing for this entity.
    /// </summary>
    private void Sleep(Entity<CEGOAPComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ClearPlan((ent, ent.Comp));
        RemCompDeferred<CEActiveGOAPComponent>(ent);
        RemCompDeferred<ActiveNPCComponent>(ent);
    }
}


public sealed partial class CECheckGOAPAwakeEvent : HandledEntityEventArgs
{
    private bool _awake;
    public bool Awake => _awake;

    public void WakeUp()
    {
        _awake = true;
        Handled = true;
    }
}
