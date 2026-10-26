using Content.Shared.Examine;

namespace Content.Shared._DND.Classes;

public sealed partial class DndClericSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnExamine(EntityUid uid, DndThaumaturgeComponent component, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString(
            "dnd-thaumaturge-examine",
            ("first", Loc.GetString(component.FirstSkill)),
            ("second", Loc.GetString(component.SecondSkill)),
            ("bonus", component.Bonus)));
    }
}
