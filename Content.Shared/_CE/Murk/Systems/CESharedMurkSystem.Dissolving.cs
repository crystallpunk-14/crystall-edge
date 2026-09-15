using Content.Shared._CE.Murk.Components;
using Content.Shared.Alert;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Murk.Systems;

public abstract partial class CESharedMurkSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private AlertsSystem _alerts = default!;

    [SubscribeLocalEvent]
    private void OnDissolvingRefreshMovementSpeed(Entity<CEMurkDissolvingComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.Enabled || ent.Comp.Dissolved <= 0f)
            return;

        var modifier = 1f - ent.Comp.Dissolved * ent.Comp.MaxSlowdown;
        args.ModifySpeed(modifier);
    }

    [SubscribeLocalEvent]
    private void OnDissolvingRejuvenate(Entity<CEMurkDissolvingComponent> ent, ref RejuvenateEvent args)
    {
        SetDissolved(ent.Owner, ent.Comp, 0f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateDissolving();
    }

    private void UpdateDissolving()
    {
        var now = _timing.CurTime;

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
            SetDissolved(uid, dissolving, newDissolved);
        }
    }

    private void SetDissolved(EntityUid uid, CEMurkDissolvingComponent dissolving, float value)
    {
        if (value == dissolving.Dissolved)
            return;

        dissolving.Dissolved = value;
        DirtyField(uid, dissolving, nameof(CEMurkDissolvingComponent.Dissolved));
        _movement.RefreshMovementSpeedModifiers(uid);
        UpdateDissolvingAlert(uid, dissolving);
    }

    private void UpdateDissolvingAlert(EntityUid uid, CEMurkDissolvingComponent dissolving)
    {
        if (dissolving.Dissolved <= 0f)
        {
            _alerts.ClearAlert(uid, dissolving.Alert);
            return;
        }

        short severity = dissolving.Dissolved switch
        {
            <= 1f / 3f => 1,
            <= 2f / 3f => 2,
            _ => 3,
        };

        _alerts.ShowAlert(uid, dissolving.Alert, severity);
    }
}
