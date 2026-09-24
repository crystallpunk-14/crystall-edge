using Content.Shared._CE.Murk.Components;
using Content.Shared.Alert;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Murk;

public abstract partial class CESharedMurkSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

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

            var speed = InMurk(uid, xform)
                ? dissolving.DissolvingSpeed * GetDissolvingModifier(uid)
                : -dissolving.RestoringSpeed * GetRestoringModifier(uid);
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

    /// <summary>
    /// Combined <see cref="CEMurkDissolvingModifierComponent.DissolvingModifier"/> of every active
    /// status effect on the entity, multiplied together. 1 if none are active.
    /// </summary>
    private float GetDissolvingModifier(EntityUid uid)
    {
        if (!_statusEffects.TryEffectsWithComp<CEMurkDissolvingModifierComponent>(uid, out var effects))
            return 1f;

        var modifier = 1f;
        foreach (var effect in effects)
        {
            modifier *= effect.Comp1.DissolvingModifier;
        }

        return modifier;
    }

    /// <summary>
    /// Combined <see cref="CEMurkDissolvingModifierComponent.RestoringModifier"/> of every active
    /// status effect on the entity, multiplied together. 1 if none are active.
    /// </summary>
    private float GetRestoringModifier(EntityUid uid)
    {
        if (!_statusEffects.TryEffectsWithComp<CEMurkDissolvingModifierComponent>(uid, out var effects))
            return 1f;

        var modifier = 1f;
        foreach (var effect in effects)
        {
            modifier *= effect.Comp1.RestoringModifier;
        }

        return modifier;
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
