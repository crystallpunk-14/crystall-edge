using System.Linq;
using Content.Server._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Weather;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.ZLevels.Weather;

/// <summary>
/// Walks the weather graph of <see cref="CEStationWeatherComponent"/> and applies each node's weather to the station's z-network.
/// </summary>
public sealed partial class CEStationWeatherSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private CEWeatherSystem _weather = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEStationWeatherComponent, CEStationZLevelsComponent>();
        while (query.MoveNext(out var uid, out var weather, out var zLevels))
        {
            if (!weather.Enabled)
                continue;

            if (weather.CurrentNode is not null && _timing.CurTime < weather.NextChangeTime)
                continue;

            if (zLevels.ZNetworkEntity is not { } network || !TryComp<CEZMapNetworkComponent>(network, out var networkComp))
                continue;

            if (weather.Nodes.Count == 0)
                continue;

            var next = PickNextNode((uid, weather));
            var node = weather.Nodes[next];

            var duration = _random.Next(node.MinDuration, node.MaxDuration);
            weather.CurrentNode = next;
            weather.NextChangeTime = _timing.CurTime + duration;

            _weather.SetWeather((network, networkComp), node.Weather, duration);
        }
    }

    private string PickNextNode(Entity<CEStationWeatherComponent> ent)
    {
        if (ent.Comp.CurrentNode is { } current && ent.Comp.Nodes.TryGetValue(current, out var currentNode))
        {
            var picked = PickWeighted(currentNode.Transitions, ent.Comp.Nodes);
            if (picked is not null)
                return picked;

            Log.Error($"Weather node '{current}' of {ToPrettyString(ent)} has no valid transitions, picking a random node.");
        }

        return _random.Pick(ent.Comp.Nodes.Keys.ToList());
    }

    private string? PickWeighted(Dictionary<string, float> transitions, Dictionary<string, CEWeatherNode> nodes)
    {
        var total = 0f;
        foreach (var (key, weight) in transitions)
        {
            if (weight > 0f && nodes.ContainsKey(key))
                total += weight;
        }

        if (total <= 0f)
            return null;

        var roll = _random.NextFloat() * total;
        foreach (var (key, weight) in transitions)
        {
            if (weight <= 0f || !nodes.ContainsKey(key))
                continue;

            roll -= weight;
            if (roll <= 0f)
                return key;
        }

        return null;
    }
}
