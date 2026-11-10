using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.PulseAmulet;

[Serializable, NetSerializable]
public enum CEPulseProjectorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum CEPulseMarkerStatus : byte
{
    Worn,
    Removed,
    Critical,
    Dead,
}

[Serializable, NetSerializable]
public readonly record struct CEPulseMarker(NetCoordinates Coordinates, CEPulseMarkerStatus Status);

[Serializable, NetSerializable]
public sealed class CEPulseProjectorBoundUserInterfaceState(List<CEPulseMarker> markers) : BoundUserInterfaceState
{
    public readonly List<CEPulseMarker> Markers = markers;
}

/// <summary>
/// True while any amulet wearer is in critical condition or dead. Switches the projector's status layer.
/// </summary>
[Serializable, NetSerializable]
public enum CEPulseProjectorVisuals : byte
{
    Alert,
}
