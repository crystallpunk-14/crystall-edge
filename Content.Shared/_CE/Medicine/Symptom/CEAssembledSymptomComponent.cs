namespace Content.Shared._CE.Medicine.Symptom;

/// <summary>
/// Marks a symptom status effect granted by symptom assembly. Only marked symptoms are removed once no source needs them,
/// and their own <see cref="CESymptomSourceComponent"/> is ignored.
/// </summary>
[RegisterComponent]
public sealed partial class CEAssembledSymptomComponent : Component;
