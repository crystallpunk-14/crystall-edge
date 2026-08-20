using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.MeleeWeapon;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.Graphics;

namespace Content.Client._CE.MeleeWeapon;

public sealed partial class CEClientWeaponSystem : CESharedWeaponSystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private InputSystem _inputSystem = default!;
    [Dependency] private MapSystem _map = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();
        UpdatesOutsidePrediction = true;
    }

    public override void HandleArcAttackHit(EntityUid user, Entity<CEWeaponComponent> weapon, List<EntityUid> targets, string? effectSlot, float power = 1f)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        // Send the client-calculated hit list as a predicted event.
        // The shared handler will call TryAttack both during prediction and on server.
        RaisePredictiveEvent(new CEWeaponArcHitEvent(
            GetNetEntity(weapon.Owner),
            GetNetEntityList(targets),
            effectSlot,
            power));
    }
}
