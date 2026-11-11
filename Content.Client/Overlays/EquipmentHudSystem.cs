using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusEffectNew;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Overlays;

/// <summary>
/// This is a base system to make it easier to enable or disabling UI elements based on whether or not the player has
/// some component, either on their controlled entity on some worn piece of equipment.
/// </summary>
public abstract partial class EquipmentHudSystem<T> : EntitySystem where T : IComponent
{
    [Dependency] private IPlayerManager _player = default!;

    [ViewVariables]
    public bool IsActive { get; private set; }
    protected virtual SlotFlags TargetSlots => ~SlotFlags.POCKET;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<T, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<T, GotEquippedEvent>(OnCompEquip);
        SubscribeLocalEvent<T, GotUnequippedEvent>(OnCompUnequip);

        SubscribeLocalEvent<T, RefreshEquipmentHudEvent<T>>(OnRefreshComponentHud);
        SubscribeLocalEvent<T, InventoryRelayedEvent<RefreshEquipmentHudEvent<T>>>(OnRefreshEquipmentHud);

        // CrystallEdge: status effects grant HUDs too, so skills never put the component on the player
        SubscribeLocalEvent<T, StatusEffectRelayedEvent<RefreshEquipmentHudEvent<T>>>(OnRefreshStatusEffectHud);
        SubscribeLocalEvent<T, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<T, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        // CrystallEdge end

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void Update(RefreshEquipmentHudEvent<T> ev)
    {
        IsActive = true;
        UpdateInternal(ev);
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        DeactivateInternal();
    }

    protected virtual void UpdateInternal(RefreshEquipmentHudEvent<T> args) { }

    protected virtual void DeactivateInternal() { }

    private void OnStartup(Entity<T> ent, ref ComponentStartup args)
    {
        RefreshOverlay();
    }

    private void OnRemove(Entity<T> ent, ref ComponentRemove args)
    {
        RefreshOverlay();
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RefreshOverlay();
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_player.LocalSession?.AttachedEntity is null)
            Deactivate();
    }

    private void OnCompEquip(Entity<T> ent, ref GotEquippedEvent args)
    {
        RefreshOverlay();
    }

    private void OnCompUnequip(Entity<T> ent, ref GotUnequippedEvent args)
    {
        RefreshOverlay();
    }

    // CrystallEdge: status effects grant HUDs too
    private void OnStatusEffectApplied(Entity<T> ent, ref StatusEffectAppliedEvent args)
    {
        RefreshOverlay();
    }

    private void OnStatusEffectRemoved(Entity<T> ent, ref StatusEffectRemovedEvent args)
    {
        RefreshOverlay();
    }

    private void OnRefreshStatusEffectHud(Entity<T> ent, ref StatusEffectRelayedEvent<RefreshEquipmentHudEvent<T>> args)
    {
        var ev = args.Args;
        OnRefreshComponentHud(ent, ref ev);
        args.Args = ev;
    }
    // CrystallEdge end

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        Deactivate();
    }

    protected virtual void OnRefreshEquipmentHud(Entity<T> ent, ref InventoryRelayedEvent<RefreshEquipmentHudEvent<T>> args)
    {
        OnRefreshComponentHud(ent, ref args.Args);
    }

    protected virtual void OnRefreshComponentHud(Entity<T> ent, ref RefreshEquipmentHudEvent<T> args)
    {
        args.Active = true;
        args.Components.Add(ent.Comp);
    }

    protected void RefreshOverlay()
    {
        if (_player.LocalSession?.AttachedEntity is not { } entity)
            return;

        var ev = new RefreshEquipmentHudEvent<T>(TargetSlots);
        RaiseLocalEvent(entity, ref ev);

        if (ev.Active)
            Update(ev);
        else
            Deactivate();
    }
}
