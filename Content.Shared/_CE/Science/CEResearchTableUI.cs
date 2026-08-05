using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science;

[Serializable, NetSerializable]
public enum CEResearchTableUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CEResearchTableMergeAspectsMessage(
    ProtoId<CEMagicEssenceTypePrototype> first,
    ProtoId<CEMagicEssenceTypePrototype> second) : BoundUserInterfaceMessage
{
    public readonly ProtoId<CEMagicEssenceTypePrototype> First = first;
    public readonly ProtoId<CEMagicEssenceTypePrototype> Second = second;
}
