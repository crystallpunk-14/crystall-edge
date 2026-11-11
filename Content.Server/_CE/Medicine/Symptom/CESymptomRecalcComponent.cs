namespace Content.Server._CE.Medicine.Symptom;

/// <summary>
/// Bearer flag: a symptom source was applied or removed, symptoms are reassembled on the next tick of <see cref="CESymptomSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(CESymptomSystem))]
public sealed partial class CESymptomRecalcComponent : Component;
