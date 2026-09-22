using Robust.Shared.Prototypes;

namespace Content.Server._CE.StationEvents;

/// <summary>
/// Generic station event component: spawns <see cref="Prototype"/> at the first free tile found
/// scanning down from the topmost z-level of a random column in the station's z-map network - see
/// <see cref="CETopTileSpawnRuleSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CETopTileSpawnRuleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype;
}
