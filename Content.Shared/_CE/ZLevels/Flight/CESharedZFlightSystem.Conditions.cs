
using Content.Shared.Standing;

namespace Content.Shared._CE.ZLevels.Flight;

public abstract partial class CESharedZFlightSystem
{
    private void InitializeConditions()
    {
        SubscribeLocalEvent<StandingStateComponent, CEStartFlightAttemptEvent>(OnStandingStartFlightAttempt);
    }


    private void OnStandingStartFlightAttempt(Entity<StandingStateComponent> ent, ref CEStartFlightAttemptEvent args)
    {
        if (ent.Comp.Standing) return;
        args.Cancel();
        _popup.PopupClient(Loc.GetString("ce-flight-lying-down"), ent, ent);
    }
}
