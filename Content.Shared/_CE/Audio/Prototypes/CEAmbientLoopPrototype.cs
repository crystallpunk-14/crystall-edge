using Content.Shared.EntityConditions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Audio.Prototypes;

/// <summary>
/// Attaches entity conditions to sound files to play ambience.
/// </summary>
[Prototype("ambientLoop")]
public sealed partial class CEAmbientLoopPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public EntityCondition[]? Conditions;
}
