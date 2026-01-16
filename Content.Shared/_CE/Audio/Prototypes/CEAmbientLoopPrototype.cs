using Content.Shared.Random.Rules;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Audio.Prototypes;

/// <summary>
/// Attaches a rules prototype to sound files to play ambience.
/// </summary>
[Prototype("ambientLoop")]
public sealed partial class CEAmbientLoopPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField(required: true)]
    public ProtoId<RulesPrototype> Rules = string.Empty;
}
