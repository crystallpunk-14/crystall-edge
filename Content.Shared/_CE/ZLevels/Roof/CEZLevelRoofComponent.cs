/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels.Roof;

/// <summary>
/// Marks a mapGrid entity (supports both planetmap + grids) for automatic roof management driven by tile changes in the Z-level system.
/// Systems use this marker to add, update, or remove roof tiles when the underlying tiles change.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEZLevelRoofComponent : Component
{
    public const int ChunkSize = 8;

    /// <summary>
    /// Chunk origin and bitmask of tiles shielded from weather by a tile with
    /// <see cref="Content.Shared.Maps.ContentTileDefinition.BlocksWeather"/> on any z-level above.
    /// Unlike the shadow roof, transparent tiles (glass) still block weather.
    /// Not a DataField: regenerated from the z-network on load.
    /// </summary>
    [AutoNetworkedField]
    public Dictionary<Vector2i, ulong> WeatherData = new();
}
