using System.Linq;
using Content.Server._CE.EssenceBurner.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Power.Components;
using Robust.Shared.Timing;

namespace Content.Server._CE.EssenceBurner.Systems;

/// <summary>
/// Ticks <see cref="CEMagicEssenceBurnerComponent"/>: burns its solution regardless of power, converting
/// magic essence reagent into battery charge and anything else into instability buildup.
/// </summary>
public sealed partial class CEMagicEssenceBurnerSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private CEMagicEssenceSystem _magicEssence = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEMagicEssenceBurnerComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var burner, out var battery))
        {
            if (_timing.CurTime < burner.NextUpdate)
                continue;

            burner.NextUpdate = _timing.CurTime + burner.UpdateFrequency;
            var dt = (float)burner.UpdateFrequency.TotalSeconds;

            burner.Instability = MathF.Max(0f, burner.Instability - burner.InstabilityDecayRate * dt);

            if (_solutionContainer.TryGetSolution((uid, null), burner.Solution, out var soln, out var solution))
            {
                var remaining = FixedPoint2.Min(burner.BurnRate * dt, solution.Volume);

                foreach (var reagentQuantity in solution.Contents.ToArray())
                {
                    if (remaining <= FixedPoint2.Zero)
                        break;

                    var amount = FixedPoint2.Min(reagentQuantity.Quantity, remaining);
                    if (amount <= FixedPoint2.Zero)
                        continue;

                    _solutionContainer.RemoveReagent(soln.Value, reagentQuantity.Reagent, amount);
                    remaining -= amount;

                    if (_magicEssence.TryGetEssenceFromReagent(reagentQuantity.Reagent.Prototype, out _))
                        _battery.ChangeCharge((uid, battery), burner.EnergyPerUnit * amount.Float());
                    else
                        burner.Instability += burner.InstabilityPerUnit * amount.Float();
                }
            }

            if (burner.Instability >= burner.MaxInstability)
            {
                burner.Instability = 0f;
                Spawn(burner.ExplosionMishap, Transform(uid).Coordinates);
            }
        }
    }
}
