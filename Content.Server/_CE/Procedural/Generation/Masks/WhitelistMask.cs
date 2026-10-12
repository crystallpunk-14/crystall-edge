using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.Procedural.Generation.Masks;

/// <summary>
/// Matches tiles with an anchored entity passing <see cref="Whitelist"/> — e.g. inverted with the Wall component,
/// "no walls here" while trees and other clutter don't count.
/// </summary>
public sealed partial class WhitelistMask : ICETileMask
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    public override bool Matches(CEProceduralGenerationContext context, EntityUid map, MapGridComponent grid, Vector2i tile, Tile currentTile)
    {
        var whitelist = context.EntityManager.System<EntityWhitelistSystem>();
        var anchored = context.Map.GetAnchoredEntitiesEnumerator(map, grid, tile);
        while (anchored.MoveNext(out var ent))
        {
            if (whitelist.IsValid(Whitelist, ent.Value))
                return true;
        }

        return false;
    }
}
