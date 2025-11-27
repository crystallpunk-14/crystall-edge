using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared._CE.LockKey;
using Content.Shared.Dataset;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CE.Ambitions.Parsings;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEAmbitionParsing
{
    public abstract string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner);
}

public sealed partial class RandomFood : CEAmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        List<CECookingRecipePrototype> all = new();

        foreach (var recipe in protoManager.EnumeratePrototypes<CECookingRecipePrototype>())
        {
            if (recipe.FoodData.Name is null)
                continue;

            all.Add(recipe);
        }

        if (all.Count == 0)
            return null;

        return Loc.GetString(random.Pick(all).FoodData.Name!);
    }
}

public sealed partial class RandomDataset : CEAmbitionParsing
{
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Dataset;

    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        if (!protoManager.Resolve(Dataset, out var resolvedDataset))
            return null;

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

    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        List<EntityPrototype> all = new();

        if (!protoManager.TryIndex(Category, out var filter))
            return null;

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

    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        return Range.Next(random).ToString();
    }
}

public sealed partial class RandomOtherJob : CEAmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        List<JobPrototype> all = new();

        var mindSys = entManager.System<SharedMindSystem>();
        var jobSys = entManager.System<SharedJobSystem>();

        if (owner == null)
            return null;

        if (!mindSys.TryGetMind(owner.Value, out var mindId, out var mind))
            return null;

        if (!jobSys.MindTryGetJob(mindId, out var currentJob))
            return null;

        foreach (var job in protoManager.EnumeratePrototypes<JobPrototype>())
        {
            if (currentJob == job)
                continue;

            if (!job.SetPreference)
                continue;

            all.Add(job);
        }

        if (all.Count == 0)
            return null;

        return Loc.GetString(random.Pick(all).Name);
    }
}

public sealed partial class RandomSpecies : CEAmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        List<SpeciesPrototype> all = new();

        foreach (var job in protoManager.EnumeratePrototypes<SpeciesPrototype>())
        {
            if (!job.RoundStart)
                continue;

            all.Add(job);
        }

        if (all.Count == 0)
            return null;

        return Loc.GetString(random.Pick(all).Name);
    }
}

public sealed partial class RandomLocation : CEAmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        List<CELockTypePrototype> all = new();

        foreach (var lockProto in protoManager.EnumeratePrototypes<CELockTypePrototype>())
        {
            if (lockProto.Name is null)
                continue;

            all.Add(lockProto);
        }
        if (all.Count == 0)
            return null;

        return Loc.GetString(random.Pick(all).Name!);
    }
}

public sealed partial class RandomOtherPerson : CEAmbitionParsing
{
    public override string? GetText(IEntityManager entManager, IPrototypeManager protoManager, IRobustRandom random, EntityUid? owner)
    {
        List<string> all = new();

        foreach (var player in Filter.GetAllPlayers())
        {
            var attachedEntity = player.AttachedEntity;

            if (attachedEntity is null || attachedEntity == owner)
                continue;
            if (!entManager.HasComponent<MobStateComponent>(attachedEntity))
                continue;
            if (!entManager.TryGetComponent<MetaDataComponent>(attachedEntity, out var metaData))
                continue;

            all.Add(metaData.EntityName);
        }

        if (all.Count == 0)
            return null;

        return random.Pick(all);
    }
}
