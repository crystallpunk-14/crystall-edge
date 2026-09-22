namespace Content.Server._CE.Objectives.Components;

/// <summary>
/// Marks an objective that tracks cumulative spend at black-market trading platforms - see
/// <see cref="Content.Server._CE.Objectives.Systems.CEObjectiveBlackMarketSpendConditionSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEObjectiveBlackMarketSpendConditionComponent : Component
{
    /// <summary>
    /// How many gold coins' worth need to be spent for this objective to complete.
    /// </summary>
    [DataField(required: true)]
    public int TargetGoldSteal;

    /// <summary>
    /// Running total spent so far, in copper-equivalent currency units (100 per gold coin - same
    /// unit as <see cref="Content.Shared._CE.Currency.CESharedCurrencySystem.GP"/>).
    /// </summary>
    [DataField]
    public int AmountSpent;
}
