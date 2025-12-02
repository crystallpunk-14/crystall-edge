/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 *
 * Taken from https://github.com/EphemeralSpace/ephemeral-space/pull/335/files?notification_referrer_id=NT_kwDOBb-lNbQyMDgzMjQ4Nzk4Nzo5NjQ0NTc0OQ
 */

using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Blinking;

public abstract class CESharedBlinkingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CEBlinkerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CEBlinkerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CEBlinkerComponent, SleepStateChangedEvent>(OnSleepStateChanged);
    }

    private void OnMapInit(Entity<CEBlinkerComponent> ent, ref MapInitEvent args)
    {
        ResetBlink(ent);
    }

    private void OnMobStateChanged(Entity<CEBlinkerComponent> ent, ref MobStateChangedEvent args)
    {
        SetEnabled(ent.AsNullable(), args.NewMobState != MobState.Dead);
    }

    private void OnSleepStateChanged(Entity<CEBlinkerComponent> ent, ref SleepStateChangedEvent args)
    {
        Appearance.SetData(ent.Owner, CEBlinkVisuals.EyesClosed, args.FellAsleep);
    }

    private void ResetBlink(Entity<CEBlinkerComponent> ent)
    {
        ent.Comp.NextBlinkTime = _timing.CurTime + _random.Next(ent.Comp.MinBlinkDelay, ent.Comp.MaxBlinkDelay);
        Dirty(ent);
    }

    public void SetEnabled(Entity<CEBlinkerComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);

        if (enabled)
            ResetBlink((ent, ent.Comp));
    }

    public virtual void Blink(Entity<CEBlinkerComponent> ent)
    {
        ResetBlink(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEBlinkerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled)
                continue;
            if (_timing.CurTime < comp.NextBlinkTime)
                continue;
            Blink((uid, comp));
        }
    }
}
