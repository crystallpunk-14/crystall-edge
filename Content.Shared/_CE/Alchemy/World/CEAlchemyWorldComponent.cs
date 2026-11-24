using System.Numerics;
using Content.Shared._CE.Alchemy.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Alchemy.World;

/// <summary>
/// The brewing map of the round: positions of carriers and positions and sizes of zones, laid out by the round seed.
/// Lives on a single nullspace entity visible to every client. Anything missing here falls back
/// to the base position from its prototype, so the analysis works even without this entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEAlchemyWorldComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Seed;

    /// <summary>
    /// Final position of a reagent's <c>CoordinatePoint</c>. One carrier point per reagent is supported.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<ReagentPrototype>, Vector2> Carriers = new();

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEAlchemyZonePrototype>, Vector2> Zones = new();

    /// <summary>
    /// Final radius of a zone, grown by the round layout.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEAlchemyZonePrototype>, float> Radii = new();
}
