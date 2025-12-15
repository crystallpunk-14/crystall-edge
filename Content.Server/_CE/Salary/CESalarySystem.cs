using Content.Server._CE.Currency;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._CE.Salary;

public sealed partial class CESalarySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CECurrencySystem _currency = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESalaryPayrollComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CESalaryPayrollComponent, InteractHandEvent>(OnInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CESalaryCounterComponent>();
        while (query.MoveNext(out var ent, out var counter))
        {
            if (_timing.CurTime < counter.NextSalaryTime)
                continue;

            counter.NextSalaryTime = _timing.CurTime + counter.Frequency;
            counter.UnpaidSalary += counter.Salary;
        }
    }

    private void OnExamined(Entity<CESalaryPayrollComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<CESalaryCounterComponent>(args.Examiner, out var counter))
        {
            args.PushMarkup(Loc.GetString("ce-salary-payroll-examine-unsupported-job"));
            return;
        }

        if (counter.UnpaidSalary <= 0)
        {
            args.PushMarkup(Loc.GetString("ce-salary-payroll-examine-empty"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("ce-salary-payroll-examine", ("count", _currency.GetCurrencyPrettyString(counter.UnpaidSalary))));
        }

        //Timer
        var remainingToSalaryTime = counter.NextSalaryTime - _timing.CurTime;
        //time in format mm:ss
        var minutes = (int)remainingToSalaryTime.TotalMinutes;
        var seconds = remainingToSalaryTime.Seconds;

        args.PushMarkup(Loc.GetString("ce-salary-payroll-examine-timer", ("time", $"{minutes:D2}:{seconds:D2}")));
    }

    private void OnInteract(Entity<CESalaryPayrollComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<CESalaryCounterComponent>(args.User, out var counter))
        {
            _popup.PopupEntity(Loc.GetString("ce-salary-payroll-examine-unsupported-job"), args.User, args.User);
            return;
        }

        if (counter.UnpaidSalary <= 0)
        {
            _popup.PopupEntity(Loc.GetString("ce-salary-payroll-examine-empty"), args.User, args.User);
            return;
        }

        _audio.PlayPvs(ent.Comp.BuySound, Transform(ent).Coordinates);
        SpawnAtPosition(ent.Comp.BuyVisual, Transform(ent).Coordinates);

        _currency.GenerateMoney(counter.UnpaidSalary, Transform(ent).Coordinates);
        counter.UnpaidSalary = 0;
    }
}
