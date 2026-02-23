using Content.Server._White.Temperature.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Server._White.Temperature.Systems;

public sealed class WhiteSolutionTemperatureSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private readonly TimeSpan _updateTick = TimeSpan.FromSeconds(1f);
    private TimeSpan _timeToNextUpdate = TimeSpan.Zero;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTiming.CurTime <= _timeToNextUpdate)
            return;

        _timeToNextUpdate = _gameTiming.CurTime + _updateTick;

        var query = EntityQueryEnumerator<WhiteSolutionTemperatureComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var temp, out var container))
        {
            foreach (var (_, solution) in _solutionContainer.EnumerateSolutions((uid, container)))
            {
                if (!TryAffectTemp(solution.Comp.Solution.Temperature, temp.StandardTemp, solution.Comp.Solution.Volume, out var newT, power: 0.05f))
                    continue;

                _solutionContainer.SetTemperature(solution, newT);
            }
        }
    }

    private static bool TryAffectTemp(float oldT, float targetT, FixedPoint2 mass, out float newT, float power = 1)
    {
        newT = oldT;

        if (mass == 0)
            return false;

        newT = (float)(oldT + (targetT - oldT) / mass * power);
        return true;
    }
}
