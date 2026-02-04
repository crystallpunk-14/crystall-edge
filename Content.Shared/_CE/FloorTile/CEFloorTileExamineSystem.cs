using Content.Shared.Examine;
using Content.Shared.Tiles;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.FloorTile;

public sealed class CEFloorTileExamineSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FloorTileComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<FloorTileComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Outputs is null)
            return;

        foreach (var tile in ent.Comp.Outputs)
        {
            if (!_proto.Resolve(tile, out var indexedTile))
                continue;

            var tileName = Loc.GetString(indexedTile.Name);

            var baseName = indexedTile.BaseTurf;
            if (!string.IsNullOrEmpty(baseName) && _proto.TryIndex<ContentTileDefinition>(baseName, out var baseProto))
            {
                baseName = Loc.GetString(baseProto.Name);
            }

            args.PushMarkup(Loc.GetString("ce-floor-tile-examine", ("tileName", tileName), ("baseName", baseName)));
        }
    }
}
