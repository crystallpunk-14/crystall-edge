using Content.Shared._CE.Ambitions.Parsings;

namespace Content.Shared._CE.Ambitions.Components;

/// <summary>
///
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
}
