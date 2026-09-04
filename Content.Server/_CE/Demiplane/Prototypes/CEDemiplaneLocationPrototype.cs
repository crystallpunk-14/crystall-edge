using Content.Server._CE.Demiplane.Generation;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Prototypes;

/// <summary>
/// A single demiplane stage the city can jump to. Name and difficulty range, plus which
/// <see cref="ICEDemiplaneLocationGenerator"/> produces its geometry — a server-only concern
/// (generation spawns real maps/grids, never something a client should run), so this whole
/// prototype lives server-side rather than splitting fields across Shared/Server.
/// </summary>
[Prototype("demiplaneLocation")]
public sealed partial class CEDemiplaneLocationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Difficulty range this location can be picked for.
    /// </summary>
    [DataField(required: true)]
    public MinMax Difficulty = new(0, 10);

    [DataField(required: true)]
    public ICEDemiplaneLocationGenerator Generator = default!;

    /// <summary>
    /// Ambient components (parallax, lighting, day-cycle, etc.) this location gives the whole
    /// z-network while the city is here — applied to every map in the network when the location
    /// is entered, and removed from every map in the network when it's left. See
    /// <see cref="CEDemiplaneSystem"/>.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();
}