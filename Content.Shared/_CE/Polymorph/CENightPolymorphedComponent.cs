namespace Content.Shared._CE.Polymorph;

/// <summary>
/// Tags the polymorphed form spawned by <see cref="CENightPolymorphComponent"/>, so
/// <see cref="Content.Server._CE.Polymorph.CENightPolymorphSystem"/> can find every one of them to
/// revert at dawn without tracking any state of its own.
/// </summary>
[RegisterComponent]
public sealed partial class CENightPolymorphedComponent : Component;
