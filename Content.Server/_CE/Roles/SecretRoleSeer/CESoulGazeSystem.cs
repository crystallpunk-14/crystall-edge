using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared._CE.Roles;
using Content.Shared._CE.Roles.SecretRoleSeer;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Jittering;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Roles.SecretRoleSeer;

/// <summary>
/// Server-only on purpose: the target's mind and secret role are never sent to the performer's
/// client, so the outcome can't be predicted.
/// </summary>
public sealed partial class CESoulGazeSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private CESecretRoleIconSystem _roleIcon = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void OnSoulGaze(CESoulGazeActionEvent args)
    {
        if (args.Handled ||
            !_mind.TryGetMind(args.Performer, out var performerMind, out _) ||
            !_mind.TryGetMind(args.Target, out var targetMind, out var targetMindComp))
        {
            return;
        }

        args.Handled = true;

        var performer = args.Performer;
        var target = args.Target;

        string message;
        if (_role.MindHasRole<CESecretRoleComponent>((targetMind, targetMindComp), out var roleEnt) &&
            roleEnt.Value.Comp2.Role is { } roleId &&
            _proto.Resolve(roleId, out var role))
        {
            var memory = EnsureComp<CESoulGazeMemoryComponent>(performerMind);
            memory.Minds.Add(targetMind);
            Dirty(performerMind, memory);

            // The icon is only networked to viewers allowed to recognize it - re-evaluate that now.
            if (TryComp<CESecretRoleIconComponent>(target, out var icon))
                Dirty(target, icon);

            _adminLog.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(performer):player} used soul gaze on {ToPrettyString(target):target} and learned secret role {roleId}");

            var color = _roleIcon.TryGetDepartment(roleId, out var department) ? department.Color : Color.White;
            message = Loc.GetString("ce-soul-gaze-revealed",
                ("name", Name(target)),
                ("target", target),
                ("role", role.LocalizedName),
                ("color", color));
        }
        else
        {
            _adminLog.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(performer):player} used soul gaze on {ToPrettyString(target):target} and found no secret role");

            message = Loc.GetString("ce-soul-gaze-nothing", ("name", Name(target)));
        }

        if (TryComp<ActorComponent>(performer, out var performerActor))
        {
            _popup.PopupEntity(FormattedMessage.RemoveMarkupOrThrow(message), performer, performer);
            _chat.ChatMessageToOne(ChatChannel.Server, FormattedMessage.RemoveMarkupOrThrow(message), message, EntityUid.Invalid, false, performerActor.PlayerSession.Channel);

            if (args.Sound is { } sound)
                _audio.PlayGlobal(sound, performerActor.PlayerSession);
        }

        _jitter.DoJitter(target, args.JitterDuration, true, args.JitterAmplitude, args.JitterFrequency);

        var shiver = Loc.GetString("ce-soul-gaze-shiver");
        _popup.PopupEntity(shiver, target, target, PopupType.MediumCaution);
        if (TryComp<ActorComponent>(target, out var targetActor))
            _chat.ChatMessageToOne(ChatChannel.Server, shiver, shiver, EntityUid.Invalid, false, targetActor.PlayerSession.Channel);
    }
}
