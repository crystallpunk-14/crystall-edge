using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// Matches tiles already flagged roofed by the upstream Roof system — the same per-tile flag that
/// blocks weather and outdoor lighting (<see cref="RoofComponent"/> / <see cref="SharedRoofSystem"/>).
/// A map that hasn't had a roof computed for it yet (nothing above it, or generated standalone
/// before ever joining a z-network) just reads as "not roofed" everywhere.
/// </summary>
public sealed partial class CERoofedMask : ICETileMask
{
    public override bool Matches(CEProceduralGenerationContext context, EntityUid map, MapGridComponent grid, Vector2i tile, Tile currentTile)
    {
        if (!context.EntityManager.TryGetComponent<RoofComponent>(map, out var roof))
            return false;

        return context.Roof.IsRooved((map, grid, roof), tile);
    }
}
