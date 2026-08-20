using Content.Shared._CE.EntityEffect;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Animation.Effects;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEEntityEffectSpawnerComponent : Component
{
    [DataField]
    public List<CEEntityEffect> Effects = new();

    [DataField]
    public TimeSpan FirstDelay = TimeSpan.FromSeconds(1f);

    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextEffectTime = TimeSpan.Zero;
}
