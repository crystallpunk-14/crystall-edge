using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Recruitment;

[Serializable, NetSerializable]
public sealed class CERecruitmentChoiceMessage(bool accepted) : EuiMessageBase
{
    public readonly bool Accepted = accepted;
}

/// <summary>
/// When the invite runs out (server CurTime) and how long it lasted in total - drives the countdown bar.
/// </summary>
[Serializable, NetSerializable]
public sealed class CERecruitmentEuiState(TimeSpan endTime, TimeSpan duration) : EuiStateBase
{
    public readonly TimeSpan EndTime = endTime;
    public readonly TimeSpan Duration = duration;
}
