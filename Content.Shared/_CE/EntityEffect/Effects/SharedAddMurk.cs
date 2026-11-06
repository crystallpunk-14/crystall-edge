namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Feeds the murk that consumes the Lucson Sphere. Only counts while the sphere is cracked.
/// </summary>
public sealed partial class AddMurk : CEEntityEffectBase<AddMurk>
{
    /// <summary>
    /// Fraction of the whole murk, or the number of player-scaled contributions if <see cref="ScaleWithPlayers"/>.
    /// </summary>
    [DataField]
    public float Amount = 1f;

    /// <summary>
    /// If true, the murk fills after a share of the round's peak online worth of contributions
    /// (a victim per a few players); otherwise <see cref="Amount"/> is a fixed fraction of the murk.
    /// </summary>
    [DataField]
    public bool ScaleWithPlayers;
}
