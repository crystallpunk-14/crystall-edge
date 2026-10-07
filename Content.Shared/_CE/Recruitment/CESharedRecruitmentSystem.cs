using Content.Shared._CE.Roles;
using Content.Shared.Actions.Events;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Shared._CE.Recruitment;

/// <summary>
/// Validates <see cref="CERecruitmentActionComponent"/> targets before the action is used.
/// </summary>
public abstract partial class CESharedRecruitmentSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private CESecretRoleIconSystem _roleIcon = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [SubscribeLocalEvent]
    private void OnValidate(Entity<CERecruitmentActionComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Invalid)
            return;

        if (GetEntity(args.Input.EntityTarget) is not { Valid: true } target)
            return;

        if (GetInvalidReason(ent, target) is not { } reason)
            return;

        Popup.PopupEntity(Loc.GetString(reason), args.User, args.User);
        args.Invalid = true;
    }

    private LocId? GetInvalidReason(Entity<CERecruitmentActionComponent> ent, EntityUid target)
    {
        if (!TryComp<MindContainerComponent>(target, out var mindContainer) ||
            !mindContainer.HasMind ||
            HasComp<GhostComponent>(target) ||
            !_mobState.IsAlive(target))
        {
            return "ce-recruitment-invalid-target";
        }

        // Members of the department can see each other's role icon component, so this check is
        // predicted correctly for exactly the people it rejects.
        if (TryComp<CESecretRoleIconComponent>(target, out var targetIcon) &&
            _roleIcon.TryGetDepartment(ent.Comp.Role, out var department) &&
            _roleIcon.TryGetDepartment(targetIcon.Role, out var targetDepartment) &&
            department.ID == targetDepartment.ID)
        {
            return "ce-recruitment-already-member";
        }

        return GetServerInvalidReason(target);
    }

    /// <summary>
    /// Checks only the server can make - whether the target has a connected player, or is already
    /// considering another invite.
    /// </summary>
    protected virtual LocId? GetServerInvalidReason(EntityUid target)
    {
        return null;
    }
}
