using Robust.Shared.GameStates;

namespace Content.Shared._CE.NewPlayer;

/// <summary>
/// Marks a body controlled by a new player. Only networked to viewers who aren't new players
/// themselves, so it drives both the status icon above the head and the examine text.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CENewPlayerComponent : Component
{
    public override bool SessionSpecific => true;
}
