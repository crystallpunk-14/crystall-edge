namespace Content.Server._CE.MagicEnergy;


[RegisterComponent]
[Access(typeof(CEMagicEnergySystem))]
public sealed partial class CEEnergyRadiationArmorComponent : Component
{
    [DataField]
    public float Armor = 0f;
}
