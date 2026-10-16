using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicVision.Components;

/// <summary>
/// Gives the entity an action that toggles magic vision on and off.
/// </summary>
[RegisterComponent]
public sealed partial class CEMagicVisionToggleComponent : Component
{
    [DataField]
    public EntProtoId Action = "CEActionToggleMagicVision";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public bool Enabled;
}
