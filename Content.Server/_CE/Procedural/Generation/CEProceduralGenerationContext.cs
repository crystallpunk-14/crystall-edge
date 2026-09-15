using System.Threading;
using System.Threading.Tasks;
using Content.Server.Decals;
using Content.Server.Parallax;
using Content.Shared.Light.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation;

/// <summary>
/// Everything an <see cref="ICEProceduralLayer"/> needs to run, bundled so the interface method
/// takes one parameter regardless of what a given layer actually uses. Also the context passed to
/// demiplane location generators — they share this one type rather than each subsystem defining its
/// own field-identical copy. A record so a modifier or a per-level pass can decorrelate itself with
/// e.g. <c>context with { Seed = context.Seed + offset }</c> instead of a whole new context.
/// </summary>
public sealed record CEProceduralGenerationContext(
    IEntityManager EntityManager,
    MapSystem Map,
    BiomeSystem Biome,
    IPrototypeManager Prototype,
    ITileDefinitionManager TileDefManager,
    DecalSystem Decals,
    SharedRoofSystem Roof,
    int Seed,
    int Difficulty,
    Func<ValueTask> Suspend,
    CancellationToken Cancellation);
