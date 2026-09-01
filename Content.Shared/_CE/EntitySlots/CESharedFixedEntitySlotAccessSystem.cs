using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared._CE.EntitySlots;

/// <summary>
/// Exposes visible fixed-slot occupants to canonical interaction and pickup handling.
/// It grants only container accessibility; normal range, obstruction, hands and item checks still apply.
/// </summary>
public sealed partial class CESharedFixedEntitySlotAccessSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEFixedEntitySlotAccessibleOccupantComponent, AccessibleOverrideEvent>(OnAccessibleOverride);
    }

    private void OnAccessibleOverride(
        Entity<CEFixedEntitySlotAccessibleOccupantComponent> ent,
        ref AccessibleOverrideEvent args)
    {
        if (args.Accessible || args.Target != ent.Owner ||
            !_containers.TryGetContainingContainer(ent.Owner, out var container) ||
            !HasComp<CEFixedEntitySlotAccessComponent>(container.Owner) ||
            !_interaction.CanAccess(args.User, container.Owner))
            return;

        args.Accessible = true;
        args.Handled = true;
    }
}
