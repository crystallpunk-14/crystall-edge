using Content.Shared._CE.Achievements;
using Content.Shared._CE.Achievements.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Achievements;

public sealed partial class CEAchievementsRequirementManager : ICEAchievementsRequirementManager
{
    [Dependency] private IEntityManager _entityManager = default!;

    public void Initialize()
    {
    }

    public HashSet<string> PlayerAchievements { get; } = new();
    public Dictionary<string, float> AchievementPercentages { get; } = new();
    public bool DataLoaded => true;
    public event Action? AchievementsUpdated
    {
        add { }
        remove { }
    }

    public bool HasAchievement(NetUserId userId, ProtoId<CEAchievementPrototype> achievement)
    {
        return _entityManager.System<CEAchievementsSystem>().HasCachedAchievement(userId, achievement);
    }
}
