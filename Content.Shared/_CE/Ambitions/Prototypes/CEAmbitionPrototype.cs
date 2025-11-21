using Content.Shared._CE.Ambitions.Parsings;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Ambitions.Prototypes;

/// <summary>
///
/// </summary>
[Prototype("ambition")]
public sealed class CEAmbitionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Desc;

    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/examine-star.png"));

    [DataField]
    public Dictionary<string, CEAmbitionParsing> Parsings = new();
}
