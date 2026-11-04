using Content.Shared.Materials;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.ResourceManager.Requirements;

[Serializable, NetSerializable]
public sealed partial class MaterialResource : CEResourceRequirement
{
    [DataField(required: true)]
    public ProtoId<MaterialPrototype> Material;

    [DataField]
    public int Count = 1;

    public override bool CheckRequirement(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        var count = 0;
        foreach (var ent in placedEntities)
        {
            if (!entManager.TryGetComponent<PhysicalCompositionComponent>(ent, out var material))
                continue;

            entManager.TryGetComponent<StackComponent>(ent, out var stack);

            foreach (var (key, value) in material.MaterialComposition)
            {
                if (key != Material)
                    continue;

                if (stack is null)
                {
                    count += value;
                }
                else
                {
                    count += value * stack.Count;
                }
            }
        }

        if (count < Count)
            return false;

        return true;
    }

    public override void PostCraft(IEntityManager entManager, IPrototypeManager protoManager, HashSet<EntityUid> placedEntities)
    {
        var stackSystem = entManager.System<SharedStackSystem>();

        // Largest units first, so a partially used unit wastes as little material as possible
        var candidates = new List<(EntityUid Uid, int UnitValue)>();
        foreach (var placedEntity in placedEntities)
        {
            if (!entManager.TryGetComponent<PhysicalCompositionComponent>(placedEntity, out var material))
                continue;
            if (!material.MaterialComposition.TryGetValue(Material, out var unitValue) || unitValue <= 0)
                continue;

            candidates.Add((placedEntity, unitValue));
        }

        candidates.Sort((a, b) => b.UnitValue.CompareTo(a.UnitValue));

        var requiredCount = Count;
        foreach (var (placedEntity, unitValue) in candidates)
        {
            if (requiredCount <= 0)
                return;

            if (!entManager.TryGetComponent<StackComponent>(placedEntity, out var stack))
            {
                requiredCount -= unitValue;
                entManager.DeleteEntity(placedEntity);
                continue;
            }

            // A unit can't be split: a partially needed unit is consumed whole
            var unitsToRemove = Math.Min(stack.Count, (requiredCount + unitValue - 1) / unitValue);
            var newStackCount = stack.Count - unitsToRemove;

            if (newStackCount <= 0)
                entManager.DeleteEntity(placedEntity);
            else
                stackSystem.SetCount((placedEntity, stack), newStackCount);

            requiredCount -= unitsToRemove * unitValue;
        }
    }

    public override double GetPrice(IEntityManager entManager,
        IPrototypeManager protoManager)
    {
        if (protoManager.TryIndex(Material, out var indexedMaterial))
        {
            return indexedMaterial.Price * Count;
        }

        return 0;
    }

    public override string GetRequirementAmount()
    {
        return Count.ToString();
    }

    public override string GetRequirementTitle(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Material, out var indexedMaterial))
            return "Error material";

        return Loc.GetString("ce-workbench-material-req",
            ("name", Loc.GetString(indexedMaterial.Name)),
            ("count", Count));
    }

    public override List<CEResourceIconLayer> GetRequirementIcon(IEntityManager entManager, IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Material, out var indexedMaterial) || indexedMaterial.Icon is not { } icon)
            return new List<CEResourceIconLayer>();

        return new List<CEResourceIconLayer> { CEResourceIconLayer.FromSprite(icon) };
    }
}
