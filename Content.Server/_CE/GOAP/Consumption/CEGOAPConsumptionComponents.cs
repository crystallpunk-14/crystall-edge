using Content.Shared._CE.Consumption;

namespace Content.Server._CE.GOAP.Consumption;

[RegisterComponent]
public sealed partial class CEGOAPConsumeComponent : Component
{
    public CEGOAPConsumePhase Phase;
    public CEConsumableSource SourceDefinition = default!;
    public EntityUid? Provider;
    public EntityUid? Consumable;
}

[RegisterComponent]
public sealed partial class CEGOAPConsumeRetryComponent : Component
{
    public readonly Dictionary<CEConsumableSource, TimeSpan> UntilBySource =
        new(ReferenceEqualityComparer.Instance);
}

public enum CEGOAPConsumePhase : byte
{
    Acquiring,
    Consuming,
    Finished,
    Failed,
}
