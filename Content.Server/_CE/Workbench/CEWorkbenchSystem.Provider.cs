using Content.Shared.Placeable;
using Robust.Shared.Containers;

namespace Content.Server._CE.Workbench;

public sealed partial class CEWorkbenchSystem
{
    private void InitProviders()
    {
        SubscribeLocalEvent<CEWorkbenchPlaceableProviderComponent, CEWorkbenchGetResourcesEvent>(OnGetPlaceableResource);
        SubscribeLocalEvent<CEWorkbenchPlaceableProviderComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<CEWorkbenchPlaceableProviderComponent, ItemRemovedEvent>(OnItemRemoved);

        SubscribeLocalEvent<CEWorkbenchContainerProviderComponent, CEWorkbenchGetResourcesEvent>(OnGetContainerResource);
        SubscribeLocalEvent<CEWorkbenchContainerProviderComponent, EntInsertedIntoContainerMessage>(OnInsertedToContainer);
        SubscribeLocalEvent<CEWorkbenchContainerProviderComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    private void OnGetPlaceableResource(Entity<CEWorkbenchPlaceableProviderComponent> ent, ref CEWorkbenchGetResourcesEvent args)
    {
        if (!TryComp<ItemPlacerComponent>(ent, out var placer))
            return;

        args.AddResources(placer.PlacedEntities);
    }

    private void OnItemRemoved(Entity<CEWorkbenchPlaceableProviderComponent> ent, ref ItemRemovedEvent args)
    {
        UpdateUIRecipes(ent.Owner);
    }

    private void OnItemPlaced(Entity<CEWorkbenchPlaceableProviderComponent> ent, ref ItemPlacedEvent args)
    {
        UpdateUIRecipes(ent.Owner);
    }


    private void OnGetContainerResource(Entity<CEWorkbenchContainerProviderComponent> ent, ref CEWorkbenchGetResourcesEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerName, out var container))
            return;

        args.AddResources(container.ContainedEntities);
    }

    private void OnInsertedToContainer(Entity<CEWorkbenchContainerProviderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateUIRecipes(ent.Owner);
    }

    private void OnRemovedFromContainer(Entity<CEWorkbenchContainerProviderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateUIRecipes(ent.Owner);
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
