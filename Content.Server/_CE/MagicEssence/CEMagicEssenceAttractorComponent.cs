using Robust.Shared.Audio;

namespace Content.Server._CE.MagicEssence;

/// <summary>
/// A powered device that draws in nearby floating magic essence (see <see cref="CEMagicEssenceAttractingComponent"/>)
/// and, on contact, drains its solution into <see cref="Solution"/>.
/// </summary>
[RegisterComponent, Access(typeof(CEMagicEssenceAttractorSystem))]
public sealed partial class CEMagicEssenceAttractorComponent : Component
{
    [DataField]
    public string Solution = "collector";

    [DataField]
    public SoundSpecifier ConsumeSound = new SoundPathSpecifier("/Audio/_CE/Effects/essence_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithVariation(0.2f),
    };
}
