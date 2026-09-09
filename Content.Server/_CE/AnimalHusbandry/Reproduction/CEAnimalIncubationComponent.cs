namespace Content.Server._CE.AnimalHusbandry.Reproduction;

/// <summary>Physical egg; fertile prototypes compose TimerTrigger and hatching effects.</summary>
[RegisterComponent]
public sealed partial class CEAnimalIncubationComponent : Component
{
    [DataField] public bool Fertilized;
}
