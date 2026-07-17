using System.Diagnostics.CodeAnalysis;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Sponsor;

public interface ICESponsorManager
{
    public void Initialize();

    public bool UserHasFeature(NetUserId userId,
        ProtoId<CESponsorFeaturePrototype> feature,
        bool ifDisabledSponsorship = true);

    public bool TryGetSponsorOOCColor(NetUserId userId, [NotNullWhen(true)] out Color? color);
}

public sealed class CESponsorRoleUpdate : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public ProtoId<CESponsorRolePrototype> Role { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Role = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Role);
    }
}