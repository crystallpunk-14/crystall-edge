namespace Content.Server._CE.Bluetext;

/// <summary>
/// A component added to the antagonist player's mind that allows them to write flavor text about the progress of the goal's completion.
/// </summary>
[RegisterComponent, Access(typeof(CEBlueTextSystem))]
public sealed partial class CEBlueTextTrackerComponent : Component
{
    [DataField]
    public string BlueText = string.Empty;
}
