namespace Content.Shared._CE.Consumption;

/// <summary>
/// Composes independent source strategies and selects their globally nearest
/// provider. Source order breaks equal-distance ties. If provider categories
/// overlap, the first matching leaf owns resolution exclusively.
/// </summary>
[DataDefinition]
public sealed partial class CECompositeConsumableSource
    : CEConsumableSourceBase<CECompositeConsumableSource>
{
    [DataField(required: true)]
    public List<CEConsumableSource> Sources = new();
}
