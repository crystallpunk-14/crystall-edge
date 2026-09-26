using Content.Shared.Examine;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Lets holders of a <see cref="CESecretRoleIconComponent"/> recognize each other - via a status
/// icon above their head and an examine line - without leaking that recognition to anyone else.
/// A viewer always recognizes their own exact role; whether they also recognize other roles in the
/// same department is <see cref="CESecretDepartmentPrototype.MembersRecognizeEachOther"/>. Ghosts
/// (observers) recognize every role, same as spectating in general lets you see everything.
/// </summary>
public sealed partial class CESecretRoleIconSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CESecretRoleIconComponent> ent, ref ComponentStartup args)
    {
        DirtyAllRoleIcons();
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        DirtyAllRoleIcons();
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        DirtyAllRoleIcons();
    }

    private void DirtyAllRoleIcons()
    {
        var query = EntityQueryEnumerator<CESecretRoleIconComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetStateAttempt(Entity<CESecretRoleIconComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player?.AttachedEntity is not { } viewer)
            return;

        args.Cancelled = !CanRecognize(ent, viewer);
    }

    [SubscribeLocalEvent]
    private void OnGetStatusIcons(Entity<CESecretRoleIconComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_proto.TryIndex(ent.Comp.Role, out var role) && _proto.TryIndex(role.Icon, out var icon))
            args.StatusIcons.Add(icon);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CESecretRoleIconComponent> ent, ref ExaminedEvent args)
    {
        if (args.Examiner == ent.Owner)
            return;

        if (!CanRecognize(ent, args.Examiner))
            return;

        if (!_proto.TryIndex(ent.Comp.Role, out var role))
            return;

        var color = TryGetDepartment(ent.Comp.Role, out var department) ? department.Color : Color.White;

        // Leading blank line, and the lowest possible priority, to always land as the very last
        // line of the examine text, no matter what else contributed to it.
        var message = new FormattedMessage();
        message.PushNewline();
        message.AddMessage(FormattedMessage.FromMarkupOrThrow(
            Loc.GetString("ce-secretrole-examine-fmt", ("role", role.LocalizedName), ("color", color))));

        args.PushMessage(message, int.MinValue);
    }

    /// <summary>
    /// Whether <paramref name="viewer"/> is allowed to see the icon/examine text carried by
    /// <paramref name="ent"/> - the single source of truth used both server-side (to decide whether
    /// the component's state is even networked to that viewer) and for the examine text.
    /// </summary>
    private bool CanRecognize(Entity<CESecretRoleIconComponent> ent, EntityUid viewer)
    {
        // Never your own icon above your own head.
        if (viewer == ent.Owner)
            return false;

        if (HasComp<GhostComponent>(viewer))
            return true;

        if (!_mind.TryGetMind(viewer, out var mindId, out var mind) ||
            !_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt) ||
            roleEnt.Value.Comp2.Role is not { } viewerRole)
        {
            return false;
        }

        if (viewerRole == ent.Comp.Role)
            return true;

        return TryGetDepartment(viewerRole, out var viewerDepartment) &&
            viewerDepartment.MembersRecognizeEachOther &&
            TryGetDepartment(ent.Comp.Role, out var targetDepartment) &&
            targetDepartment.ID == viewerDepartment.ID;
    }

    private bool TryGetDepartment(ProtoId<CESecretRolePrototype> role, out CESecretDepartmentPrototype department)
    {
        foreach (var candidate in _proto.EnumeratePrototypes<CESecretDepartmentPrototype>())
        {
            if (!candidate.Roles.Contains(role))
                continue;

            department = candidate;
            return true;
        }

        department = default!;
        return false;
    }
}
