namespace Content.Shared._CE.Humanoid;

/// <summary>
/// Automatically colors the specified layers to match the skin tone
/// </summary>
[RegisterComponent]
public sealed partial class CESkinColoredLayersComponent : Component
{
    [DataField]
    public List<string> Maps = new();
}
