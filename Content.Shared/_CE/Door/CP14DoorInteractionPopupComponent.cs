using Robust.Shared.Audio;

namespace Content.Shared._CE.Door;

[RegisterComponent, Access(typeof(CEDoorInteractionPopupSystem))]
public sealed partial class CEDoorInteractionPopupComponent : Component
{
    /// <summary>
    /// Time delay between interactions to avoid spam.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan InteractDelay = TimeSpan.FromSeconds(1.0);

    [DataField]
    public string InteractString = "entity-storage-component-locked-message";

    [DataField]
    public SoundSpecifier? InteractSound;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastInteractTime = TimeSpan.Zero;

}
