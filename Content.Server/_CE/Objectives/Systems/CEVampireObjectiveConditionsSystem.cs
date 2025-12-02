using Content.Server._CE.Objectives.Components;
using Content.Server.Station.Components;
using Content.Shared._CE.Vampire.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles.Jobs;

namespace Content.Server._CE.Objectives.Systems;

public sealed class CEVampireObjectiveConditionsSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;

    public readonly float RequiredAlivePercentage = 0.5f;
    public readonly int RequiredHeartLevel = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEVampireBloodPurityConditionComponent, ObjectiveAfterAssignEvent>(OnBloodPurityAfterAssign);
        SubscribeLocalEvent<CEVampireBloodPurityConditionComponent, ObjectiveGetProgressEvent>(OnBloodPurityGetProgress);
    }

    public float CalculateAlivePlayersPercentage()
    {
        var query = EntityQueryEnumerator<StationJobsComponent>();

        var totalPlayers = 0f;
        var alivePlayers = 0f;
        while (query.MoveNext(out var uid, out var jobs))
        {
            totalPlayers += jobs.PlayerJobs.Count;

            foreach (var (netUserId, jobsList) in jobs.PlayerJobs)
            {
                if (!_mind.TryGetMind(netUserId, out var mind))
                    continue;

                if (!_jobs.MindTryGetJob(mind, out var jobRole))
                    continue;

                var firstMindEntity = GetEntity(mind.Value.Comp.OriginalOwnedEntity);

                if (firstMindEntity is null)
                    continue;

                if (!_mobState.IsDead(firstMindEntity.Value))
                    alivePlayers++;
            }
        }

        return totalPlayers > 0 ? (alivePlayers / totalPlayers) : 0f;
    }

    private void OnBloodPurityAfterAssign(Entity<CEVampireBloodPurityConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
         _meta.SetEntityName(ent, Loc.GetString("ce-objective-vampire-pure-blood-title"));
         _meta.SetEntityDescription(ent, Loc.GetString("ce-objective-vampire-pure-blood-desc"));
         _objectives.SetIcon(ent, ent.Comp.Icon);
    }

    private void OnBloodPurityGetProgress(Entity<CEVampireBloodPurityConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var query = EntityQueryEnumerator<CEVampireComponent>();

        var progress = 1f;
        while (query.MoveNext(out var uid, out var vampire))
        {
            if (uid == args.Mind.OwnedEntity)
                continue;

            progress = 0;
            break;
        }

        args.Progress = progress;
    }
}
