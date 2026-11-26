using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Alchemy.Mortar;

/// <summary>
/// Used on a <see cref="CEMortarComponent"/> to grind everything inside it.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CEMortarSystem))]
public sealed partial class CEPestleComponent : Component
{
    [DataField]
    public TimeSpan GrindDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Should be about as long as <see cref="GrindDuration"/>.
    /// </summary>
    [DataField]
    public SoundSpecifier GrindSound = new SoundPathSpecifier("/Audio/_CE/Effects/pestle_grinding.ogg");

    /// <summary>
    /// Playing grinding sound, stopped when the grinding is interrupted. Server only.
    /// </summary>
    [ViewVariables]
    public EntityUid? AudioStream;
}
