using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.RoundPortal;

/// <summary>
/// A portal that links the world with the outside: late joiners arrive through it, and players can use it
/// to leave the round early, freeing their job and secret role for others.
/// </summary>
[RegisterComponent]
public sealed partial class CERoundPortalComponent : Component
{
    /// <summary>
    /// Where late joiners appear relative to the portal, rotated with it.
    /// </summary>
    [DataField]
    public Vector2 SpawnOffset = new(0, -1);

    /// <summary>
    /// How long a player has to stand still at the portal to leave the round.
    /// </summary>
    [DataField]
    public TimeSpan LeaveDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier? ArrivalSound;

    [DataField]
    public SoundSpecifier? DepartureSound;

    [DataField]
    public EntProtoId? ArrivalEffect;

    [DataField]
    public EntProtoId? DepartureEffect;
}
