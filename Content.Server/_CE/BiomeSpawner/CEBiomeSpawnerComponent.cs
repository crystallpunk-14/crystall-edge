/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared.Parallax.Biomes;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.BiomeSpawner;

/// <summary>
/// Fills the tile in which it is located with the contents of the biome. Includes: tile, decals and entities
/// </summary>
[RegisterComponent, Access(typeof(CEBiomeSpawnerSystem))]
public sealed partial class CEBiomeSpawnerComponent : Component
{
    [DataField]
    public ProtoId<BiomeTemplatePrototype> Biome = "Grasslands";

    /// <summary>
    /// Entities that we don't remove.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist DeleteBlacklist = new();
}
