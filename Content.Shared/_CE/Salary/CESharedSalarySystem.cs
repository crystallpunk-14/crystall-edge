using Content.Shared._CE.Currency;
using Content.Shared.Examine;

namespace Content.Shared._CE.Salary;

public abstract partial class CESharedSalarySystem : EntitySystem
{
    [Dependency] protected CESharedCurrencySystem Currency = default!;

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CESalaryComponent> ent, ref ExaminedEvent args)
    {
        // Above the cooldown line (priority 9)
        var markup = ent.Comp.Unpaid > 0
            ? Loc.GetString("ce-salary-examine-unpaid", ("amount", Currency.GetCurrencyPrettyString(ent.Comp.Unpaid)))
            : Loc.GetString("ce-salary-examine-next-dawn");

        args.PushMarkup(markup, priority: 10);
    }
}
