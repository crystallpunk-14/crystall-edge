using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class CEResearchMapTile
{
    [DataField]
    public bool DeadZone;

    /// <summary>Set for target and placed tiles; unused when <see cref="DeadZone"/> is true.</summary>
    [DataField]
    public ProtoId<CEMagicEssenceTypePrototype>? Aspect;

    /// <summary>True for a discovery's fixed target aspect; false for a player-placed one.</summary>
    [DataField]
    public bool Fixed;
}
