namespace Content.Server._CE.AnimalHusbandry.Reproduction;

/// <summary>Limited fertilized products granted by mating and spent only after laying.</summary>
[RegisterComponent]
public sealed partial class CEAnimalFertilityComponent : Component
{
    [DataField] public int ProductsRemaining;
}
