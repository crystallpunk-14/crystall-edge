namespace Content.Shared._CE.ThirdArm.Components;

[RegisterComponent]
public sealed partial class CEThirdArmModuleComponent : Component
{
    /// <summary>
    ///     Layers added to the third arm's own sprite (icon/ground/inventory) while this module is inserted.
    /// </summary>
    [DataField]
    public List<PrototypeLayerData> IconLayers = new();

    /// <summary>
    ///     Layers added to the wearer's sprite (equipped-NECK) while this module is inserted.
    /// </summary>
    [DataField]
    public List<PrototypeLayerData> EquippedLayers = new();

    /// <summary>
    ///     Charge per second drained from the arm's battery while this module is inserted. 0 = no passive drain.
    /// </summary>
    [DataField]
    public float PassiveDrainRate;
}
