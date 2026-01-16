using Robust.Shared.Prototypes;

namespace Content.Server._CE.Sliceable;

/// <summary>
/// The backbone of any plant. Provides common variables for the plant to other components
/// </summary>
[RegisterComponent]
public sealed partial class CEAdditionalSliceableDropComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Loot = new();
}
