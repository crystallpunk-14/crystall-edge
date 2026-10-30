namespace Content.Server._CE.NewPlayer;

/// <summary>
/// Marks a mind whose player was a new player when first put into a body this round - keeps them
/// a new player until the round ends, even if they reach the playtime threshold mid-round.
/// </summary>
[RegisterComponent]
public sealed partial class CENewPlayerMindComponent : Component;
