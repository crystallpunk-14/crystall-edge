using System.Linq;
using Content.Server._CE.Currency;
using Content.Server._CE.Trade;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Salary;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server._CE.Salary;

/// <summary>
/// Accrues <see cref="CESalaryComponent"/> salary at every dawn and pays it out through the salary action.
/// </summary>
public sealed partial class CESalarySystem : CESharedSalarySystem
{
    [Dependency] private CECurrencySystem _currency = default!;
    [Dependency] private CETradeSystem _trade = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<CESalaryComponent> _salaryQuery = default!;

    [SubscribeLocalEvent]
    private void OnStartDay(CEGlobalStartDayEvent args)
    {
        var query = EntityQueryEnumerator<CESalaryComponent>();
        while (query.MoveNext(out var uid, out var salary))
        {
            salary.Unpaid += salary.Salary;
            Dirty(uid, salary);
        }
    }

    [SubscribeLocalEvent]
    private void OnSalaryAction(CESalaryActionEvent args)
    {
        if (args.Handled || !_salaryQuery.TryComp(args.Action, out var salary))
            return;

        args.Handled = true;
        var user = args.Performer;

        if (salary.Unpaid <= 0)
        {
            _popup.PopupEntity(Loc.GetString("ce-salary-empty"), user, user);
            return;
        }

        var coords = Transform(user).Coordinates;
        var coins = _currency.GenerateMoney(salary.Unpaid, coords).ToList();
        _trade.Deliver(coins, user, _transform.GetMapCoordinates(user), coords);
        _audio.PlayPvs(salary.PaySound, coords);

        var amount = FormattedMessage.RemoveMarkupPermissive(_currency.GetCurrencyPrettyString(salary.Unpaid)).Trim();
        _popup.PopupEntity(Loc.GetString("ce-salary-paid", ("amount", amount)), user, user);

        salary.Unpaid = 0;
        Dirty(args.Action, salary);
    }
}
