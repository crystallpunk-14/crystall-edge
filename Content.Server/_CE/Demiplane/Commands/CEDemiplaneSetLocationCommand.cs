using Content.Server._CE.Demiplane.Prototypes;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Station.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Commands;

[AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
public sealed partial class CEDemiplaneSetLocationCommand : LocalizedEntityCommands
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private CEDemiplaneSystem _demiplane = default!;

    private const string NullLocation = "null";
    private const float DefaultTeleportTime = 60f;

    public override string Command => "demiplane-set-location";
    public override string Description => "Clears a station's current demiplane stage and, after a delay, teleports it to a freshly generated one (or to the void, if the location is `null`).";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                var stations = new List<CompletionOption>();
                var query = _entities.EntityQueryEnumerator<StationDataComponent, MetaDataComponent>();
                while (query.MoveNext(out var uid, out _, out var meta))
                {
                    stations.Add(new CompletionOption(_entities.GetNetEntity(uid).ToString(), meta.EntityName));
                }
                return CompletionResult.FromHintOptions(stations, "station net entity");
            case 2:
                var locations = new List<CompletionOption> { new(NullLocation, "back to the void") };
                foreach (var location in _proto.EnumeratePrototypes<CEDemiplaneLocationPrototype>())
                {
                    locations.Add(new CompletionOption(location.ID, Loc.GetString(location.Name)));
                }
                return CompletionResult.FromHintOptions(locations, "demiplaneLocation prototype, or `null`");
            case 3:
                return CompletionResult.FromHint($"teleport time in seconds (default {DefaultTeleportTime:0})");
        }
        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var stationNet) || !_entities.TryGetEntity(stationNet, out var station))
        {
            shell.WriteError($"Unable to find entity {args[0]}");
            return;
        }

        ProtoId<CEDemiplaneLocationPrototype>? location = null;
        if (!string.Equals(args[1], NullLocation, StringComparison.OrdinalIgnoreCase))
        {
            if (!_proto.HasIndex<CEDemiplaneLocationPrototype>(args[1]))
            {
                shell.WriteError($"Unknown demiplane location: {args[1]}");
                return;
            }
            location = args[1];
        }

        var teleportTime = TimeSpan.FromSeconds(DefaultTeleportTime);
        if (args.Length == 3)
        {
            if (!float.TryParse(args[2], out var seconds) || seconds < 0)
            {
                shell.WriteError($"Invalid teleport time: {args[2]}");
                return;
            }
            teleportTime = TimeSpan.FromSeconds(seconds);
        }

        if (!_demiplane.StartTeleport(station.Value, location, teleportTime))
        {
            shell.WriteError("Failed to start the teleport - see server log.");
            return;
        }

        shell.WriteLine(location is null
            ? $"Station {station} will drift over the void in {teleportTime.TotalSeconds:0}s."
            : $"Station {station} will arrive at `{location}` in {teleportTime.TotalSeconds:0}s.");
    }
}
