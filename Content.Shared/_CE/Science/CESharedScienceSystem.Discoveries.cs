using Content.Shared._CE.Science.Prototypes;
using Content.Shared._CE.Skill;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedScienceSystem
{
    [Dependency] private CESharedSkillSystem _skill = default!;

    /// <summary>
    /// Whether <paramref name="actor"/> has already personally learned the skill this discovery
    /// grants.
    /// </summary>
    public bool IsDiscoveryKnown(EntityUid actor, CEScienceDiscoveryPrototype discovery)
    {
        return _skill.HaveSkill(actor, discovery.Skill);
    }

    /// <summary>
    /// Whether <paramref name="actor"/> currently satisfies this discovery's gating requirements,
    /// independent of whether they already know it.
    /// </summary>
    public bool DiscoveryRequirementsMet(EntityUid actor, CEScienceDiscoveryPrototype discovery)
    {
        return _skill.SkillConditionsMet(actor, discovery.Skill);
    }

    /// <summary>
    /// Counts how many non-abstract discoveries in <paramref name="area"/> <paramref name="actor"/>
    /// already knows versus still has left to unlock (requirements met, not yet known). Used to show
    /// per-area study progress and detect a fully studied area (Unlockable == 0).
    /// </summary>
    public (int Known, int Unlockable) GetAreaProgress(EntityUid actor, ProtoId<CEScienceAreaPrototype> area)
    {
        var known = 0;
        var unlockable = 0;

        foreach (var discovery in _proto.EnumeratePrototypes<CEScienceDiscoveryPrototype>())
        {
            if (discovery.Abstract || discovery.Area != area)
                continue;

            if (IsDiscoveryKnown(actor, discovery))
                known++;
            else if (DiscoveryRequirementsMet(actor, discovery))
                unlockable++;
        }

        return (known, unlockable);
    }
}
