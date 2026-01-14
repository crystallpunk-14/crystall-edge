using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.RadialConstruction;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CERadialConstructionComponent : Component
{
    [DataField]
    public List<EntProtoId> AvailablePrototypes = new();

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public ProtoId<ToolQualityPrototype> RequiredQuality = "Screwing";
}
