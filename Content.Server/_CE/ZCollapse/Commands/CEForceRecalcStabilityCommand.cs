using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server._CE.ZCollapse.Commands;

/// <summary>
/// Forces a grid's ZCollapse stability to recompute. There's no separate "reset broken bookkeeping"
/// path to run anymore — this just marks the grid dirty, the exact same thing any anchor/tile change
/// does, so if the result still looks wrong afterward that's a sign the Cores/Supports index is out
/// of sync, not the flood-fill math.
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
        shell.WriteLine($"Queued ZCollapse stability recompute for grid {gridUid}.");
    }
}
