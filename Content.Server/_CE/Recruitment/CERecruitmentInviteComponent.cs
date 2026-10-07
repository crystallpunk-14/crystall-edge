using Content.Shared._CE.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.Recruitment;

/// <summary>
/// A pending recruitment invite on the invited player's body, waiting for them to answer the
/// dialog. See <see cref="CERecruitmentSystem"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(CERecruitmentSystem))]
public sealed partial class CERecruitmentInviteComponent : Component
{
    [DataField]
    public EntityUid Recruiter;

    [DataField]
    public ProtoId<CESecretRolePrototype> Role;

    /// <summary>
    /// When the invite runs out and counts as refused.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// The dialog shown to the invited player.
    /// </summary>
    [ViewVariables]
    public CERecruitmentEui? Eui;
}
