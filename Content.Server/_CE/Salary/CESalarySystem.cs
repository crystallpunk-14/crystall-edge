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
        SubscribeLocalEvent<CESalaryCounterComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CESalaryCounterComponent> ent, ref MapInitEvent args)
    {
        // Initialize the first salary time
        if (ent.Comp.NextSalaryTime == TimeSpan.Zero)
            ent.Comp.NextSalaryTime = _timing.CurTime + ent.Comp.Frequency;
    }

    /// <summary>
    /// Calculates and updates the accumulated unpaid salary based on elapsed time since last update.
    /// This method checks how many salary payment periods have passed and adds the corresponding amount.
    /// </summary>
    private void UpdateAccumulatedSalary(Entity<CESalaryCounterComponent> counter)
    {
        var currentTime = _timing.CurTime;

        // If no next salary time is set, initialize it
        if (counter.Comp.NextSalaryTime == TimeSpan.Zero)
        {
            counter.Comp.NextSalaryTime = currentTime + counter.Comp.Frequency;
            return;
        }

        // Calculate how many full salary periods have elapsed
        var elapsedTime = currentTime - counter.Comp.NextSalaryTime + counter.Comp.Frequency;
        if (elapsedTime <= TimeSpan.Zero)
            return;

        var periodsElapsed = (int)(elapsedTime / counter.Comp.Frequency);
        if (periodsElapsed <= 0)
            return;

        // Add accumulated salary for all elapsed periods
        counter.Comp.UnpaidSalary += counter.Comp.Salary * periodsElapsed;

        // Update next salary time, accounting for all elapsed periods
        counter.Comp.NextSalaryTime += counter.Comp.Frequency * periodsElapsed;
    }

    private void OnExamined(Entity<CESalaryPayrollComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<CESalaryCounterComponent>(args.Examiner, out var counter))
        {
            args.PushMarkup(Loc.GetString("ce-salary-payroll-examine-unsupported-job"));
            return;
        }

        // Update accumulated salary before displaying
        UpdateAccumulatedSalary((args.Examiner, counter));

        if (counter.UnpaidSalary <= 0)
            args.PushMarkup(Loc.GetString("ce-salary-payroll-examine-empty"));
        else
            args.PushMarkup(Loc.GetString("ce-salary-payroll-examine", ("count", _currency.GetCurrencyPrettyString(counter.UnpaidSalary))));

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

        // Update accumulated salary before withdrawal
        UpdateAccumulatedSalary((args.User, counter));

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
