using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GameTicking.Components;

/// <summary>
/// Marks the body of a player's main character - the one spawned from the lobby, or the body a dark vessel moved into.
/// Only it earns achievements and statistics; ghost roles never do.
/// </summary>
[RegisterComponent, Access(typeof(CEMainCharacterSystem))]
public sealed partial class CEMainCharacterComponent : Component
{
    [DataField]
    public NetUserId Player;

    [DataField]
    public ProtoId<JobPrototype>? Job;
}
