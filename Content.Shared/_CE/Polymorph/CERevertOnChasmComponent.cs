namespace Content.Shared._CE.Polymorph;

/// <summary>
/// Polymorphed form that reverts to its original body instead of falling into a Z-level chasm.
/// The original body then falls on its own, so its equipment (e.g. safe-fall amulets) gets a chance to save it.
/// Handled by <see cref="Content.Server._CE.Polymorph.CERevertOnChasmSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CERevertOnChasmComponent : Component;
