namespace Content.Server._CE.Workbench;

/// <summary>
/// Provides resources to the workbench located in a container in the same entity.
/// </summary>
[RegisterComponent]
[Access(typeof(CEWorkbenchSystem))]
public sealed partial class CEWorkbenchContainerProviderComponent : Component
{
    /// <summary>
    /// The name of the container to check for craftable resources.
    /// </summary>
    [DataField(required: true)]
    public string ContainerName;
}
