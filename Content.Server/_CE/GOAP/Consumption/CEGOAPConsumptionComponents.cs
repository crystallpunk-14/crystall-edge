using Content.Shared._CE.GOAP.Consumption;

namespace Content.Server._CE.GOAP.Consumption;

[RegisterComponent]
public sealed partial class CEGOAPConsumeComponent : Component
{
    public CEGOAPConsumePhase Phase;
    public EntityUid? Target;
}

[RegisterComponent]
public sealed partial class CEGOAPConsumeRetryComponent : Component
{
    public readonly Dictionary<CEGOAPConsumeAction, TimeSpan> UntilByAction = new();
}

public enum CEGOAPConsumePhase : byte
{
    Acquiring,
    Consuming,
    Finished,
    Failed,
}
