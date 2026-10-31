namespace Content.Server._CE.LockKey;

/// <summary>
/// Workbench add-on that stamps the configured shape into every crafted key or lock.
/// </summary>
[RegisterComponent, Access(typeof(CELockKeyForgeSystem))]
public sealed partial class CELockKeyForgeComponent : Component
{
    [DataField]
    public List<int> Shape = new();

    [DataField]
    public int MaxComplexity = 6;

    /// <summary>
    /// Length of the random shape generated on map init if none is set.
    /// </summary>
    [DataField]
    public int DefaultComplexity = 3;
}
