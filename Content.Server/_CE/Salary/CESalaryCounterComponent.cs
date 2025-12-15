namespace Content.Server._CE.Salary;

[RegisterComponent, Access(typeof(CESalarySystem))]
public sealed partial class CESalaryCounterComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan NextSalaryTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan Frequency = TimeSpan.FromMinutes(20);

    [DataField]
    public int Salary = 100;

    [DataField]
    public int UnpaidSalary = 0;
}
