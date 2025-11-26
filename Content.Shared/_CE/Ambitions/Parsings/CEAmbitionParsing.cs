using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared._CE.LockKey;
using Content.Shared.Dataset;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CE.Ambitions.Parsings;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEAmbitionParsing
{
    public abstract string GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random);
}

public sealed partial class RandomFood : CEAmbitionParsing
{
    public override string GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random)
    {
        List<CECookingRecipePrototype> allRecipes = new();

        foreach (var recipe in protoManager.EnumeratePrototypes<CECookingRecipePrototype>())
        {
            if (recipe.FoodData.Name is null)
                continue;

            allRecipes.Add(recipe);
        }

        return Loc.GetString(random.Pick(allRecipes).FoodData.Name!);
    }
}

public sealed partial class RandomDataset : CEAmbitionParsing
{
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Dataset;

    public override string GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random)
    {
        if (!protoManager.Resolve(Dataset, out var resolvedDataset))
            return "error";

        var value = random.Pick(resolvedDataset.Values);
        return Loc.GetString(value);
    }
}


public sealed partial class RandomEntity : CEAmbitionParsing
{
    [DataField]
    public ProtoId<EntityCategoryPrototype> Category = "ForkFiltered";

    [DataField]
    public List<string> Whitelist = new();

    public override string GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random)
    {
        List<EntityPrototype> all = new();

        if (!protoManager.TryIndex(Category, out var filter))
            return "error";

        foreach (var item in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (item.Abstract)
                continue;
            if (item.HideSpawnMenu)
                continue;
            if (!item.Categories.Contains(filter))
                continue;
            var suitable = true;
            foreach (var compName in Whitelist)
            {
                if (!item.Components.TryGetComponent(compName, out _))
                {
                    suitable = false;
                    break;
                }
            }

            if (!suitable)
                continue;

            all.Add(item);
        }

        return random.Pick(all).Name;
    }
}

public sealed partial class RandomNumber : CEAmbitionParsing
{
    [DataField(required: true)]
    public MinMax Range;

    public override string GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random)
    {
        return Range.Next(random).ToString();
    }
}

public sealed partial class RandomJob : CEAmbitionParsing
{
    public override string GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random)
    {
        List<JobPrototype> all = new();

        foreach (var job in protoManager.EnumeratePrototypes<JobPrototype>())
        {
            if (!job.SetPreference)
                continue;

            all.Add(job);
        }
        return Loc.GetString(random.Pick(all).Name);
    }
}

public sealed partial class RandomSpecies : CEAmbitionParsing
{
    public override string GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random)
    {
        List<SpeciesPrototype> all = new();

        foreach (var job in protoManager.EnumeratePrototypes<SpeciesPrototype>())
        {
            if (!job.RoundStart)
                continue;

            all.Add(job);
        }
        return Loc.GetString(random.Pick(all).Name);
    }
}

public sealed partial class RandomLocation : CEAmbitionParsing
{
    public override string GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random)
    {
        List<CELockTypePrototype> all = new();

        foreach (var lockProto in protoManager.EnumeratePrototypes<CELockTypePrototype>())
        {
            if (lockProto.Name is null)
                continue;

            all.Add(lockProto);
        }
        return Loc.GetString(random.Pick(all).Name!);
    }
}
