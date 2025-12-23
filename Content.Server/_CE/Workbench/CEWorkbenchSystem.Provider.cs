using Content.Shared.Placeable;

namespace Content.Server._CE.Workbench;

public sealed partial class CEWorkbenchSystem
{
    private void InitProviders()
    {
        SubscribeLocalEvent<CEWorkbenchPlaceableProviderComponent, CEWorkbenchGetResourcesEvent>(OnGetPlaceableResource);
        SubscribeLocalEvent<CEWorkbenchContainerProviderComponent, CEWorkbenchGetResourcesEvent>(OnGetContainerResource);
    }

    private void OnGetPlaceableResource(Entity<CEWorkbenchPlaceableProviderComponent> ent, ref CEWorkbenchGetResourcesEvent args)
    {
        if (!TryComp<ItemPlacerComponent>(ent, out var placer))
            return;

        args.AddResources(placer.PlacedEntities);
    }

    private void OnGetContainerResource(Entity<CEWorkbenchContainerProviderComponent> ent, ref CEWorkbenchGetResourcesEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerName, out var container))
            return;

        args.AddResources(container.ContainedEntities);
    }
}

public sealed class CEWorkbenchGetResourcesEvent : EntityEventArgs
{
    public HashSet<EntityUid> Resources { get; private set; } = new();

    public void AddResource(EntityUid resource)
    {
        Resources.Add(resource);
    }

    public void AddResources(IEnumerable<EntityUid> resources)
    {
        foreach (var resource in resources)
        {
            Resources.Add(resource);
        }
    }
}
