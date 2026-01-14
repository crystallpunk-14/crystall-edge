using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.RadialConstruction;

[Serializable, NetSerializable]
public sealed class CERadialConstructionMessage(EntProtoId protoId) : BoundUserInterfaceMessage
{
    public EntProtoId ProtoId = protoId;
}

[Serializable, NetSerializable]
public enum CERadialConstructionUiKey : byte
{
    Key
}
