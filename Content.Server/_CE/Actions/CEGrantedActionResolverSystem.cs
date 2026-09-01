using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Actions;

/// <summary>
/// Resolves one uniquely granted action by prototype without taking ownership of action granting.
/// </summary>
public sealed partial class CEGrantedActionResolverSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    /// <summary>
    /// Resolves the same first matching action selected by the existing CE GOAP
    /// use-action system.
    /// </summary>
    public bool TryResolveFirst(
        EntityUid holder,
        EntProtoId prototype,
        out Entity<ActionComponent> resolved)
    {
        return TryResolve(holder, prototype, requireUnique: false, out resolved);
    }

    public bool TryResolveUnique(
        EntityUid holder,
        EntProtoId prototype,
        out Entity<ActionComponent> resolved)
    {
        return TryResolve(holder, prototype, requireUnique: true, out resolved);
    }

    private bool TryResolve(
        EntityUid holder,
        EntProtoId prototype,
        bool requireUnique,
        out Entity<ActionComponent> resolved)
    {
        resolved = default;
        var found = false;

        foreach (var action in _actions.GetActions(holder))
        {
            if (MetaData(action).EntityPrototype?.ID != (string) prototype)
                continue;

            if (found && requireUnique)
            {
                resolved = default;
                return false;
            }

            resolved = action;
            found = true;

            if (!requireUnique)
                return true;
        }

        return found;
    }
}
