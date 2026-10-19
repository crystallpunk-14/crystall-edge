using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Roundflow;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;

namespace Content.Server._CE.DayCycle;

/// <summary>
/// Counts days on every day cycle map and announces "Day N" / "Night N" when they change on the station surface
/// </summary>
public sealed partial class CEDayCounterSystem : EntitySystem
{
    private static readonly SoundSpecifier DaySound = new SoundPathSpecifier("/Audio/_CE/Announce/event_boom.ogg");
    private static readonly SoundSpecifier NightSound = new SoundPathSpecifier("/Audio/_CE/Announce/darkness_boom.ogg");

    [SubscribeLocalEvent]
    private void OnStartDay(Entity<CEDayCycleComponent> ent, ref CEStartDayEvent args)
    {
        ent.Comp.DayNumber++;
        Dirty(ent);

        Announce(ent, "ce-day-cycle-day", DaySound);
    }

    [SubscribeLocalEvent]
    private void OnStartNight(Entity<CEDayCycleComponent> ent, ref CEStartNightEvent args)
    {
        Announce(ent, "ce-day-cycle-night", NightSound);
    }

    private void Announce(Entity<CEDayCycleComponent> ent, string title, SoundSpecifier sound)
    {
        if (ent.Comp.DayNumber <= 0)
            return;

        if (TryComp<CEZMapComponent>(ent, out var zMap) && zMap.Depth != 0)
            return;

        if (!HasComp<StationMemberComponent>(ent))
            return;

        RaiseNetworkEvent(new CEScreenPopupShowEvent(Loc.GetString(title, ("number", ent.Comp.DayNumber)), "", sound));
    }
}
