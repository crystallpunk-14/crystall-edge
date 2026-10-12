using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Wildlands.Components;

/// <summary>
/// Generates <see cref="Config"/> around the station once its z-network is loaded, before players spawn.
/// </summary>
[RegisterComponent, Access(typeof(CEWildlandsSystem))]
public sealed partial class CEStationWildlandsComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CEWildlandsConfigPrototype> Config;
}
