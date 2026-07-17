using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.Sponsor;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Requires the player to have (via TypeAuth) a Discord sponsor role meeting a minimum priority.
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class CESponsorFeatureRequired : JobRequirement
{
    [DataField(required: true)]
    public ProtoId<CESponsorFeaturePrototype> Feature = string.Empty;

    public override bool Check(NetUserId? userId,
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        if (userId is null)
            return false;

        var sponsorship = IoCManager.Resolve<ICESponsorManager>();

        var haveFeature = sponsorship.UserHasFeature(userId.Value, Feature);

        if (haveFeature)
            return true;

        var prototypeMan = IoCManager.Resolve<IPrototypeManager>();

        var indexedFeature = prototypeMan.Index(Feature);
        var lowestRole = GetLowestPriorityRole(indexedFeature.MinPriority, prototypeMan);
        prototypeMan.TryIndex(lowestRole, out var indexedRole);

        if (indexedRole == null)
            return false;
        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("ce-role-req-sponsor-feature-req", ("role", indexedRole.Name)));

        return false;
    }

    public ProtoId<CESponsorRolePrototype>? GetLowestPriorityRole(float priority, IPrototypeManager protoMan)
    {
        ProtoId<CESponsorRolePrototype>? lowestRole = null;
        var lowestPriority = float.MaxValue;

        foreach (var role in protoMan.EnumeratePrototypes<CESponsorRolePrototype>())
        {
            if (!role.Examinable)
                continue;

            if (role.Priority >= priority && role.Priority < lowestPriority)
            {
                lowestPriority = role.Priority;
                lowestRole = role.ID;
            }
        }

        return lowestRole;
    }
}
