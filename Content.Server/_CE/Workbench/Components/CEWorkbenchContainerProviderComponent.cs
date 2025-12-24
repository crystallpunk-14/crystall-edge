namespace Content.Server._CE.Workbench;

/// <summary>
/// Provides resources to the workbench located in a container in the same entity.
/// </summary>
[RegisterComponent]
[Access(typeof(CEWorkbenchSystem))]
public sealed partial class CEWorkbenchContainerProviderComponent : Component
{
    [DataField(required: true)]
    public string ContainerName;
}
