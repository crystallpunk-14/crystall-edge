using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicEssence.Components;

/// <summary>
/// Marks an entity as a source of thaumaturgical essence, yielding a random amount of each
/// listed essence type (e.g. when harvested or destroyed).
/// </summary>
[RegisterComponent]
public sealed partial class CEMagicEssenceStructureComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, MinMax> Essences = new();
}
