namespace Content.Server._CE.Salary;

/// <summary>
/// Component that tracks salary accumulation for entities (typically players) over time.
/// Automatically increments unpaid salary at regular intervals based on configured frequency and amount.
/// The accumulated salary can be withdrawn at a <see cref="CESalaryPayrollComponent"/> terminal.
/// </summary>
[RegisterComponent, Access(typeof(CESalarySystem)), AutoGenerateComponentPause]
public sealed partial class CESalaryCounterComponent : Component
{
    /// <summary>
    /// The timestamp when the next salary increment should be added to <see cref="UnpaidSalary"/>.
    /// Updated automatically by <see cref="CESalarySystem"/> each time salary is paid.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextSalaryTime = TimeSpan.Zero;

    /// <summary>
    /// Time interval between automatic salary payments.
    /// </summary>
    [DataField]
    public TimeSpan Frequency = TimeSpan.FromMinutes(12);

    /// <summary>
    /// Amount of currency to add to <see cref="UnpaidSalary"/> each time <see cref="Frequency"/> elapses.
    /// This is the base salary rate per payment period.
    /// </summary>
    [DataField]
    public int Salary = 100;

    /// <summary>
    /// Current accumulated unpaid salary that can be withdrawn at a payroll terminal.
    /// Automatically incremented by <see cref="Salary"/> amount every <see cref="Frequency"/>.
    /// Reset to 0 when collected at <see cref="CESalaryPayrollComponent"/>.
    /// </summary>
    [DataField]
    public int UnpaidSalary = 0;
}
