using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles;

public sealed partial class CESecretRoleSelectionSystem
{
    [Dependency] private CESecretRoleIconSystem _roleIcon = default!;

    /// <summary>
    /// Looks up a mind's current and original secret role, for the round-end player manifest (see
    /// <c>GameTicker.RoundFlow.cs</c>'s <c>ShowRoundEndScoreboard</c>). False if the mind never
    /// held a secret role.
    /// </summary>
    public bool TryGetSecretRoles(EntityUid mindId,
        [NotNullWhen(true)] out ProtoId<CESecretRolePrototype>? role,
        out ProtoId<CESecretRolePrototype>? originalRole)
    {
        role = null;
        originalRole = null;

        if (!_role.MindHasRole<CESecretRoleComponent>(mindId, out var roleEnt) ||
            roleEnt.Value.Comp2.Role is not { } roleId)
            return false;

        role = roleId;
        originalRole = roleEnt.Value.Comp2.OriginalRole;
        return true;
    }

    [SubscribeLocalEvent]
    private void OnRunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (args.New == GameRunLevel.PostRound)
            _roleIcon.RevealAllRoleIcons();
    }

    // GameRuleSystem<T> already subscribes to RoundEndTextAppendEvent and dispatches to this
    // virtual method - a second raw SubscribeLocalEvent here would throw on duplicate
    // subscription, unlike ES's ESSecretIdentitySystem which isn't a GameRuleSystem<T> itself.
    protected override void AppendRoundEndText(EntityUid uid,
        CESecretRoleSelectionComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var departments = GetActiveDepartments();
        if (departments.Count == 0)
            return;

        args.AddLine(Loc.GetString("ce-roundend-secret-role-factions-header"));
        foreach (var (department, _) in departments)
        {
            args.AddLine(Loc.GetString("ce-roundend-secret-role-department-list",
                ("name", Loc.GetString(department.Name)),
                ("color", department.Color)));

            if (component.DepartmentObjectiveHolders.TryGetValue(department.ID, out var holderUid))
            {
                foreach (var objective in _objectives.GetObjectives(holderUid))
                {
                    _objectives.RefreshObjectiveProgress(objective.AsNullable());

                    args.AddLine(Loc.GetString("ce-roundend-secret-role-objective-fmt",
                        ("text", _objectives.GetObjectiveString(objective.AsNullable()))));
                }
            }

            args.AddLine(string.Empty);
        }

        args.AddLine(Loc.GetString("ce-roundend-secret-role-player-summary-header"));
        foreach (var (department, members) in departments)
        {
            args.AddLine(Loc.GetString("ce-roundend-secret-role-player-group",
                ("name", Loc.GetString(department.Name)),
                ("color", department.Color)));

            foreach (var (mindId, mind, role, originalRole) in members)
            {
                var username = mind.OriginalOwnerUserId != null
                    ? _playerManager.GetPlayerData(mind.OriginalOwnerUserId.Value).UserName
                    : Loc.GetString("generic-unknown-title");

                var characterName = mind.CharacterName ?? Loc.GetString("generic-unknown-title");

                var objectives = CompOrNull<CEObjectiveHolderComponent>(mindId)?.OwnedObjectives ?? [];

                args.AddLine(Loc.GetString("ce-roundend-secret-role-player-summary",
                    ("name", characterName),
                    ("username", username),
                    ("role", _roleIcon.GetRolesMarkup(originalRole, role.ID)),
                    ("objCount", objectives.Count)));

                foreach (var objectiveUid in objectives)
                {
                    if (!TryComp<CEObjectiveComponent>(objectiveUid, out var objectiveComp))
                        continue;

                    _objectives.RefreshObjectiveProgress((objectiveUid, objectiveComp));

                    args.AddLine(Loc.GetString("ce-roundend-secret-role-player-objective-fmt",
                        ("text", _objectives.GetObjectiveString((objectiveUid, objectiveComp)))));
                }
            }

            args.AddLine(string.Empty);
        }
    }

    /// <summary>
    /// Groups every mind that currently holds a secret role by its faction, ordered by
    /// <see cref="CESecretDepartmentPrototype.Weight"/> (highest first) - mirrors
    /// <c>ESSharedSecretIdentitySystem.GetOrderedOrganizations</c> upstream.
    /// </summary>
    private List<(CESecretDepartmentPrototype Department, List<(EntityUid MindId, MindComponent Mind, CESecretRolePrototype Role, ProtoId<CESecretRolePrototype>? OriginalRole)> Members)>
        GetActiveDepartments()
    {
        var grouped =
            new Dictionary<string, (CESecretDepartmentPrototype Department, List<(EntityUid, MindComponent, CESecretRolePrototype, ProtoId<CESecretRolePrototype>?)> Members)>();

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mindComp))
        {
            if (!_role.MindHasRole<CESecretRoleComponent>((mindId, mindComp), out var roleEnt))
                continue;

            if (roleEnt.Value.Comp2.Role is not { } roleId ||
                !_proto.TryIndex(roleId, out var role) ||
                !TryGetDepartment(roleId, out var department))
                continue;

            if (!grouped.TryGetValue(department.ID, out var entry))
            {
                entry = (department, new List<(EntityUid, MindComponent, CESecretRolePrototype, ProtoId<CESecretRolePrototype>?)>());
                grouped[department.ID] = entry;
            }

            entry.Members.Add((mindId, mindComp, role, roleEnt.Value.Comp2.OriginalRole));
        }

        return grouped.Values.OrderByDescending(entry => entry.Department.Weight).ThenBy(entry => entry.Department.ID).ToList();
    }
}
