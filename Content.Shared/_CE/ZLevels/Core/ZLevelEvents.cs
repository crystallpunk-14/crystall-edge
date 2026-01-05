using Robust.Shared.Serialization;

namespace Content.Shared._CE.ZLevels.Core.EntitySystems;

/// <summary>
/// Sent by the client to request changing the currently viewed Z-layer level.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangeViewedZLayerEvent
(NetEntity? target, int newValue)
 : EntityEventArgs
{
    public readonly NetEntity? Target = target;
    public int NewValue = newValue;
}
