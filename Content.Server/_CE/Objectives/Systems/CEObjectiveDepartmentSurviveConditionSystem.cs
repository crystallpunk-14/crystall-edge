using Content.Server._CE.Objectives.Components;
using Content.Server._CE.Roles;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveDepartmentSurviveConditionComponent"/> - the average
/// survival of every body that held a role from the department: alive = 1, critical = 0.5, dead or deleted = 0.
/// </summary>
public sealed partial class CEObjectiveDepartmentSurviveConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private IPrototypeManager _proto = default!;

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
        var query = EntityQueryEnumerator<CEObjectiveDepartmentSurviveConditionComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _objectives.RefreshObjectiveProgress(uid);
        }
    }

    /// <summary>
    /// Adds every existing body that holds one of <paramref name="roles"/>, as its current or original
    /// role, to <paramref name="bodies"/> - for objectives created after the roles were handed out.
    /// </summary>
    public void CollectBodies(HashSet<EntityUid> bodies, ICollection<ProtoId<CESecretRolePrototype>> roles)
    {
        var query = EntityQueryEnumerator<CESecretRoleIconComponent>();
        while (query.MoveNext(out var uid, out var icon))
        {
            var original = CompOrNull<CESecretRoleOriginalIconComponent>(uid)?.Role ?? icon.Role;
            if (IsHolder(roles, icon.Role, original))
                bodies.Add(uid);
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
    /// Average survival of <paramref name="bodies"/>, flipped if <paramref name="invert"/>. No bodies at
    /// all counts as fully done for an inverted objective and as failed otherwise.
    /// </summary>
    public float GetProgress(HashSet<EntityUid> bodies, bool invert)
    {
        if (bodies.Count == 0)
            return invert ? 1f : 0f;

        var survival = 0f;
        foreach (var body in bodies)
        {
            survival += GetBodySurvival(body);
        }

        var average = survival / bodies.Count;
        return invert ? 1f - average : average;
    }

    private float GetBodySurvival(EntityUid body)
    {
        if (TerminatingOrDeleted(body))
            return 0f;

        if (!TryComp<MobStateComponent>(body, out var mobState))
            return 1f;

        return mobState.CurrentState switch
        {
            MobState.Alive => 1f,
            MobState.Critical => 0.5f,
            MobState.Dead => 0f,
            _ => 1f,
        };
    }
}
