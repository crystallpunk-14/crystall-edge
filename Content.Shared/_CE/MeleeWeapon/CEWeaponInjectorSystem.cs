using Content.Shared._CE.MeleeWeapon.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Robust.Shared.Collections;

namespace Content.Shared._CE.MeleeWeapon;

public sealed partial class CEWeaponInjectorSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    [SubscribeLocalEvent]
    private void OnAttack(Entity<CEWeaponInjectorComponent> ent, ref CEAttackUsingEvent args)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var soln, out var solution)
            || solution.Volume <= 0)
            return;

        var targets = new ValueList<Entity<BloodstreamComponent>>();
        foreach (var target in args.Targets)
        {
            if (_bloodstreamQuery.TryComp(target, out var bloodstream))
                targets.Add((target, bloodstream));
        }

        if (targets.Count == 0)
            return;

        var removed = _solution.SplitSolution(soln.Value, ent.Comp.TransferAmount * targets.Count);
        var perTarget = removed.Volume * (1f / targets.Count);

        foreach (var target in targets)
        {
            var dose = removed.SplitSolution(perTarget);
            var doseText = SharedSolutionContainerSystem.ToPrettyString(dose);

            if (!_bloodstream.TryAddToBloodstream(target.AsNullable(), dose))
                continue;

            _adminLog.Add(LogType.MeleeHit,
                $"{ToPrettyString(args.User):user} injected {ToPrettyString(target):target} with {doseText:solution} using {ToPrettyString(ent):used}");
        }
    }
}
