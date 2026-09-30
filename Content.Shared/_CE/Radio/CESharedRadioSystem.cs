using Content.Shared._CE.Radio.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Verbs;

namespace Content.Shared._CE.Radio;

/// <summary>
/// Radio booth toggling, power and examine. The actual speech relay lives on the server.
/// </summary>
public abstract partial class CESharedRadioSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    [SubscribeLocalEvent]
    private void OnMicrophoneInit(Entity<CERadioMicrophoneComponent> ent, ref MapInitEvent args)
    {
        UpdateListener(ent);
    }

    [SubscribeLocalEvent]
    private void OnActivate(Entity<CERadioMicrophoneComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        SetEnabled(ent, args.User, !ent.Comp.Enabled);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnGetAltVerbs(Entity<CERadioMicrophoneComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(ent.Comp.Enabled ? "ce-radio-microphone-verb-off" : "ce-radio-microphone-verb-on"),
            Act = () => SetEnabled(ent, user, !ent.Comp.Enabled),
        });
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CERadioMicrophoneComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        SetEnabled(ent, null, false);
    }

    [SubscribeLocalEvent]
    private void OnListenAttempt(Entity<CERadioMicrophoneComponent> ent, ref ListenAttemptEvent args)
    {
        if (!ent.Comp.Enabled
            || !_power.IsPowered(ent.Owner)
            || HasComp<CERadioSpeakerComponent>(args.Source) // no feedback loops
            || !_interaction.InRangeUnobstructed(args.Source, ent.Owner, 0))
        {
            args.Cancel();
        }
    }

    [SubscribeLocalEvent]
    private void OnMicrophoneExamined(Entity<CERadioMicrophoneComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("ce-radio-microphone-examine-frequencies",
            ("frequencies", string.Join(", ", ent.Comp.Frequencies))));
    }

    [SubscribeLocalEvent]
    private void OnSpeakerExamined(Entity<CERadioSpeakerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("ce-radio-speaker-examine-frequency", ("frequency", ent.Comp.Frequency)));
    }

    /// <summary>
    /// Turns the radio booth on or off. Turning on requires power.
    /// </summary>
    public void SetEnabled(Entity<CERadioMicrophoneComponent> ent, EntityUid? user, bool enabled)
    {
        if (enabled && !_power.IsPowered(ent.Owner))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("ce-radio-microphone-no-power"), ent, user.Value);
            return;
        }

        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);

        UpdateListener(ent);
    }

    private void UpdateListener(Entity<CERadioMicrophoneComponent> ent)
    {
        _appearance.SetData(ent, RadioDeviceVisuals.Broadcasting, ent.Comp.Enabled);

        if (ent.Comp.Enabled)
            EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(ent);
    }
}
