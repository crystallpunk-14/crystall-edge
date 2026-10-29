using Content.Server._CE.GameTicking;
using Content.Server._CE.Objectives;
using Content.Server._CE.Roles;
using Content.Server.Administration.Logs;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared._CE.DarkVessel;
using Content.Shared._CE.EntityEffect.Conditions;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared._CE.Skill;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._CE.DarkVessel;

/// <summary>
/// Handles <see cref="CEDarkVesselActionEvent"/>: after a do-after next to a dead body that held a
/// secret role, moves the performer's mind into it, takes over that secret role and heals the body,
/// while the old body drops dead. The body's previous owner stays a ghost for good.
/// </summary>
public sealed partial class CEDarkVesselSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private CEMainCharacterSystem _mainCharacter = default!;
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private CERevivableConditionSystem _revivable = default!;
    [Dependency] private CESecretRoleSelectionSystem _secretRoles = default!;
    [Dependency] private CESharedSkillSystem _skill = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnAction(CEDarkVesselActionEvent args)
    {
        if (args.Handled || !TryComp<CEDarkVesselActionComponent>(args.Action, out var vessel))
            return;

        if (GetInvalidReason(args.Target) is { } reason)
        {
            _popup.PopupEntity(Loc.GetString(reason), args.Performer, args.Performer);
            return;
        }

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.Performer,
            vessel.Delay,
            new CEDarkVesselDoAfterEvent { Action = args.Action },
            null,
            args.Target)
        {
            Broadcast = true,
            BlockDuplicate = true,
            BreakOnMove = true,
            BreakOnDamage = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(CEDarkVesselDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        var user = args.User;
        if (GetInvalidReason(target) is not null ||
            !TryComp<CESecretRoleIconComponent>(target, out var targetIcon) ||
            !TryComp<ActorComponent>(user, out var actor) ||
            !_mind.TryGetMind(user, out var mindId, out var mind))
        {
            return;
        }

        args.Handled = true;
        var role = targetIcon.Role;

        // The old body's actor component drops its session as soon as the mind leaves it.
        var session = actor.PlayerSession;

        EvictOwner(target);
        _mind.TransferTo(mindId, target, mind: mind);
        _mainCharacter.TransferMainCharacter(user, target);

        _damageable.ClearAllDamage(target);
        _mobState.ChangeMobState(target, MobState.Alive, origin: user);
        _mobState.ChangeMobState(user, MobState.Dead);

        // Skills stay on the body, so the new one keeps its own; the vessel's objectives stay too.
        _secretRoles.TrySetSecretRole(session,
            role,
            out _,
            removeSkills: false,
            keepObjectives: true,
            giveGear: false);

        if (TryComp<CEDarkVesselActionComponent>(args.Action, out var vessel))
        {
            foreach (var skill in vessel.KeptSkills)
            {
                _skill.TryAddSkill(target, skill, force: true);
            }
        }

        foreach (var objective in _objectives.GetOwnedObjectives(mindId))
        {
            if (!TryComp<CEObjectiveDarkVesselConditionComponent>(objective, out var condition))
                continue;

            condition.Done = true;
            _objectives.RefreshObjectiveProgress(objective.AsNullable());
        }

        _adminLog.Add(LogType.AntagSelection,
            LogImpact.High,
            $"{ToPrettyString(mindId):player} left the body {ToPrettyString(user)} for the body {ToPrettyString(target)}, taking over secret role {role}");
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveDarkVesselConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        args.Progress = ent.Comp.Done ? 1f : 0f;
    }

    // The action's Revivable target condition already covers dead/revivable/has-a-player - only the
    // secret role is left, since role icons aren't networked to the client. Re-checked when the
    // do-after ends, as the body may have been revived or taken in the meantime.
    private LocId? GetInvalidReason(EntityUid target)
    {
        if (!_revivable.IsRevivable(target))
            return "ce-revivable-required";

        if (!HasComp<CESecretRoleIconComponent>(target))
            return "ce-dark-vessel-no-role";

        return null;
    }

    /// <summary>
    /// Casts the body's previous owner out for good - there is no body left to return to.
    /// </summary>
    private void EvictOwner(EntityUid body)
    {
        if (!_mind.TryGetMind(body, out var mindId, out var mind))
            return;

        // Ghosting out of a dead body only visits the ghost - the mind still owns the body until it's
        // moved into the ghost for good.
        if (mind.VisitingEntity is null)
            _ghost.OnGhostAttempt(mindId, false, viaCommand: true, forced: true, mind: mind);

        if (mind.VisitingEntity is not { } ghost)
            return;

        _mind.TransferTo(mindId, ghost, mind: mind);
        _ghost.SetCanReturnToBody((ghost, null), false);
    }
}
