using Content.Shared._CE.Murk.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Murk.Systems;

public abstract partial class CESharedMurkSystem
{
    [Dependency] private IGameTiming _timingDissolving = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedDissolving = default!;

    [SubscribeLocalEvent]
    private void OnDissolvingRefreshMovementSpeed(Entity<CEMurkDissolvingComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.Enabled || ent.Comp.Dissolved <= 0f)
            return;

        var modifier = 1f - ent.Comp.Dissolved * ent.Comp.MaxSlowdown;
        args.ModifySpeed(modifier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateDissolving();
    }

    private void UpdateDissolving()
    {
        var now = _timingDissolving.CurTime;

        var query = EntityQueryEnumerator<CEMurkDissolvingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dissolving, out var xform))
        {
            if (!dissolving.Enabled)
                continue;

            if (now < dissolving.NextUpdate)
                continue;

            dissolving.NextUpdate = now + dissolving.Frequency;
            DirtyField(uid, dissolving, nameof(CEMurkDissolvingComponent.NextUpdate));

            var speed = InMurk(uid, xform) ? dissolving.DissolvingSpeed : -dissolving.RestoringSpeed;
            var delta = speed * (float)dissolving.Frequency.TotalSeconds;
            var newDissolved = Math.Clamp(dissolving.Dissolved + delta, 0f, 1f);
            if (newDissolved == dissolving.Dissolved)
                continue;

            dissolving.Dissolved = newDissolved;
            DirtyField(uid, dissolving, nameof(CEMurkDissolvingComponent.Dissolved));
            _movementSpeedDissolving.RefreshMovementSpeedModifiers(uid);
        }
    }
}
