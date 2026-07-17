using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server._CE.ZCollapse.Commands;

/// <summary>
/// Consistency-repair fallback: wipes and fully re-derives a grid's ZCollapse stability data from
/// currently anchored Cores/Supports. The incremental algorithm should never actually need this in
/// normal play — it exists to recover from a bug, not as a routine tool.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed partial class CEForceRecalcStabilityCommand : LocalizedEntityCommands
{
    [Dependency] private CEZCollapseSystem _collapse = default!;
    [Dependency] private IEntityManager _entities = default!;

    public override string Command => "znetwork-collapserecalc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        EntityUid? target = null;

        if (args.Length >= 1)
        {
            if (!NetEntity.TryParse(args[0], out var targetNet) || !_entities.TryGetEntity(targetNet, out target))
            {
                shell.WriteError($"Unable to find entity {args[0]}");
                return;
            }
        }
        else if (shell.Player?.AttachedEntity is { } attached &&
                  _entities.TryGetComponent<TransformComponent>(attached, out var xform))
        {
            target = xform.GridUid;
        }

        if (target is not { } gridUid)
        {
            shell.WriteError("No target grid — pass a grid NetEntity or run this while attached to an entity on the target grid.");
            return;
        }

        _collapse.ForceRecalculateGrid(gridUid);
        shell.WriteLine($"Recalculated ZCollapse stability for grid {gridUid}.");
    }
}
