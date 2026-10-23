using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared._CE.Roles;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Roles.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed partial class CESecretRoleSetCommand : LocalizedCommands
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private ISharedPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override string Command => "secretroleset";
    public override string Description => "Sets a player's secret role, replacing whatever secret role (objectives and skills included) they had before. The original role is kept unless overwriteOriginal is true.";
    public override string Help => "Usage: secretroleset <player> <roleId> [overwriteOriginal=false]";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError("Not enough arguments! Usage: secretroleset <player> <roleId> [overwriteOriginal=false]");
            return;
        }

        var overwriteOriginal = false;
        if (args.Length > 2 && !bool.TryParse(args[2], out overwriteOriginal))
        {
            shell.WriteError($"'{args[2]}' is not a valid boolean.");
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteError($"Player '{args[0]}' not found or not connected.");
            return;
        }

        var roleId = new ProtoId<CESecretRolePrototype>(args[1]);

        if (!_entities.System<CESecretRoleSelectionSystem>().TrySetSecretRole(session, roleId, out var error, overwriteOriginal: overwriteOriginal))
        {
            shell.WriteError(error);
            return;
        }

        var admin = shell.Player?.Name ?? "Console";
        _adminLog.Add(LogType.AdminCommands,
            LogImpact.High,
            $"{admin} set secret role of {session.Name} to {roleId} (overwrite original: {overwriteOriginal})");

        shell.WriteLine($"Successfully set secret role '{args[1]}' for player '{args[0]}'.");
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
                CompletionHelper.PrototypeIDs<CESecretRolePrototype>(proto: _proto),
                "Secret role prototype ID");
        }

        if (args.Length == 3)
            return CompletionResult.FromHintOptions(CompletionHelper.Booleans, "Overwrite original role (default false)");

        return CompletionResult.Empty;
    }
}
