using Content.Shared._CE.Trade;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ResourceManager.Requirements;

public sealed partial class StackResource : CEResourceRequirement
{
    [DataField(required: true)]
    public ProtoId<StackPrototype> Stack;

    [DataField]
    public int Count = 1;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        var count = 0;
        foreach (var ent in placedEntities)
        {
            if (!entManager.TryGetComponent<StackComponent>(ent, out var stack))
                continue;

            if (stack.StackTypeId != Stack)
                continue;

            count += stack.Count;
        }

        if (count < Count)
            return false;

        return true;
    }

    public override void PostCraft(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        var stackSystem = entManager.System<SharedStackSystem>();

        var requiredCount = Count;
        foreach (var placedEntity in placedEntities)
        {
            if (!entManager.TryGetComponent<StackComponent>(placedEntity, out var stack))
                continue;

            if (stack.StackTypeId != Stack)
                continue;

            var count = (int)MathF.Min(requiredCount, stack.Count);

            if (stack.Count - count <= 0)
                entManager.DeleteEntity(placedEntity);
            else
                stackSystem.SetCount((placedEntity, stack), stack.Count - count);

            requiredCount -= count;
        }
    }

    public override double GetPrice(IEntityManager entManager,
        IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Stack, out var indexedStack))
            return 0;

        if (!protoManager.TryIndex(indexedStack.Spawn, out var indexedProto))
            return 0;

        var priceSys = entManager.System<CEEconomySystem>();

        return priceSys.GetEstimatedPrice(indexedProto) * Count;
    }

    public override string GetRequirementTitle(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Stack, out var indexedStack))
            return "Error stack";

        return $"{Loc.GetString(indexedStack.Name)} x{Count}";
    }

    public override string GetRequirementAmount()
    {
        return Count.ToString();
    }

    public override List<CEResourceIconLayer> GetRequirementIcon(IEntityManager entManager, IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Stack, out var indexedStack) || indexedStack.Icon is not { } icon)
            return new List<CEResourceIconLayer>();

        return new List<CEResourceIconLayer> { CEResourceIconLayer.FromSprite(icon) };
    }
}
