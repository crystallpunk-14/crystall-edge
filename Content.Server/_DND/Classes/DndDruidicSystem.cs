using Content.Server.Administration;
using Content.Shared._DND.Classes;
using Content.Shared._DND.Features;
using Content.Shared._DND.Spells;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._DND.Classes;

/// <summary>
/// Druidic signs and the Message cantrip ask for a short line of text.
/// </summary>
public sealed partial class DndDruidicSystem : EntitySystem
{
    [Dependency] private QuickDialogSystem _dialog = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnVerbs(EntityUid uid, DndDruidicComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.User != uid || !TryComp(args.User, out ActorComponent? actor))
            return;

        var user = args.User;
        var session = actor.PlayerSession;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("dnd-druidic-sign-verb"),
            Act = () =>
            {
                _dialog.OpenDialog(session, Loc.GetString("dnd-druidic-sign-verb"), Loc.GetString("dnd-druidic-sign-prompt"), (string message) =>
                {
                    PlaceSign(user, message);
                });
            },
        });
    }

    [SubscribeLocalEvent(after: [typeof(DndSpellSystem)])]
    private void OnCast(EntityUid uid, DndSpellcastingComponent component, DndCastSpellEvent args)
    {
        if (args.Spell != "Message" || !args.Handled || !TryComp(uid, out ActorComponent? actor))
            return;

        var caster = uid;
        var target = args.Target;
        _dialog.OpenDialog(actor.PlayerSession, Loc.GetString("dnd-spell-message-name"), Loc.GetString("dnd-spell-message-prompt"), (string message) =>
        {
            var text = Trim(message);
            if (text == "" || !Exists(target))
                return;

            _popup.PopupEntity(text, target, target, PopupType.Medium);
            if (target != caster)
                _popup.PopupEntity(text, caster, caster, PopupType.Small);
        });
    }

    private void PlaceSign(EntityUid user, string message)
    {
        var text = Trim(message);
        if (text == "" || !Exists(user))
            return;

        var prototype = "DndDruidicSign";
        var sign = Spawn(prototype, Transform(user).Coordinates);
        var component = EnsureComp<DndDruidicSignComponent>(sign);
        component.Message = text;
        Dirty(sign, component);
    }

    private static string Trim(string message)
    {
        var text = message.Trim();
        if (text.Length > 80)
            text = text.Substring(0, 80);

        return text;
    }
}
