using Content.Shared._CE.Vampire.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Examine;

namespace Content.Shared._CE.Vampire;

public abstract partial class CESharedVampireSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private void InitializeSpell()
    {
        SubscribeLocalEvent<CEMagicEffectVampireComponent, ActionAttemptEvent>(OnVampireCastAttempt);
        SubscribeLocalEvent<CEMagicEffectVampireComponent, ExaminedEvent>(OnVampireCastExamine);

        SubscribeLocalEvent<CEActionVampireCandlesComponent, ExaminedEvent>(OnVampireCandlesCastExamine);
    }

    private void OnVampireCastAttempt(Entity<CEMagicEffectVampireComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        //If we are not vampires in principle, we certainly should not have this ability,
        //but then we will not limit its use to a valid vampire form that is unavailable to us.

        if (!HasComp<CEVampireComponent>(args.User))
            return;

        if (!HasComp<CEVampireVisualsComponent>(args.User))
        {
            _popup.PopupClient(Loc.GetString("ce-magic-spell-need-vampire-valid"), args.User, args.User);
            args.Cancelled = true;
        }
    }

    private void OnVampireCastExamine(Entity<CEMagicEffectVampireComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup($"{Loc.GetString("ce-magic-spell-need-vampire-valid")}");
    }

    private void OnVampireCandlesCastExamine(Entity<CEActionVampireCandlesComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup($"{Loc.GetString("ce-magic-spell-need-ignited-vamp-candles", ("count", ent.Comp.CandleCount))}");
    }
}
