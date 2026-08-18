using Content.Server._CE.MagicEssence.Systems;

namespace Content.Server._CE.MagicEssence.Components;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(CEMagicEssenceAttractorSystem))]
public sealed partial class CEPortableMagicEssenceAttractorComponent : Component
{
    [DataField]
    public float EnergyDraw = 2f;

    [DataField]
    public TimeSpan EnergyConsumeFrequency = TimeSpan.FromSeconds(1f);

    [DataField, AutoPausedField]
    public TimeSpan NextConsumeTime = TimeSpan.Zero;
}
