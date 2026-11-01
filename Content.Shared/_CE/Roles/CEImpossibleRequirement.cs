using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Never satisfied: locks content that is still in development.
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class CEImpossibleRequirement : JobRequirement
{
    public override bool Check(NetUserId? userId,
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("ce-role-req-impossible"));
        return false;
    }
}
