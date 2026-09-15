using Content.Server.Administration;
using Content.Shared._CE.SkillTree;
using Content.Shared._CE.SkillTree.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.SkillTree.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class CESkillTreeAddCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private ISharedPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override string Command => "skilltreeadd";
    public override string Description => "Grants a player access to a skill tree.";
    public override string Help => "Usage: skilltreeadd <player> <treeId>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError("Not enough arguments! Usage: skilltreeadd <player> <treeId>");
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteError($"Player '{args[0]}' not found or not connected.");
            return;
        }

        if (session.AttachedEntity is not { } target)
        {
            shell.WriteError($"Player '{args[0]}' has no attached entity.");
            return;
        }

        var treeId = new ProtoId<CESkillTreePrototype>(args[1]);

        if (!_proto.HasIndex(treeId))
        {
            shell.WriteError($"Unknown skill tree prototype '{args[1]}'.");
            return;
        }

        var skillTreeSystem = _entities.System<CESkillTreeSystem>();

        if (!skillTreeSystem.TryAddSkillTree(target, treeId))
        {
            shell.WriteError($"Player '{args[0]}' already has skill tree '{args[1]}'.");
            return;
        }

        shell.WriteLine($"Successfully added skill tree '{args[1]}' to player '{args[0]}'.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _players),
                "Player name");
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<CESkillTreePrototype>(proto: _proto),
                "Skill tree prototype ID");
        }

        return CompletionResult.Empty;
    }
}
