using Robust.Shared.GameStates;

namespace Content.Shared._CE.Vampire.Components;

/// <summary>
/// Requires that burning candles with the CEVampireCandleComponent component be placed around the spell's target.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class CEActionVampireCandlesComponent : Component
{
    [DataField]
    public int CandleCount = 3;

    [DataField]
    public float Range = 2f;
}

[RegisterComponent]
[NetworkedComponent]
public sealed partial class CEVampireCandleComponent : Component
{
}
