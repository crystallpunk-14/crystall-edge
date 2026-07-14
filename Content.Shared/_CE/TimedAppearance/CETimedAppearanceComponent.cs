using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.TimedAppearance;

/// <summary>
/// Tracks an in-progress timed appearance override on an entity.
/// Added by <see cref="CETimedAppearanceSystem.SetTimedAppearance"/> and removed automatically
/// when the timer expires or a new override is started.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CETimedAppearanceComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan EndTime;
}

[Serializable, NetSerializable]
public enum CEAnimationAppearanceVisuals : byte
{
    Key,
}
