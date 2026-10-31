using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Character;

/// <summary>
/// Placed on the mind of a player who entered the round from the lobby. The mind outlives the body,
/// so the character stays known even after death, early leave or the body's destruction. Ghost roles
/// never get one. See <see cref="CECharacterSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(CECharacterSystem))]
public sealed partial class CECharacterComponent : Component
{
    /// <summary>
    /// The character's body. Null once another character took it over, e.g. a dark vessel moving in.
    /// </summary>
    [DataField]
    public EntityUid? Body;

    [DataField]
    public ProtoId<JobPrototype>? Job;
}

/// <summary>
/// Placed on the body of a <see cref="CECharacterComponent"/>, pointing back at its mind - so a body
/// leads to its character even when no mind sits in it anymore.
/// </summary>
[RegisterComponent, Access(typeof(CECharacterSystem))]
public sealed partial class CECharacterBodyComponent : Component
{
    [DataField]
    public EntityUid Mind;
}
