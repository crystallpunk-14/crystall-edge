using Content.Server.Administration;
using Content.Shared._CE.Murk.Systems;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._CE.Murk;

[AdminCommand(AdminFlags.Fun)]
public sealed partial class CESetMurkCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _entities = default!;

    public override string Command => "znetwork-murk";
    public override string Description => "Ensures murk on every map of zNetwork and sets its intensity";
    public override string Help => "znetwork-murk <net entity> <intensity>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("Not enough arguments!");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var targetNet) ||
            !_entities.TryGetEntity(targetNet, out var target))
        {
            shell.WriteError($"Unable to find entity {args[0]}");
            return;
        }

        if (!_entities.TryGetComponent<CEZMapNetworkComponent>(target, out var levelComp))
        {
            shell.WriteError($"Target entity doesnt have CEZLevelsNetworkComponent {args[0]}");
            return;
        }

        if (!float.TryParse(args[1], out var intensity))
        {
            shell.WriteError("Intensity must be a number!");
            return;
        }

        _entities.System<CESharedMurkSystem>().SetNetworkIntensity((target.Value, levelComp), intensity);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = new List<CompletionOption>();
            var query = _entities.EntityQueryEnumerator<CEZMapNetworkComponent, MetaDataComponent>();
            while (query.MoveNext(out var uid, out _, out var meta))
            {
                options.Add(new CompletionOption(_entities.GetNetEntity(uid).ToString(), meta.EntityName));
            }
            return CompletionResult.FromHintOptions(options, "zNetwork net entity");
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHint("Base murk intensity: 0 is no murk, 1 is full strength");
        }

        return CompletionResult.Empty;
    }
}
