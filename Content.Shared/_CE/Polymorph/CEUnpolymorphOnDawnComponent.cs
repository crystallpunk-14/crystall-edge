namespace Content.Shared._CE.Polymorph;

/// <summary>
/// Tags a polymorph target prototype (or any polymorphed form) as one that should automatically
/// revert back to normal at dawn - read by <see cref="Content.Server._CE.Polymorph.CENightPolymorphSystem"/>,
/// which finds every one of them without tracking any state of its own.
/// </summary>
[RegisterComponent]
public sealed partial class CEUnpolymorphOnDawnComponent : Component;
