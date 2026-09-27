using Robust.Shared.Prototypes;

namespace Content.Server._CE.StationEvents;

/// <summary>
/// Generic station event component: spawns <see cref="Prototype"/> somewhere within the Lucson
/// Sphere's murk radius, on any z-level it reaches - see <see cref="CEInLucsonSphereSpawnRuleSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEInLucsonSphereSpawnRuleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Prototype;
}
