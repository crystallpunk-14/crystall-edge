using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.RoundPortal;

[Serializable, NetSerializable]
public enum CERoundPortalUiKey : byte
{
    Key,
}

/// <summary>
/// Sent when the player confirms leaving the round through the portal.
/// </summary>
[Serializable, NetSerializable]
public sealed class CERoundPortalLeaveMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed partial class CERoundPortalLeaveDoAfterEvent : SimpleDoAfterEvent;
