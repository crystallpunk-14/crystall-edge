using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation;

namespace Content.Server._CE.Procedural;

public sealed partial class CEDungeonSystem
{
    /// <summary>
    /// Loads a fresh, standalone instance of <see cref="DefaultMap"/> — the shared entry point
    /// every generation path uses to obtain a starting map instead of a blank <c>CreateMap</c>.
    /// </summary>
    public EntityUid LoadMap()
    {
        if (!_loader.TryLoadMap(DefaultMap, out var mapEnt, out _))
            throw new Exception($"Failed to load default map '{DefaultMap}' for procedural generation.");

        return mapEnt.Value.Owner;
    }

    public async Task<List<EntityUid>> GenerateLayers(
        CEProceduralGenerationContext context,
        Dictionary<int, List<ICEProceduralLayer>> layersByHeight)
    {
        var byHeight = new Dictionary<int, EntityUid>();
        foreach (var height in layersByHeight.Keys)
        {
            byHeight[height] = LoadMap();
        }

        var heights = new List<int>(layersByHeight.Keys);
        heights.Sort();
        heights.Reverse();

        var result = new List<EntityUid>(heights.Count);
        foreach (var height in heights)
        {
            var map = byHeight[height];

            foreach (var layer in layersByHeight[height])
            {
                await layer.Apply(context, map);
                await context.Suspend();
                context.Cancellation.ThrowIfCancellationRequested();
            }

            result.Add(map);
        }

        return result;
    }
}
