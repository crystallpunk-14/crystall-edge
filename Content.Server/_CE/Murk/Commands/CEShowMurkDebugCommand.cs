using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._CE.Murk.Commands;

/// <summary>
/// Toggles the murk debug overlay (free zones, boundary walls, pylon spacing) - a mapping tool for
/// uninitialized maps, see <c>CEMurkSystem.Debug.cs</c>.
/// </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed partial class CEShowMurkDebugCommand : LocalizedEntityCommands
{
    [Dependency] private CEMurkSystem _murk = default!;

    public override string Command => "showmurkdebug";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var session = shell.Player;
        if (session == null)
            return;

        shell.WriteLine(_murk.ToggleDebugView(session)
            ? "murk debug overlay enabled"
            : "murk debug overlay disabled");
    }
}
