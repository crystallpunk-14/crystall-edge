using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.EntityEffect.Systems.StatusEffectApply;

/// <summary>
/// Holds configuration for periodically applying one or more CE entity effects
/// while a status effect is active on an entity.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEStatusEffectApplyEntityEffectComponent : Component
{
    [DataField(required: true)]
    public List<CEEntityEffect> Effects = new();

    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextApplyTime = TimeSpan.Zero;
}
