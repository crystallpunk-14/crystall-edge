using Content.Server._CE.Workbench;
using Content.Shared._CE.LockKey;
using Content.Shared._CE.LockKey.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.LockKey;

public sealed partial class CELockKeyForgeSystem : EntitySystem
{
    [Dependency] private CELockKeySystem _lockKey = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CELockKeyForgeComponent> ent, ref MapInitEvent args)
    {
        if (IsValidShape(ent.Comp, ent.Comp.Shape))
            return;

        ent.Comp.Shape = new List<int>();
        for (var i = 0; i < ent.Comp.DefaultComplexity; i++)
        {
            ent.Comp.Shape.Add(_random.Next(CESharedLockKeySystem.MinHeight, CESharedLockKeySystem.MaxHeight + 1));
        }
    }

    [SubscribeLocalEvent]
    private void OnBeforeUIOpen(Entity<CELockKeyForgeComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnSetShape(Entity<CELockKeyForgeComponent> ent, ref CELockKeyForgeSetShapeMessage args)
    {
        if (!IsValidShape(ent.Comp, args.Shape))
            return;

        ent.Comp.Shape = new List<int>(args.Shape);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnCrafted(Entity<CELockKeyForgeComponent> ent, ref CEWorkbenchCraftedEvent args)
    {
        foreach (var result in args.Results)
        {
            if (TryComp<CEKeyComponent>(result, out var key))
                _lockKey.TrySetShape((result, key), ent.Comp.Shape);

            if (TryComp<CELockComponent>(result, out var lockComp))
                _lockKey.TrySetShape((result, lockComp), ent.Comp.Shape);
        }
    }

    private void UpdateUi(Entity<CELockKeyForgeComponent> ent)
    {
        EntProtoId? result = null;
        if (TryComp<CEWorkbenchComponent>(ent, out var workbench) &&
            _proto.Resolve(workbench.SelectedRecipe, out var recipe))
            result = recipe.Result;

        _ui.SetUiState(ent.Owner,
            CELockKeyForgeUiKey.Key,
            new CELockKeyForgeUiState(new List<int>(ent.Comp.Shape), ent.Comp.MaxComplexity, result));
    }

    private static bool IsValidShape(CELockKeyForgeComponent forge, List<int> shape)
    {
        if (shape.Count == 0 || shape.Count > forge.MaxComplexity)
            return false;

        foreach (var height in shape)
        {
            if (height is < CESharedLockKeySystem.MinHeight or > CESharedLockKeySystem.MaxHeight)
                return false;
        }

        return true;
    }
}
