using Robust.Shared.Audio;

namespace Content.Shared._CE.LockKey.Components;

/// <summary>
/// Allows, when interacting with keys, to mill different teeth, changing the shape of the key
/// </summary>
[RegisterComponent]
public sealed partial class CEKeyFileComponent : Component
{
    /// <summary>
    /// sound when used
    /// </summary>
    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/_CE/Effects/sharpening_stone.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.02f),
    };
}
