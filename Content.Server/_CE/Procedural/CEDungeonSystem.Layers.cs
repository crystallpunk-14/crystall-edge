using System.Threading.Tasks;
using Content.Server._CE.Procedural.Generation;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.Procedural;

public sealed partial class CEDungeonSystem
{
    public async Task<List<EntityUid>> GenerateLayers(
        CEProceduralGenerationContext context,
        Dictionary<int, List<ICEProceduralLayer>> layersByHeight)
    {
        var byHeight = new Dictionary<int, EntityUid>();
        foreach (var height in layersByHeight.Keys)
        {
            var mapUid = context.Map.CreateMap(out _, runMapInit: false);
            context.EntityManager.EnsureComponent<MapGridComponent>(mapUid);
            byHeight[height] = mapUid;
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
