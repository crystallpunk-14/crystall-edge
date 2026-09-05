using Content.Shared._CE.Achievements.Prototypes;
using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Achievements;

/// <summary>
/// Sent from server to client with the player's achievements and global achievement percentages.
/// </summary>
[UsedImplicitly]
public sealed class CEMsgAllAchievementsData : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    /// <summary>
    /// Achievement prototype IDs that the current player has earned.
    /// </summary>
    public HashSet<string> PlayerAchievements = new();

    /// <summary>
    /// Percentage of all players who have each achievement (0–100).
    /// Key = achievement prototype ID, Value = percentage.
    /// </summary>
    public Dictionary<string, float> AchievementPercentages = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var playerCount = buffer.ReadVariableInt32();
        PlayerAchievements.EnsureCapacity(playerCount);

        for (var i = 0; i < playerCount; i++)
        {
            PlayerAchievements.Add(buffer.ReadString());
        }

        var percentCount = buffer.ReadVariableInt32();

        for (var i = 0; i < percentCount; i++)
        {
            var id = buffer.ReadString();
            var percent = buffer.ReadFloat();
            AchievementPercentages[id] = percent;
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(PlayerAchievements.Count);

        foreach (var id in PlayerAchievements)
        {
            buffer.Write(id);
        }

        buffer.WriteVariableInt32(AchievementPercentages.Count);

        foreach (var (id, percent) in AchievementPercentages)
        {
            buffer.Write(id);
            buffer.Write(percent);
        }
    }
}

[Serializable, NetSerializable]
public sealed class CEAchievementUnlockedEvent(ProtoId<CEAchievementPrototype> achievement, float percentage)
    : EntityEventArgs
{
    public ProtoId<CEAchievementPrototype> AchievementProtoId = achievement;
    public float Percentage = percentage;
}
