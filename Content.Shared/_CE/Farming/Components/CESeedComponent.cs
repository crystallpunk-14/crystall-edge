namespace Content.Shared._CE.Farming.Components;

/// <summary>
/// Marks a <see cref="Placeable.CEPlaceableComponent"/> item as a seed: examining it names the plant it grows into.
/// </summary>
[RegisterComponent, Access(typeof(CESharedFarmingSystem))]
public sealed partial class CESeedComponent : Component;
