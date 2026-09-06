using Content.Server._CE.Production;
using Content.Shared._CE.GOAP.Components;

namespace Content.Server._CE.GOAP.Sensors;

/// <summary>
/// Adapts production state changes to GOAP sensor invalidation without making
/// the production domain depend on GOAP.
/// </summary>
public sealed partial class CEGOAPProductionStateRefreshSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEGOAPComponent, CEProductionStateChangedEvent>(OnProductionStateChanged);
    }

    private void OnProductionStateChanged(
        Entity<CEGOAPComponent> ent,
        ref CEProductionStateChangedEvent args)
    {
        var refresh = new CEGOAPSensorRefreshEvent();
        RaiseLocalEvent(ent.Owner, ref refresh);
    }
}
