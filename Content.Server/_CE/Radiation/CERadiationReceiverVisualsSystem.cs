using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared._CE.Radiation;
using Robust.Shared.Analyzers;

namespace Content.Server._CE.Radiation;

public sealed partial class CERadiationReceiverVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnUpdate(RadiationSystemUpdatedEvent ev)
    {
        var query = EntityQueryEnumerator<CERadiationReceiverVisualsComponent, RadiationReceiverComponent>();
        while (query.MoveNext(out var uid, out var visuals, out var receiver))
        {
            var active = receiver.CurrentRadiation > 0;

            if (visuals.Active == active)
                continue;

            visuals.Active = active;
            _appearance.SetData(uid, CERadiationReceiverVisuals.Active, active);
        }
    }
}
