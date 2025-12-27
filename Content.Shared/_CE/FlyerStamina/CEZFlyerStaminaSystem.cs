using Content.Shared._CE.ZLevels.Flight;
using Content.Shared._CE.ZLevels.Flight.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CE.FlyerStamina;

public sealed class CEZFlyerStaminaSystem : EntitySystem
{
    [Dependency] private readonly CESharedZFlightSystem _flight = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZFlyerStaminaComponent, CEStartFlightAttemptEvent>(OnFlightAttempt);
        SubscribeLocalEvent<CEZFlyerStaminaComponent, CEEnterStaminaCritEvent>(OnStaminaCrit);
    }

    private void OnStaminaCrit(Entity<CEZFlyerStaminaComponent> ent, ref CEEnterStaminaCritEvent args)
    {
        _flight.DeactivateFlight(ent.Owner);
    }

    private void OnFlightAttempt(Entity<CEZFlyerStaminaComponent> ent, ref CEStartFlightAttemptEvent args)
    {
        if (!TryComp<StaminaComponent>(ent, out var stamina))
            return;

        if (stamina.Critical)
            args.Cancel();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StaminaComponent, CEZFlyerStaminaComponent, CEZFlyerComponent>();
        while (query.MoveNext(out var uid, out var stamina, out var flyerStamina, out var flyer))
        {
            if (_timing.CurTime < flyerStamina.NextConsumeTime)
                continue;

            flyerStamina.NextConsumeTime = _timing.CurTime + flyerStamina.StaminaConsumeFrequency;

            if (!flyer.Active)
                continue;

            _stamina.TakeStaminaDamage(uid, flyerStamina.StaminaDraw);
        }
    }
}
