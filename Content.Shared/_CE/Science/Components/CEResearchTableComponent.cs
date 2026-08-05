using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Marker component for research table UI
/// </summary>
[RegisterComponent]
public sealed partial class CEResearchTableComponent : Component
{
    [DataField]
    public string PaperSlotId = "paper";
}

[Serializable, NetSerializable]
public enum CEResearchTableVisuals : byte
{
    HasPaper,
}
