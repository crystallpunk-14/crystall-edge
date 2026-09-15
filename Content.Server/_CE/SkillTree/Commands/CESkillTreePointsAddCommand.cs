using Content.Server.Administration;
using Content.Shared._CE.SkillTree;
using Content.Shared._CE.SkillTree.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.SkillTree.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class CESkillTreePointsAddCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private ISharedPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override string Command => "skilltreepointsadd";
    public override string Description => "Grants a player points to spend in a skill tree.";
    public override string Help => "Usage: skilltreepointsadd <player> <treeId> <points>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteError("Not enough arguments! Usage: skilltreepointsadd <player> <treeId> <points>");
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

        if (!float.TryParse(args[2], out var points))
        {
            shell.WriteError($"'{args[2]}' is not a valid number.");
            return;
        }

        var skillTreeSystem = _entities.System<CESkillTreeSystem>();

        if (!skillTreeSystem.TryAddSkillTreePoints(target, treeId, points))
        {
            shell.WriteError($"Failed to add {points} points of skill tree '{args[1]}' to player '{args[0]}'.");
            return;
        }

        shell.WriteLine($"Successfully added {points} points of skill tree '{args[1]}' to player '{args[0]}'.");
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

        if (args.Length == 3)
            return CompletionResult.FromHint("Points (number)");

        return CompletionResult.Empty;
    }
}
