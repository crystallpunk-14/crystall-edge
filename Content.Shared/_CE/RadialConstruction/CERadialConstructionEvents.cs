using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.RadialConstruction;

[Serializable, NetSerializable]
public sealed class CERadialConstructionMessage(EntProtoId protoId) : BoundUserInterfaceMessage
{
    public EntProtoId ProtoId = protoId;
}

/// <summary>
/// Which of an entity's <see cref="CERadialConstructionComponent.Variants"/> the menu should currently show -
/// determined by the server from the item that was used to open it.
/// </summary>
[Serializable, NetSerializable]
public sealed class CERadialConstructionBuiState(List<EntProtoId> availablePrototypes) : BoundUserInterfaceState
{
    public List<EntProtoId> AvailablePrototypes = availablePrototypes;
}

[Serializable, NetSerializable]
public enum CERadialConstructionUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class CERadialConstructionFinishedEvent : SimpleDoAfterEvent
{
    public EntProtoId TargetPrototype;

    public CERadialConstructionFinishedEvent(EntProtoId targetPrototype)
    {
        TargetPrototype = targetPrototype;
    }
}
