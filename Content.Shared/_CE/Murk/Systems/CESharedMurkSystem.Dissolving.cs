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
    private void OnDissolvingRejuvenate(Entity<CEMurkDissolvingComponent> ent, ref RejuvenateEvent args)
    {
        if (ent.Comp.Converted || !TryComp<CEMurkDissolvingStatusComponent>(ent.Owner, out var status))
            return;

        SetDissolved(ent.Owner, status, 0f);
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
            if (!dissolving.Enabled || dissolving.Converted)
                continue;

            if (now < dissolving.NextUpdate)
                continue;

            dissolving.NextUpdate = now + dissolving.Frequency;
            DirtyField(uid, dissolving, nameof(CEMurkDissolvingComponent.NextUpdate));

            var status = EnsureComp<CEMurkDissolvingStatusComponent>(uid);

            var speed = InMurk(uid, xform) ? dissolving.DissolvingSpeed : -dissolving.RestoringSpeed;
            var delta = speed * (float)dissolving.Frequency.TotalSeconds;
            var newDissolved = Math.Clamp(status.Dissolved + delta, 0f, 1f);

            if (SetDissolved(uid, status, newDissolved) && newDissolved >= 1f)
            {
                var ev = new CEMurkDissolvedEvent();
                RaiseLocalEvent(uid, ref ev);
            }
        }
    }

    /// <summary>
    /// Sets <see cref="CEMurkDissolvingStatusComponent.Dissolved"/> and refreshes everything that
    /// reacts to it. Returns whether the value actually changed.
    /// </summary>
    private bool SetDissolved(EntityUid uid, CEMurkDissolvingStatusComponent status, float value)
    {
        if (value == status.Dissolved)
            return false;

        status.Dissolved = value;
        DirtyField(uid, status, nameof(CEMurkDissolvingStatusComponent.Dissolved));
        _movement.RefreshMovementSpeedModifiers(uid);
        UpdateDissolvingAlert(uid, status);

        return true;
    }

    private void UpdateDissolvingAlert(EntityUid uid, CEMurkDissolvingStatusComponent status)
    {
        if (status.Dissolved <= 0f)
        {
            _alerts.ClearAlert(uid, status.Alert);
            return;
        }

        short severity = status.Dissolved switch
        {
            <= 1f / 3f => 1,
            <= 2f / 3f => 2,
            _ => 3,
        };

        _alerts.ShowAlert(uid, status.Alert, severity);
    }
}

/// <summary>
/// Raised on an entity the moment the murk finishes dissolving it. The server turns it into a
/// murked soul from here.
/// </summary>
[ByRefEvent]
public record struct CEMurkDissolvedEvent;
