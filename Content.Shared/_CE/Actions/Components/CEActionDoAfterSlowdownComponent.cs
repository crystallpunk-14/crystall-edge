namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Slows the caster while using action
/// </summary>
[RegisterComponent, Access(typeof(CESharedActionSystem))]
public sealed partial class CEActionDoAfterSlowdownComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1f;
}
