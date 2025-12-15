using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Salary;

/// <summary>
/// Component for salary payroll terminals that allow players to collect their accumulated unpaid salary.
/// When a player with <see cref="CESalaryCounterComponent"/> interacts with this entity,
/// they can withdraw their accumulated unpaid salary as currency items.
/// </summary>
[RegisterComponent, Access(typeof(CESalarySystem))]
public sealed partial class CESalaryPayrollComponent : Component
{
    /// <summary>
    /// Sound effect played when a player successfully collects their salary from the payroll terminal.
    /// </summary>
    [DataField]
    public SoundSpecifier BuySound = new SoundPathSpecifier("/Audio/_CE/Effects/cash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f),
    };

    /// <summary>
    /// Visual effect prototype spawned at the terminal location when salary is collected.
    /// Provides visual feedback for the salary withdrawal transaction.
    /// </summary>
    [DataField]
    public EntProtoId BuyVisual = "CECashImpact";
}
