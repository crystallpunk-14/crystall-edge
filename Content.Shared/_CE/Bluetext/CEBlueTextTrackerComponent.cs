using Robust.Shared.GameStates;

namespace Content.Shared._CE.BlueText;

/// <summary>
/// A component added to the antagonist player's mind that allows them to write flavor text about the progress of the goal's completion.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CESharedBlueTextSystem))]
public sealed partial class CEBlueTextTrackerComponent : Component
{
    [DataField]
    public string BlueText = string.Empty;
}
