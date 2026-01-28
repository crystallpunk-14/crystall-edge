using Robust.Shared.Audio;

namespace Content.Shared._CE.FarSound;

[RegisterComponent]
public sealed partial class CEFarSoundComponent : Component
{
    [DataField]
    public SoundSpecifier? CloseSound;

    [DataField]
    public SoundSpecifier? FarSound;

    [DataField]
    public float FarRange = 50f;
}
