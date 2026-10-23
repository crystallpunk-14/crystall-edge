using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.DarkVessel;

/// <summary>
/// An action that moves the performer's mind into the dead body of a player who held a secret role:
/// the body is fully healed and taken over together with that secret role, while the old body drops
/// dead. Handled entirely on the server by <c>CEDarkVesselSystem</c>.
/// </summary>
[RegisterComponent]
public sealed partial class CEDarkVesselActionComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Skills granted to the new body on top of the ones it already has.
    /// </summary>
    /// <remarks>
    /// TODO: a temporary workaround - skills live on the body, so the ones that belong to the vessel
    /// itself have to be re-granted by hand. Drop this once the skill system supports mind skills.
    /// </remarks>
    [DataField]
    public List<ProtoId<CESkillPrototype>> KeptSkills = new();
}

/// <summary>
/// Raised by an action with <see cref="CEDarkVesselActionComponent"/>.
/// </summary>
public sealed partial class CEDarkVesselActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class CEDarkVesselDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// The action that started the do-after - not passed as the do-after's "used" entity, since that
    /// one has to stay within reach of the user and the action never is.
    /// </summary>
    [NonSerialized]
    public EntityUid Action;
}
