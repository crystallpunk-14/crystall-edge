using Content.Client.GameTicking.Managers;
using Content.Shared;
using Content.Shared._CE.Light;
using Content.Shared.Light.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client._CE.Light;

/// <summary>
/// Overrides <see cref="Content.Client.Light.LightCycleSystem"/>'s ambient light color for maps that have a
/// <see cref="CELightCycleComponent"/>, replacing the vanilla sine-wave formula with a cyclic color gradient.
/// Runs after the vanilla system so it always gets the final say on <see cref="MapLightComponent.AmbientLightColor"/>.
/// </summary>
public sealed partial class CELightCycleSystem : CESharedLightCycleSystem
{
    [Dependency] private ClientGameTicker _ticker = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(Content.Client.Light.LightCycleSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = AllEntityQuery<CELightCycleComponent, LightCycleComponent, MapLightComponent>();
        while (query.MoveNext(out var uid, out var ceCycle, out var cycle, out var map))
        {
            if (!ceCycle.Running)
                continue;

            var pausedTime = _metadata.GetPauseTime(uid);

            var time = (float) _timing.CurTime
                .Add(cycle.Offset)
                .Subtract(_ticker.RoundStartTimeSpan)
                .Subtract(pausedTime)
                .TotalSeconds;

            map.AmbientLightColor = GetColor(ceCycle, cycle.Duration, time);
        }
    }
}
