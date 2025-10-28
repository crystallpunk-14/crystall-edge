using Content.Server._CE.PVS;
using Content.Server._CE.ZLevels.EntitySystems;
using Content.Server.Administration;
using Content.Shared._CE.ZLevels;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._CE.ZLevels.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class CECombineMapsIntoZLevelsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;
    private MapSystem _map = default!;
    private CEZLevelsSystem _zLevels = default!;

    private const string Name = "zlevelcombine";
    public override string Command => Name;
    public override string Description => "Connects a number of maps into a common network of z-levels. Does not work if one of the maps is already in the z-level network";
    public override string Help => $"{Name} <MapId 1> <MapId 2> ... <MapId X> (from ground to sky)";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 1)
        {
            shell.WriteError("Not enough maps to form a network of levels");
            return;
        }

        _zLevels = _entities.System<CEZLevelsSystem>();
        _map = _entities.System<MapSystem>();

        List<MapId> maps = new();
        foreach (var arg in args)
        {
            if (!int.TryParse(arg, out var mapIdInt))
            {
                shell.WriteError($"Cannot parse `{arg}` into mapId");
                return;
            }

            var mapId = new MapId(mapIdInt);

            if (mapId == MapId.Nullspace)
            {
                shell.WriteError($"Cannot parse NullSpace");
                return;
            }

            if (!_map.MapExists(mapId))
            {
                shell.WriteError($"Map {mapId} dont exist");
                return;
            }

            if (maps.Contains(mapId))
            {
                shell.WriteError($"Duplication maps: {mapId}");
                return;
            }

            maps.Add(mapId);
        }

        var network = _zLevels.CreateZNetwork();
        var counter = 0;
        var success = true;
        foreach (var findMap in maps)
        {
            if (!_zLevels.TryAddMapIntoZNetwork(network, _map.GetMap(findMap), counter))
            {
                shell.WriteError($"Unable to add map {findMap} to the new network!");
                success = false;
            }
            counter++;
        }

        if (success)
            shell.WriteLine($"Created z-level network! Z-Network entity: {network}");
        else
            shell.WriteLine($"Created z-level network {network}, but something went wrong!");
    }
}
