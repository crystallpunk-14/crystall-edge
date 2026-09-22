using Robust.Shared.GameStates;

namespace Content.Shared._CE.Objectives.Target.Components;

/// <summary>
/// Works with <see cref="CETargetObjectiveComponent"/> to select all living players as valid
/// target candidates.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(CETargetPlayersObjectiveSystem))]
public sealed partial class CETargetPlayersObjectiveComponent : Component;
