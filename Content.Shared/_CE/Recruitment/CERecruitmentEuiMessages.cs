using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Recruitment;

[Serializable, NetSerializable]
public sealed class CERecruitmentChoiceMessage(bool accepted) : EuiMessageBase
{
    public readonly bool Accepted = accepted;
}
