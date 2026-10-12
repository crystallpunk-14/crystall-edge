using Content.Server.Decals;
using Content.Server.Parallax;
using Content.Shared.Light.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Procedural.Generation;

/// <summary>
/// Everything a procedural layer needs; Region limits which tiles it may touch.
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
    Vector2i Center,
    CEProceduralRegion Region);

/// <summary>
/// Either the inside or the outside of a fixed tile set, shared by every z-level of a network.
/// </summary>
public sealed class CEProceduralRegion(HashSet<Vector2i> tiles, bool inside)
{
    public bool Contains(Vector2i tile) => tiles.Contains(tile) == inside;
}
