using Content.Server._CE.Objectives.Components;
using Content.Server._CE.Roles;
using Content.Server.Roles;
using Content.Shared._CE.Character;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveDepartmentSurviveConditionComponent"/> - the average
/// survival of every body that held a role from the department - see <see cref="GetProgress"/>.
/// </summary>
public sealed partial class CEObjectiveDepartmentSurviveConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private RoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void OnInitialize(Entity<CEObjectiveDepartmentSurviveConditionComponent> ent, ref CEInitializeObjectiveEvent args)
    {
        if (_proto.TryIndex(ent.Comp.Department, out var department))
            CollectBodies(ent.Comp.Bodies, department.Roles);
    }

    [SubscribeLocalEvent]
    private void OnRoleGranted(ref CESecretRoleGrantedEvent args)
    {
        var query = EntityQueryEnumerator<CEObjectiveDepartmentSurviveConditionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_proto.TryIndex(comp.Department, out var department) ||
                !IsHolder(department.Roles, args.Role, args.Original) ||
                !comp.Bodies.Add(args.Body))
            {
                continue;
            }

            _objectives.RefreshObjectiveProgress(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveDepartmentSurviveConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        args.Progress = GetProgress(ent.Comp.Bodies, ent.Comp.Invert);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        RefreshAll();
    }

    [SubscribeLocalEvent]
    private void OnBodyStateChanged(ref CEBodyStateChangedEvent args)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        var query = EntityQueryEnumerator<CEObjectiveDepartmentSurviveConditionComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _objectives.RefreshObjectiveProgress(uid);
        }
    }

    /// <summary>
    /// Adds the body of every character that holds one of <paramref name="roles"/>, as its current or
    /// original role, to <paramref name="bodies"/> - for objectives created after the roles were handed out.
    /// </summary>
    public void CollectBodies(HashSet<EntityUid> bodies, ICollection<ProtoId<CESecretRolePrototype>> roles)
    {
        var query = EntityQueryEnumerator<CECharacterComponent, MindComponent>();
        while (query.MoveNext(out var uid, out var character, out var mind))
        {
            if (character.Body is not { } body ||
                !_role.MindHasRole<CESecretRoleComponent>((uid, mind), out var roleEnt) ||
                roleEnt.Value.Comp2.Role is not { } role)
            {
                continue;
            }

            if (IsHolder(roles, role, roleEnt.Value.Comp2.OriginalRole ?? role))
                bodies.Add(body);
        }
    }

    /// <summary>
    /// Whether <paramref name="role"/> or <paramref name="original"/> is one of <paramref name="roles"/> -
    /// the original role counts too, so recruiting a member away doesn't save them.
    /// </summary>
    public bool IsHolder(
        ICollection<ProtoId<CESecretRolePrototype>> roles,
        ProtoId<CESecretRolePrototype> role,
        ProtoId<CESecretRolePrototype> original)
    {
        return roles.Contains(role) || roles.Contains(original);
    }

    /// <summary>
    /// Average survival of <paramref name="bodies"/>, or average elimination if <paramref name="invert"/>.
    /// Leaving the round counts for both. No bodies at all counts as fully done for an inverted objective
    /// and as failed otherwise.
    /// </summary>
    public float GetProgress(HashSet<EntityUid> bodies, bool invert)
    {
        if (bodies.Count == 0)
            return invert ? 1f : 0f;

        var total = 0f;
        foreach (var body in bodies)
        {
            total += invert ? _objectives.GetEliminationProgress(body) : _objectives.GetSurvivalProgress(body);
        }

        return total / bodies.Count;
    }
}
