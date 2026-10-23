using Robust.Shared.Analyzers;
using Content.Shared._CE.Actions.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Examine;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem
{

    [SubscribeLocalEvent]
    private void OnStaminaCostExamined(Entity<CEActionStaminaCostComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup($"{Loc.GetString("ce-magic-staminacost")}: [color=#90ee90]{ent.Comp.Cost}[/color]", priority: 9);
    }

    [SubscribeLocalEvent]
    private void OnActionExamined(Entity<ActionComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.UseDelay is not { } delay)
            return;

        var time = Loc.GetString("ce-magic-cooldown-time", ("minutes", (int) delay.TotalMinutes), ("seconds", delay.Seconds));
        args.PushMarkup($"{Loc.GetString("ce-magic-cooldown")}: [color=#5da9e8]{time}[/color]", priority: 9);
    }

    [SubscribeLocalEvent]
    private void OnManacostExamined(Entity<CEActionManaCostComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup($"{Loc.GetString("ce-magic-manacost")}: [color=#5da9e8]{ent.Comp.ManaCost}[/color]", priority: 9);
    }

    [SubscribeLocalEvent]
    private void OnWeaponRequiredExamined(Entity<CEActionWeaponRequiredComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ce-magic-weapon-required"), 8);
    }
}
