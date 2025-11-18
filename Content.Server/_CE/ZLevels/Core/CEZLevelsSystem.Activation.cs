using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Ghost;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server._CE.ZLevels.Core;

public sealed partial class CEZLevelsSystem
{
    private void InitializeActivation()
    {
        SubscribeLocalEvent<CEZPhysicsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEZPhysicsComponent, AnchorStateChangedEvent>(OnAnchorStateChange);
        SubscribeLocalEvent<CEZPhysicsComponent, PhysicsBodyTypeChangedEvent>(OnPhysicsBodyTypeChange);
    }

    private void OnAnchorStateChange(Entity<CEZPhysicsComponent> ent, ref AnchorStateChangedEvent args)
    {
        CheckActivation(ent);
    }

    private void OnMapInit(Entity<CEZPhysicsComponent> ent, ref MapInitEvent args)
    {
        CheckActivation(ent);
    }

    private void OnPhysicsBodyTypeChange(Entity<CEZPhysicsComponent> ent, ref PhysicsBodyTypeChangedEvent args)
    {
        CheckActivation(ent);
    }

    private void CheckActivation(Entity<CEZPhysicsComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var xform = Transform(ent);

        if (HasComp<GhostComponent>(ent))
        {
            SetActiveStatus(ent, false);
            return;
        }

        if (xform.Anchored)
        {
            SetActiveStatus(ent, false);
            return;
        }

        if (TryComp<PhysicsComponent>(ent, out var physics))
        {
            if (physics.BodyType == BodyType.Static)
            {
                SetActiveStatus(ent, false);
                return;
            }
        }

        SetActiveStatus(ent, true);
    }

    private void SetActiveStatus(EntityUid ent, bool active)
    {
        if (active)
            EnsureComp<CEActiveZPhysicsComponent>(ent);
        else
            RemComp<CEActiveZPhysicsComponent>(ent);
    }
}
