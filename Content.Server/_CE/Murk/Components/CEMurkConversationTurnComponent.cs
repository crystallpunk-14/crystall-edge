namespace Content.Server._CE.Murk.Components;

/// <summary>
/// Present on a conversation participant while it is its turn to speak.
/// </summary>
[RegisterComponent, Access(typeof(CEMurkConversationSystem))]
public sealed partial class CEMurkConversationTurnComponent : Component;
