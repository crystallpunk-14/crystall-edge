using Content.Shared.Actions;

namespace Content.Shared._CE.Recruitment;

/// <summary>
/// Raised by an action with <see cref="CERecruitmentActionComponent"/> once its target passed validation.
/// </summary>
public sealed partial class CERecruitActionEvent : EntityTargetActionEvent;
