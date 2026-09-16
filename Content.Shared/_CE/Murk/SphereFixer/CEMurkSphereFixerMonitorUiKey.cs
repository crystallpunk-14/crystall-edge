using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Murk.SphereFixer;

[Serializable, NetSerializable]
public enum CEMurkSphereFixerMonitorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public readonly record struct CEMurkSphereFixerBlockerInfo(string Title, string Description, NetCoordinates Coordinates);

[Serializable, NetSerializable]
public sealed class CEMurkSphereFixerMonitorBoundUserInterfaceState(
    float charge,
    NetCoordinates? sphereCoordinates,
    List<CEMurkSphereFixerBlockerInfo> blockers) : BoundUserInterfaceState
{
    public readonly float Charge = charge;
    public readonly NetCoordinates? SphereCoordinates = sphereCoordinates;
    public readonly List<CEMurkSphereFixerBlockerInfo> Blockers = blockers;
}
