using Content.Server._CE.Objectives.Target;

namespace Content.Server._CE.Objectives.Target.Components;

/// <summary>
/// Rejects target candidates (see <see cref="Content.Shared._CE.Objectives.Target.Components.CETargetObjectiveComponent"/>)
/// who hold a secret role in the same <c>CESecretDepartmentPrototype</c> as the objective's holder -
/// e.g. so a Nightmare's hunting objective can't target a fellow Nightmare.
/// </summary>
[RegisterComponent]
[Access(typeof(CETargetExcludeHolderDepartmentSystem))]
public sealed partial class CETargetExcludeHolderDepartmentComponent : Component;
