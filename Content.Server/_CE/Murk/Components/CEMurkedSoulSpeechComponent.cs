namespace Content.Server._CE.Murk.Components;

/// <summary>
/// Makes a murked soul speak murk gibberish sprinkled with words it overheard.
/// </summary>
[RegisterComponent]
public sealed partial class CEMurkedSoulSpeechComponent : Component
{
    [DataField] public float AccentPickupRadius = 6f;
    [DataField] public float WordChance = 0.5f;
    [DataField] public int MinWords = 2;
    [DataField] public int MaxWords = 4;
    [DataField] public int MaxHeardWords = 10;
    [DataField] public int MinWordLength = 4;

    /// <summary>
    /// Words picked up from nearby speech, recycled into the soul's own muttering.
    /// </summary>
    [ViewVariables] public HashSet<string> HeardWords = new();
}
