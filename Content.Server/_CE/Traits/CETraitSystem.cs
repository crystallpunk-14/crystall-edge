using System.Linq;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Traits;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Traits;

/// <summary>
/// Grants a random part of the character's lobby traits on spawn: every picked trait rolls
/// independently with <see cref="CETraitRules.GrantChance"/>.
/// </summary>
public sealed partial class CETraitSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private CESharedSkillSystem _skill = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public static readonly LocId BadgeName = "ce-trait-badge";
    private static readonly LocId BadgeTooltip = "ce-trait-badge-tooltip";
    private static readonly Color BadgeColor = Color.FromHex("#D4A64A");

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId is null ||
            !_proto.TryIndex<JobPrototype>(args.JobId, out var job) ||
            !job.ApplyTraits)
            return;

        if (!CanReceiveTraits(args.Player, args.Profile))
            return;

        foreach (var trait in RollTraits(args.Profile.TraitPreferences))
        {
            ApplyTrait(args.Mob, trait);
        }
    }

    /// <summary>
    /// Whether the player's trait selection may be granted at all. Any violation - unknown or unlocked
    /// traits, broken balance, conflicts - voids the whole selection, so it can't be tilted to a positive sum.
    /// </summary>
    public bool CanReceiveTraits(ICommonSession session, HumanoidCharacterProfile profile)
    {
        var traits = profile.TraitPreferences.ToList();
        if (!CETraitRules.IsSelectionValid(traits, _proto))
            return false;

        _playTimeTracking.TryGetTrackerTimes(session, out var playTimes);
        playTimes ??= new Dictionary<string, TimeSpan>();

        foreach (var trait in traits)
        {
            if (!JobRequirements.TryRequirementsMet(_proto.Index(trait).Requirements,
                    playTimes,
                    out _,
                    EntityManager,
                    _proto,
                    profile,
                    session.UserId))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Picks the traits granted this spawn: each one independently with <see cref="CETraitRules.GrantChance"/>.
    /// </summary>
    public List<ProtoId<CETraitPrototype>> RollTraits(IEnumerable<ProtoId<CETraitPrototype>> traits)
    {
        var rolled = new List<ProtoId<CETraitPrototype>>();
        foreach (var trait in traits)
        {
            if (_random.Prob(CETraitRules.GrantChance))
                rolled.Add(trait);
        }

        return rolled;
    }

    /// <summary>
    /// Grants the trait's skill (badged as a trait) and puts its gear into the mob's hands.
    /// </summary>
    public void ApplyTrait(EntityUid mob, ProtoId<CETraitPrototype> trait)
    {
        if (!_proto.Resolve(trait, out var indexed))
            return;

        _skill.TryAddSkill(mob,
            indexed.Skill,
            descriptor: new CESkillDescriptor
            {
                Name = BadgeName,
                Color = BadgeColor,
                Tooltip = BadgeTooltip,
            });

        if (indexed.Gear is not { } gear)
            return;

        var item = Spawn(gear, Transform(mob).Coordinates);
        _hands.TryPickupAnyHand(mob, item, checkActionBlocker: false);
    }
}
