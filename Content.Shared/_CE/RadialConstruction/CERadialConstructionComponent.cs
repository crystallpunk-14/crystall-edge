using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.RadialConstruction;

/// <summary>
/// Component that allows entities to be crafted/constructed from a radial menu
/// when interacted with using a tool that has the required quality.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CERadialConstructionComponent : Component
{
    [DataField]
    public List<EntProtoId> AvailablePrototypes = new();

    [DataField]
    public float Delay = 1;

    [DataField]
    public ProtoId<ToolQualityPrototype> RequiredQuality = "CEHammering";
}
