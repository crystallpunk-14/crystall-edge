using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.MeleeWeapon;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.Client._CE.MeleeWeapon;

public sealed partial class CEClientWeaponSystem
{

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entity = _player.LocalEntity;

        if (entity == null)
            return;

        var user = entity.Value;

        if (!TryGetWeapon(user, out var used))
            return;

        if (!CombatMode.IsInCombatMode(user) || !Blocker.CanAttack(user))
        {
            used.Value.Comp.Using = false;
            return;
        }

        var primaryDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var secondaryDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);

        // Release detection — stop attacking when buttons are released.
        if (primaryDown != BoundKeyState.Down && secondaryDown != BoundKeyState.Down)
        {
            if (used.Value.Comp.Using)
                RaisePredictiveEvent(new CEStopWeaponUseEvent(GetNetEntity(used.Value)));

            return;
        }

        if (used.Value.Comp.Using)
            return;

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
            return;


        EntityCoordinates coordinates;

        if (MapManager.TryFindGridAt(mousePos, out var gridUid, out _))
            coordinates = TransformSystem.ToCoordinates(gridUid, mousePos);
        else
            coordinates = TransformSystem.ToCoordinates(_map.GetMap(mousePos.MapId), mousePos);

        //Calculate angle from player to target position
        if (!_xformQuery.TryComp(user, out var userXform))
            return;

        var playerPos = TransformSystem.GetMapCoordinates(userXform).Position;
        var targetPos = TransformSystem.ToMapCoordinates(coordinates).Position;
        var direction = targetPos - playerPos;
        var angle = Angle.FromWorldVec(direction);

        if (primaryDown == BoundKeyState.Down)
        {
            RaisePredictiveEvent(new CEWeaponUseEvent(angle, GetNetEntity(used.Value), CEUseType.Primary));
            return;
        }

        if (secondaryDown == BoundKeyState.Down)
        {
            RaisePredictiveEvent(new CEWeaponUseEvent(angle, GetNetEntity(used.Value), CEUseType.Secondary));
        }
    }
}
