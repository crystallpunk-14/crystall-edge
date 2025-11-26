using Content.Shared._CE.Ambitions.Conditions;
using Content.Shared._CE.Ambitions.Parsings;

namespace Content.Shared._CE.Ambitions.Components;

/// <summary>
/// This component is used to mark the EntProtoId objective as available for procedural ambition generation
/// and subsequent configuration in the character.
/// </summary>
[RegisterComponent, Access(typeof(CESharedAmbitionsSystem))]
public sealed partial class CEAmbitionObjectiveComponent : Component
{
    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Desc;

    [DataField]
    public Dictionary<string, CEAmbitionParsing> Parsings = new();

    [DataField]
    public List<CEAmbitionCondition> Conditions = new();

    [DataField]
    public float Weight = 1f;
}
