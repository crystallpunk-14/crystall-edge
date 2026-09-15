namespace Content.Server._CE.Murk;

/// <summary>
/// Makes a murked soul mutter to itself and strike up conversations with whoever is nearby,
/// in a language of murk gibberish sprinkled with words it overheard.
/// </summary>
[RegisterComponent]
public sealed partial class CEMurkedSoulSpeechComponent : Component
{
    [DataField] public float MinSpeechInterval = 12f;
    [DataField] public float MaxSpeechInterval = 30f;
    [DataField] public float TargetSearchRadius = 4f;
    [DataField] public float AccentPickupRadius = 6f;
    [DataField] public float WordChance = 0.5f;
    [DataField] public int MinWords = 2;
    [DataField] public int MaxWords = 4;
    [DataField] public int MaxHeardWords = 10;
    [DataField] public int MinWordLength = 4;
    [DataField] public float ConversationChance = 0.45f;
    [DataField] public int ConversationTurnPairs = 2;
    [DataField] public float MinTurnDelay = 1.2f;
    [DataField] public float MaxTurnDelay = 4.0f;
    [DataField] public float MaxSpeakPause = 8f;

    public TimeSpan NextSpeechAt;

    /// <summary>
    /// Words picked up from nearby speech, recycled into the soul's own muttering.
    /// </summary>
    [ViewVariables] public HashSet<string> HeardWords = new();

    public EntityUid? ConversationPartner;
    public int ConversationTurnsLeft;
    public bool IsMyTurn;
    public TimeSpan NextTurnAt;
    public bool IsPlayerControlled;
    public TimeSpan SpeakingUntil;
    public bool InConversation => ConversationPartner.HasValue;
}
