using Robust.Shared.GameStates;

namespace Content.Shared._CE.EnergyExtractor;

/// <summary>
/// Marks an item as fuel for a <see cref="CEEnergyExtracterComponent"/>.
/// When the extractor splits this item, its battery gains <see cref="Energy"/> charge.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEEnergyExtractableComponent : Component
{
    /// <summary>
    /// Battery charge produced when an energy extractor destroys this item.
    /// </summary>
    [DataField(required: true)]
    public float Energy;
}
