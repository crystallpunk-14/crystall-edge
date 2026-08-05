using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science.Components;

/// <summary>
/// Marks the singleton nullspace entity holding round-wide science data.
/// Spawned by <see cref="CEScienceSystem"/> on round start; duplicates are deleted on MapInit.
/// </summary>
[RegisterComponent]
public sealed partial class CEScienceComponent : Component
{
    /// <summary>
    /// Discovery prototypes not currently offered on any star's unresolved offer, drawable when
    /// rolling a new offer
    /// </summary>
    public HashSet<ProtoId<CEScienceDiscoveryPrototype>> AvailableDiscoveries = new();

    /// <summary>
    /// Discovery prototypes a player has permanently placed on the map by choosing their card.
    /// Excluded forever from <see cref="AvailableDiscoveries"/> refills.
    /// </summary>
    public HashSet<ProtoId<CEScienceDiscoveryPrototype>> ChosenDiscoveries = new();
}
