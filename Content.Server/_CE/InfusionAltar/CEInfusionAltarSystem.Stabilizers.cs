using Content.Shared._CE.InfusionAltar.Components;
using Robust.Shared.Map;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;

    private const float PairedStabilizationWeight = 0.05f;
    private const float UnpairedStabilizationPenalty = 0.02f;
    private const float MinStabilizerFactor = 0.1f;

    /// <summary>
    /// Periodically scans the area around the altar for loose <see cref="CEInfusionAltarStabilizerComponent"/>
    /// items (never placed on sub-pedestals) and recomputes <see cref="CEInfusionAltarComponent.StabilizerFactor"/>.
    /// For each pair of antipodal tile offsets, only the symmetric (matched) portion of the two sides'
    /// stabilizing power dampens instability growth; any unmatched remainder instead slightly raises it.
    /// </summary>
    private void ScanStabilizers(Entity<CEInfusionAltarComponent> ent)
    {
        var altar = ent.Comp;

        if (!TryGetTile(ent.Owner, out var altarGrid, out var altarTile))
        {
            altar.StabilizerFactor = 1f;
            return;
        }

        var altarCoords = Transform(ent.Owner).Coordinates;
        var stabilizers = _lookup.GetEntitiesInRange<CEInfusionAltarStabilizerComponent>(altarCoords, altar.StabilizerScanRadius);

        var byOffset = new Dictionary<Vector2i, float>();
        foreach (var stabilizer in stabilizers)
        {
            if (!TryGetTile(stabilizer.Owner, out var grid, out var tile) || grid != altarGrid)
                continue;

            var offset = tile - altarTile;
            byOffset[offset] = byOffset.GetValueOrDefault(offset) + stabilizer.Comp.StabilizingPower;
        }

        var factor = 1f;
        var handled = new HashSet<Vector2i>();

        foreach (var (offset, amount) in byOffset)
        {
            if (!handled.Add(offset))
                continue;

            var antipode = -offset;
            handled.Add(antipode);

            var opposite = byOffset.GetValueOrDefault(antipode);
            var paired = MathF.Min(amount, opposite);
            var excess = MathF.Abs(amount - opposite);

            factor -= paired * PairedStabilizationWeight;
            factor += excess * UnpairedStabilizationPenalty;
        }

        altar.StabilizerFactor = MathF.Max(factor, MinStabilizerFactor);
    }
}
