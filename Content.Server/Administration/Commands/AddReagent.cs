using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Administration.Commands
{
    /// <summary>
    ///     Command that allows you to edit an existing solution by adding (or removing) reagents.
    /// </summary>
    [AdminCommand(AdminFlags.Admin)]
    public sealed partial class AddReagent : IConsoleCommand
    {
        [Dependency] private IEntityManager _entManager = default!;
        [Dependency] private IPrototypeManager _protomanager = default!;

        public string Command => "addreagent";
        public string Description => "Add (or remove) some amount of reagent from some solution.";
        public string Help => $"Usage: {Command} <target> <solution> <reagent> <quantity> [entry index]"; // CrystallEdge: optional entry index

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 4)
            {
                shell.WriteLine($"Not enough arguments.\n{Help}");
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteLine($"Invalid entity id.");
                return;
            }

            var solutionContainerSystem = _entManager.System<SharedSolutionContainerSystem>();
            if (!solutionContainerSystem.TryGetSolution(uid.Value, args[1], out var solution))
            {
                var solutions = solutionContainerSystem.EnumerateSolutions(uid.Value).ToArray();
                if (!solutions.Any())
                {
                    shell.WriteLine("Entity does not have any solutions!");
                    return;
                }

                var validSolutions = string.Join(", ", solutions.Select(s => s.Name));
                shell.WriteLine($"Entity does not have a \"{args[1]}\" solution. Valid solutions are:\n{validSolutions}");
                return;
            }

            if (!_protomanager.HasIndex<ReagentPrototype>(args[2]))
            {
                shell.WriteLine($"Unknown reagent prototype");
                return;
            }

            if (!float.TryParse(args[3], out var quantityFloat))
            {
                shell.WriteLine($"Failed to parse quantity");
                return;
            }
            var quantity = FixedPoint2.New(MathF.Abs(quantityFloat));

            // CrystallEdge: reagents with data (alchemy mixtures, blood) don't match a bare prototype id
            var contents = solution.Value.Comp.Solution.Contents;

            // The entry index targets one exact entry, keeping its data.
            if (args.Length > 4)
            {
                if (!int.TryParse(args[4], out var index) ||
                    index < 0 ||
                    index >= contents.Count ||
                    contents[index].Reagent.Prototype != args[2])
                {
                    shell.WriteLine($"No \"{args[2]}\" entry with index {args[4]}");
                    return;
                }

                var reagent = contents[index].Reagent;
                if (quantityFloat > 0)
                    solutionContainerSystem.TryAddReagent(solution.Value, reagent, quantity, out _);
                else
                    solutionContainerSystem.RemoveReagent(solution.Value, reagent, quantity);

                return;
            }

            if (quantityFloat > 0)
            {
                solutionContainerSystem.TryAddReagent(solution.Value, args[2], quantity, out _);
                return;
            }

            solution.Value.Comp.Solution.RemoveReagent(args[2], quantity, ignoreReagentData: true);
            solutionContainerSystem.UpdateChemicals(solution.Value);
            // CrystallEdge end
        }
    }
}
