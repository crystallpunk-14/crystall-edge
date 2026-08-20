using Content.Server.Administration;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Skill.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class CESkillAddCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private ISharedPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override string Command => "skilladd";
    public override string Description => "Adds a skill to a player.";
    public override string Help => "Usage: skilladd <player> <skillId>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError("Not enough arguments! Usage: skilladd <player> <skillId>");
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

        var skillId = new ProtoId<CESkillPrototype>(args[1]);

        if (!_proto.HasIndex(skillId))
        {
            shell.WriteError($"Unknown skill prototype '{args[1]}'.");
            return;
        }

        var skillSystem = _entities.System<CESkillSystem>();

        if (skillSystem.HaveSkill(target, skillId))
        {
            shell.WriteError($"Player '{args[0]}' already has skill '{args[1]}'.");
            return;
        }

        if (!skillSystem.TryAddSkill(target, skillId))
        {
            shell.WriteError($"Failed to add skill '{args[1]}' to player '{args[0]}'. The player may be missing the skill storage component.");
            return;
        }

        shell.WriteLine($"Successfully added skill '{args[1]}' to player '{args[0]}'.");
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
                CompletionHelper.PrototypeIDs<CESkillPrototype>(proto: _proto),
                "Skill prototype ID");
        }

        return CompletionResult.Empty;
    }
}
