namespace Content.Server._CE.GameTicking;

/// <summary>
/// The narrative "round start" beat. Purely cosmetic;
/// other systems (e.g. secret role reveal popups) hook their own reactions off of it.
/// </summary>
public sealed class CERoundStartEvent : EntityEventArgs;
