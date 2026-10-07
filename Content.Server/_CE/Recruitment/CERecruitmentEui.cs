using Content.Server.EUI;
using Content.Shared._CE.Recruitment;
using Content.Shared.Eui;

namespace Content.Server._CE.Recruitment;

/// <summary>
/// Accept/decline dialog for a recruitment invite. Closing it any other way counts as declining.
/// </summary>
public sealed class CERecruitmentEui(EntityUid target, CERecruitmentSystem recruitment) : BaseEui
{
    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        recruitment.ResolveInvite(target, msg is CERecruitmentChoiceMessage { Accepted: true }, closeEui: false);
        Close();
    }

    public override void Closed()
    {
        base.Closed();

        // No-op if the invite was already answered.
        recruitment.ResolveInvite(target, false, closeEui: false);
    }
}
