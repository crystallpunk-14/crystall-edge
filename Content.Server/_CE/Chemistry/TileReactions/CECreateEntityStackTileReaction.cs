using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server._CE.Chemistry.TileReactions;

/// <summary>
///     Like <see cref="Content.Server.Chemistry.TileReactions.CreateEntityTileReaction"/>, but spawns as many entities
///     as the poured volume allows for (reactVolume / Usage) in one go, and fully consumes the poured volume
///     instead of only ever spawning a single entity for 1u regardless of how much was poured.
/// </summary>
[DataDefinition]
public sealed partial class CECreateEntityStackTileReaction : ITileReaction
{
    [DataField(required: true)]
    public EntProtoId Entity;

    [DataField]
    public FixedPoint2 Usage = FixedPoint2.New(1);

    /// <summary>
    ///     How many of the whitelisted entity can fit on one tile?
    /// </summary>
    [DataField]
    public int MaxOnTile = int.MaxValue;

    /// <summary>
    ///     The whitelist to use when determining what counts as "max entities on a tile".
    /// </summary>
    [DataField("maxOnTileWhitelist")]
    public EntityWhitelist? Whitelist;

    [DataField]
    public float RandomOffsetMax = 0.0f;

    public FixedPoint2 TileReact(TileRef tile,
        ReagentPrototype reagent,
        FixedPoint2 reactVolume,
        IEntityManager entityManager,
        List<ReagentData>? data)
    {
        if (reactVolume < Usage)
            return FixedPoint2.Zero;

        var count = (int) (reactVolume / Usage);
        var consumeAll = true;

        if (Whitelist != null)
        {
            var lookup = entityManager.System<EntityLookupSystem>();
            var whitelistSystem = entityManager.System<EntityWhitelistSystem>();

            var existing = 0;
            foreach (var ent in lookup.GetEntitiesInTile(tile, LookupFlags.Static))
            {
                if (whitelistSystem.IsWhitelistPass(Whitelist, ent))
                    existing += 1;
            }

            var allowed = MaxOnTile - existing;
            if (allowed <= 0)
                return FixedPoint2.Zero;

            if (allowed < count)
            {
                count = allowed;
                consumeAll = false; // some of the poured reagent didn't fit on the tile, leave it for later
            }
        }

        var random = IoCManager.Resolve<IRobustRandom>();
        var center = entityManager.System<TurfSystem>().GetTileCenter(tile);

        for (var i = 0; i < count; i++)
        {
            var xoffs = random.NextFloat(-RandomOffsetMax, RandomOffsetMax);
            var yoffs = random.NextFloat(-RandomOffsetMax, RandomOffsetMax);
            var pos = center.Offset(new Vector2(xoffs, yoffs));
            entityManager.SpawnEntity(Entity, pos);
        }

        return consumeAll ? reactVolume : Usage * count;
    }
}
