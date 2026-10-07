using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Roles;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveRoleCountConditionComponent"/>.
/// </summary>
public sealed partial class CEObjectiveRoleCountConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    [SubscribeLocalEvent]
    private void OnInitialize(Entity<CEObjectiveRoleCountConditionComponent> ent, ref CEInitializeObjectiveEvent args)
    {
        var playersPerHolder = Math.Max(1, ent.Comp.PlayersPerHolder);
        ent.Comp.Required = Math.Clamp(GetActivePlayerCount() / playersPerHolder, ent.Comp.MinRequired, ent.Comp.MaxRequired);

        if (ent.Comp.Title is { } title)
            _metaData.SetEntityName(ent, Loc.GetString(title, ("count", ent.Comp.Required)));
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveRoleCountConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        if (ent.Comp.Required <= 0)
        {
            args.Progress = 1f;
            return;
        }

        var holders = 0;
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (_role.MindHasRole<CESecretRoleComponent>((mindId, mind), out var roleEnt) &&
                roleEnt.Value.Comp2.Role == ent.Comp.Role)
            {
                holders++;
            }
        }

        args.Progress = Math.Min(1f, (float) holders / ent.Comp.Required);
    }

    // Raised whenever someone's objective list changes - which is exactly what happens when a
    // secret role is granted or cleared.
    [SubscribeLocalEvent]
    private void OnObjectivesChanged(ref CEObjectivesChangedEvent args)
    {
        var query = EntityQueryEnumerator<CEObjectiveRoleCountConditionComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _objectives.RefreshObjectiveProgress(uid);
        }
    }

    private int GetActivePlayerCount()
    {
        var count = 0;
        foreach (var session in _player.Sessions)
        {
            if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
                continue;

            if (session.AttachedEntity is not { } uid || HasComp<GhostComponent>(uid))
                continue;

            count++;
        }

        return count;
    }
}
