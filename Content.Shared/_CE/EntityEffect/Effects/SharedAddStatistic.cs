namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Adds to a player statistic of the player controlling the effect target.
/// <see cref="Statistic"/> is the name of a field in <c>Content.Server._CE.Statistics.CEStatistics</c>.
/// </summary>
public sealed partial class AddStatistic : CEEntityEffectBase<AddStatistic>
{
    [DataField(required: true)]
    public string Statistic = string.Empty;

    [DataField]
    public long Amount = 1;
}
