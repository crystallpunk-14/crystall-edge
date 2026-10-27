using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.LockKey;

[Serializable, NetSerializable]
public enum CELockKeyForgeUiKey : byte
{
    Key,
}

/// <summary>
/// Reconfigures the shape the forge stamps into crafted keys and locks.
/// Sent both by the manual editor and by picking a template.
/// </summary>
[Serializable, NetSerializable]
public sealed class CELockKeyForgeSetShapeMessage(List<int> shape) : BoundUserInterfaceMessage
{
    public readonly List<int> Shape = shape;
}

[Serializable, NetSerializable]
public sealed class CELockKeyForgeUiState(List<int> shape, int maxComplexity, EntProtoId? result) : BoundUserInterfaceState
{
    public readonly List<int> Shape = shape;
    public readonly int MaxComplexity = maxComplexity;

    /// <summary>
    /// What the forge crafts, shown as a preview.
    /// </summary>
    public readonly EntProtoId? Result = result;
}
