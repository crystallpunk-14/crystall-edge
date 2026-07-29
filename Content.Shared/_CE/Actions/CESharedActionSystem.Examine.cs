using Content.Shared._CE.Actions.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Examine;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem
{
    private void InitializeExamine()
    {
        SubscribeLocalEvent<ActionComponent, ExaminedEvent>(OnActionExamined);
        SubscribeLocalEvent<CEActionManaCostComponent, ExaminedEvent>(OnManacostExamined);
        SubscribeLocalEvent<CEActionStaminaCostComponent, ExaminedEvent>(OnStaminaCostExamined);

        SubscribeLocalEvent<CEActionWeaponRequiredComponent, ExaminedEvent>(OnWeaponRequiredExamined);
    }

    private void OnStaminaCostExamined(Entity<CEActionStaminaCostComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup($"{Loc.GetString("ce-magic-staminacost")}: [color=#90ee90]{ent.Comp.Cost}[/color]", priority: 9);
    }

    private void OnActionExamined(Entity<ActionComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.UseDelay is null)
            return;
        args.PushMarkup($"{Loc.GetString("ce-magic-cooldown")}: [color=#5da9e8]{ent.Comp.UseDelay.Value.TotalSeconds}s[/color]", priority: 9);
    }

    private void OnManacostExamined(Entity<CEActionManaCostComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup($"{Loc.GetString("ce-magic-manacost")}: [color=#5da9e8]{ent.Comp.ManaCost}[/color]", priority: 9);
    }

    private void OnWeaponRequiredExamined(Entity<CEActionWeaponRequiredComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ce-magic-weapon-required"), 8);
    }
}
