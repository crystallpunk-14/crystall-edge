using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using Robust.Shared.Configuration;

namespace Content.Server._CE.GameRules.Systems;

/// <summary>
///
/// </summary>
public sealed class CEGameRulesSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly JobSystem _job = default!;

    public override void Initialize()
    {
    }

    public Entity<MindComponent>? GetPlayer(List<MindFilter> filters)
    {
        return _mind.PickFromPool(new AliveHumansPool(), filters);
    }
}
