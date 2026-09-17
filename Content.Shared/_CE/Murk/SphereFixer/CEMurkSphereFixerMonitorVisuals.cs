using Robust.Shared.Serialization;

namespace Content.Shared._CE.Murk.SphereFixer;

/// <summary>
/// Whether the Pillar of Light currently has blockers. Layer visibility is handled separately
/// via the engine's own <c>PowerDeviceVisuals.Powered</c> - this only switches which sprite
/// state (error/ok) is shown once powered.
/// </summary>
[Serializable, NetSerializable]
public enum CEMurkSphereFixerMonitorVisuals : byte
{
    Blocked,
}
