using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Prototypes;

/// <summary>
/// A budget "currency" a demiplane modifier can spend from — just a name, no other data. See
/// <see cref="CEDemiplaneModifierPrototype.Categories"/>.
/// </summary>
[Prototype("demiplaneModifierCategory")]
public sealed partial class CEDemiplaneModifierCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
