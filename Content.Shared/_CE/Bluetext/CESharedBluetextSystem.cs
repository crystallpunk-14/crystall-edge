using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.BlueText;

public abstract class CESharedBlueTextSystem : EntitySystem
{
    public const int MaxTextLength = 1000;
}

/// <summary>
/// Message sent from client to server to save the blue text inputted by the player.
/// </summary>
public sealed class CEBlueTextSaveMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public string Text = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Text = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Text);
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
}
