using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Dash;

/// <summary>
/// This component marks entities that are currently in the dash
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(WhiteDashSystem))]
public sealed partial class WhiteDashComponent : Component
{
    [DataField]
    public EntProtoId DashEffect = "CEDustEffect";

    [DataField]
    public SoundSpecifier DashSound = new SoundPathSpecifier("/Audio/_White/Effects/dash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f)
    };
}
