using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Blinking;

/// <summary>
/// Makes a character blink. That's it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(CESharedBlinkingSystem))]
public sealed partial class CEBlinkerComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBlinkTime;

    [DataField]
    public TimeSpan MinBlinkDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan MaxBlinkDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}

[Serializable, NetSerializable]
public enum CEBlinkVisuals : byte
{
    EyesClosed,
}
