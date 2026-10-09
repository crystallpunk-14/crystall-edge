using Robust.Shared.GameStates;

namespace Content.Shared._CE.Objectives.Target.Components;

/// <summary>
/// Works with <see cref="CETargetObjectiveComponent"/> to select the objective holder's own body as
/// the only target candidate.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(CETargetHolderObjectiveSystem))]
public sealed partial class CETargetHolderObjectiveComponent : Component;
