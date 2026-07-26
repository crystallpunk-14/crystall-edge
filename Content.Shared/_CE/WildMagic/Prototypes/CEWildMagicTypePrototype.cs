using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.WildMagic.Prototypes;

[Prototype("CEWildMagicType")]
public sealed partial class CEWildMagicTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public List<PrototypeLayerData> Icon = new();

    [DataField]
    public float Difficulty = 1f;
}
