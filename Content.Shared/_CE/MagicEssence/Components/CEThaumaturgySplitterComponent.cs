namespace Content.Shared._CE.MagicEssence.Components;

/// <summary>
/// Marks a CEPressTargetComponent entity as a thaumaturgical splitter: crushed entities with
/// essence content are broken down into essence items (and thrown away from the splitter)
/// instead of taking the press's fallback crushing damage.
/// </summary>
[RegisterComponent]
public sealed partial class CEThaumaturgySplitterComponent : Component
{
    /// <summary>
    /// Throw speed used to scatter spawned essence items away from the splitter.
    /// </summary>
    [DataField]
    public float ScatterThrowSpeed = 3f;
}
