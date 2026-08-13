using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.StationEvents;

/// <summary>
/// Station event component that breaks a random big pipe together with all pipes found nearby within a radius.
/// </summary>
[RegisterComponent]
public sealed partial class CEPipeBreakageRuleComponent : Component
{
    [DataField]
    public Dictionary<EntProtoId, EntProtoId> ReplacementMap = new();

    [DataField(required: true)]
    public EntProtoId CenterPrototype;

    [DataField]
    public float Radius = 5f;

    [DataField]
    public float BreakChance = 0.8f;

    [DataField]
    public EntProtoId? CenterVfx;

    [DataField]
    public SoundSpecifier? BreakSound;
}
