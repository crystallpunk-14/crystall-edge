using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.Achievements.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Achievements;

/// <summary>
/// Requires a player to have (or not have) a specific achievement.
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class CEAchievementRequirement : JobRequirement
{
    [DataField(required: true)]
    public ProtoId<CEAchievementPrototype> Achievement;

    public override bool Check(NetUserId? userId,
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        var hasAchievement = userId is not null &&
                             IoCManager.Resolve<ICEAchievementsRequirementManager>().HasAchievement(userId.Value, Achievement);

        if (hasAchievement != Inverted)
            return true;

        var name = Achievement.Id;
        var desc = string.Empty;
        if (protoManager.TryIndex(Achievement, out var achievement))
        {
            name = Loc.GetString(achievement.Name);
            desc = Loc.GetString(achievement.Desc);
        }

        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            Inverted ? "ce-role-req-achievement-forbidden" : "ce-role-req-achievement-required",
            ("achievement", name),
            ("desc", desc)));
        return false;
    }
}
