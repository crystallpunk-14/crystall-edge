using Content.Shared._CE.Ghost;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Roles;

public sealed partial class CESecretRoleIconSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private CEGhostMetaSystem _ghostMeta = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CESecretRoleIconComponent> ent, ref ComponentStartup args)
    {
        DirtyAllRoleIcons();
    }

    [SubscribeLocalEvent]
    private void OnOriginalStartup(Entity<CESecretRoleOriginalIconComponent> ent, ref ComponentStartup args)
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

    /// <summary>
    /// Makes every role icon recognizable by everyone - called once the round is over.
    /// </summary>
    public void RevealAllRoleIcons()
    {
        var query = EntityQueryEnumerator<CESecretRoleIconComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Revealed = true;
            Dirty(uid, comp);
        }

        var originalQuery = EntityQueryEnumerator<CESecretRoleOriginalIconComponent>();
        while (originalQuery.MoveNext(out var uid, out var comp))
        {
            comp.Revealed = true;
            Dirty(uid, comp);
        }
    }

    /// <summary>
    /// Re-evaluates which viewers receive every role icon.
    /// </summary>
    public void DirtyAllRoleIcons()
    {
        var query = EntityQueryEnumerator<CESecretRoleIconComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }

        var originalQuery = EntityQueryEnumerator<CESecretRoleOriginalIconComponent>();
        while (originalQuery.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetStateAttempt(Entity<CESecretRoleIconComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player?.AttachedEntity is not { } viewer)
            return;

        args.Cancelled = !CanRecognize(ent, ent.Comp.Role, viewer, ent.Comp.Revealed);
    }

    [SubscribeLocalEvent]
    private void OnOriginalGetStateAttempt(Entity<CESecretRoleOriginalIconComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player?.AttachedEntity is not { } viewer)
            return;

        args.Cancelled = !CanRecognize(ent, ent.Comp.Role, viewer, ent.Comp.Revealed);
    }

    [SubscribeLocalEvent]
    private void OnGetStatusIcons(Entity<CESecretRoleIconComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity is not { } viewer || !CanRecognize(ent, ent.Comp.Role, viewer, ent.Comp.Revealed))
            return;

        AddStatusIcon(ent.Comp.Role, ref args);
    }

    [SubscribeLocalEvent]
    private void OnOriginalGetStatusIcons(Entity<CESecretRoleOriginalIconComponent> ent, ref GetStatusIconsEvent args)
    {
        // A single icon above the head - the current role wins whenever the viewer recognizes it.
        if (_player.LocalEntity is not { } viewer ||
            !TryGetVisibleRoles(ent, viewer, out var original, out var current) ||
            current is not null ||
            original is not { } originalRole)
        {
            return;
        }

        AddStatusIcon(originalRole, ref args);
    }

    private void AddStatusIcon(ProtoId<CESecretRolePrototype> roleId, ref GetStatusIconsEvent args)
    {
        if (_proto.TryIndex(roleId, out var role) && _proto.TryIndex(role.Icon, out var icon))
            args.StatusIcons.Add(icon);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CESecretRoleIconComponent> ent, ref ExaminedEvent args)
    {
        PushExamineText(ent, args);
    }

    [SubscribeLocalEvent]
    private void OnOriginalExamined(Entity<CESecretRoleOriginalIconComponent> ent, ref ExaminedEvent args)
    {
        // Both roles share one line, written by the current role's handler whenever it exists.
        if (HasComp<CESecretRoleIconComponent>(ent))
            return;

        PushExamineText(ent, args);
    }

    private void PushExamineText(EntityUid target, ExaminedEvent args)
    {
        if (!TryGetVisibleRoles(target, args.Examiner, out var original, out var current))
            return;

        // Leading blank line, and the lowest possible priority, to always land as the very last
        // line of the examine text, no matter what else contributed to it.
        var message = new FormattedMessage();
        message.PushNewline();
        message.AddMessage(FormattedMessage.FromMarkupOrThrow(
            Loc.GetString("ce-secretrole-examine-fmt", ("role", GetRolesMarkup(original, current)))));

        args.PushMessage(message, int.MinValue);
    }

    /// <summary>
    /// Which of <paramref name="target"/>'s roles <paramref name="viewer"/> recognizes, read off the
    /// icon components - so on the client it only ever sees roles the server networked to it.
    /// </summary>
    /// <param name="original">The original role - null if unrecognized or never changed.</param>
    /// <returns>Whether at least one role is recognized.</returns>
    public bool TryGetVisibleRoles(EntityUid target,
        EntityUid viewer,
        out ProtoId<CESecretRolePrototype>? original,
        out ProtoId<CESecretRolePrototype>? current)
    {
        original = null;
        current = null;

        if (TryComp<CESecretRoleIconComponent>(target, out var icon) && CanRecognize(target, icon.Role, viewer, icon.Revealed))
            current = icon.Role;

        if (TryComp<CESecretRoleOriginalIconComponent>(target, out var originalIcon) &&
            CanRecognize(target, originalIcon.Role, viewer, originalIcon.Revealed))
        {
            original = originalIcon.Role;
        }

        return original is not null || current is not null;
    }

    /// <summary>
    /// Whether <paramref name="viewer"/> is allowed to see <paramref name="role"/> held by
    /// <paramref name="target"/> - the single source of truth used both server-side (to decide whether
    /// an icon component's state is even networked to that viewer) and for the icon/examine text.
    /// Each of the target's roles is checked on its own, against both of the viewer's roles.
    /// </summary>
    /// <param name="revealed">Whether the icon carrying <paramref name="role"/> was revealed at round end.</param>
    public bool CanRecognize(EntityUid target, ProtoId<CESecretRolePrototype> role, EntityUid viewer, bool revealed)
    {
        // Never your own icon above your own head.
        if (viewer == target)
            return false;

        if (revealed || _ghostMeta.CanSeeMeta(viewer))
            return true;

        if (!_mind.TryGetMind(viewer, out var mindId, out var mind))
            return false;

        var recognizeEv = new CESecretRoleRecognizeEvent(target);
        RaiseLocalEvent(mindId, ref recognizeEv);
        if (recognizeEv.Recognized)
            return true;

        if (!TryGetDepartment(role, out var targetDepartment) ||
            !targetDepartment.MembersRecognizeEachOther ||
            !_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt))
        {
            return false;
        }

        return IsInDepartment(roleEnt.Value.Comp2.Role, targetDepartment) ||
            IsInDepartment(roleEnt.Value.Comp2.OriginalRole, targetDepartment);
    }

    private static bool IsInDepartment(ProtoId<CESecretRolePrototype>? role, CESecretDepartmentPrototype department)
    {
        return role is { } roleId && department.Roles.Contains(roleId);
    }

    public bool TryGetDepartment(ProtoId<CESecretRolePrototype> role, out CESecretDepartmentPrototype department)
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

    /// <summary>
    /// "Original → Current" as markup, each role in its department's color. Collapses to a single
    /// role when either is null or both are the same.
    /// </summary>
    public string GetRolesMarkup(ProtoId<CESecretRolePrototype>? original, ProtoId<CESecretRolePrototype>? current)
    {
        return JoinRoles(GetRoleMarkup(original), GetRoleMarkup(current), original == current);
    }

    /// <summary>
    /// Plain-text <see cref="GetRolesMarkup"/>, for labels that carry a single color.
    /// </summary>
    public string GetRolesName(ProtoId<CESecretRolePrototype>? original, ProtoId<CESecretRolePrototype>? current)
    {
        return JoinRoles(GetRoleName(original), GetRoleName(current), original == current);
    }

    private string JoinRoles(string? original, string? current, bool same)
    {
        if (original is null || same)
            return current ?? original ?? string.Empty;

        if (current is null)
            return original;

        return Loc.GetString("ce-secret-role-history-fmt", ("original", original), ("current", current));
    }

    private string? GetRoleName(ProtoId<CESecretRolePrototype>? roleId)
    {
        return roleId is { } id && _proto.TryIndex(id, out var role) ? role.LocalizedName : null;
    }

    private string? GetRoleMarkup(ProtoId<CESecretRolePrototype>? roleId)
    {
        if (roleId is not { } id || GetRoleName(id) is not { } name)
            return null;

        var color = TryGetDepartment(id, out var department) ? department.Color : Color.White;
        return $"[color={color.ToHex()}]{FormattedMessage.EscapeText(name)}[/color]";
    }
}

/// <summary>
/// Raised on the viewer's mind to let other systems grant recognition of <see cref="Target"/>'s
/// secret roles on top of the usual department rules.
/// </summary>
[ByRefEvent]
public record struct CESecretRoleRecognizeEvent(EntityUid Target, bool Recognized = false);
