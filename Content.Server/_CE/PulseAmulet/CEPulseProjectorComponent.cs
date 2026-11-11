using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.PulseAmulet;

/// <summary>
/// Machine that shows every charged pulse amulet on the island map, together with its wearer's condition.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(CEPulseProjectorSystem))]
public sealed partial class CEPulseProjectorComponent : Component
{
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
