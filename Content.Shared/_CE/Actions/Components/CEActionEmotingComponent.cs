namespace Content.Shared._CE.Actions.Components;

[RegisterComponent]
public sealed partial class CEActionEmotingComponent : Component
{
    [DataField]
    public string StartEmote = string.Empty; //Not LocId!

    [DataField]
    public string EndEmote = string.Empty; //Not LocId!
}
