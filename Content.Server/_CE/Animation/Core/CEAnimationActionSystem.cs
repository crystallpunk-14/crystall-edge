using System.Linq;
using Content.Shared._CE.ZLevels.Core;
using Content.Shared._CE.Animation.Core;
using Content.Shared._CE.Animation.Core.Components;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Server._CE.Animation.Core;

public sealed partial class CEAnimationActionSystem : CESharedAnimationActionSystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    /// <summary>
    /// Sends <see cref="CEEntityAnimationEvent"/> to all clients in PVS (except the entity's own player)
    /// so they can play the visual animation locally without relying on the shared update loop,
    /// which is now skipped for them because <see cref="CEActiveAnimationActionComponent.LastEvent"/> is networked.
    /// </summary>
    protected override void OnKeyframeActions(EntityUid uid, CEActiveAnimationActionComponent controller, TimeSpan keyFrame, List<CEEntityEffect> actions)
    {
        // Only send visual events if there is an EntityAnimation or UserAnimation effect in this keyframe.
        if (!actions.Any(a => a is EntityAnimation || a is UserAnimation))
            return;

        if (controller.ActiveAnimation is not { } animId)
            return;

        var angle = _transform.GetWorldRotation(uid);

        var ev = new CEEntityAnimationEvent(
            GetNetEntity(uid),
            controller.Used.HasValue ? GetNetEntity(controller.Used.Value) : null,
            angle,
            keyFrame,
            animId,
            controller.AnimationSpeed,
            controller.TargetEntity.HasValue ? GetNetEntity(controller.TargetEntity.Value) : null,
            controller.TargetCoordinates.HasValue ? GetNetCoordinates(controller.TargetCoordinates.Value) : null);

        // Exclude the entity's own player (they handle visuals via prediction).
        var filter = CEFilter.ZPvsExcept(uid, EntityManager);
        RaiseNetworkEvent(ev, filter);
    }
}
