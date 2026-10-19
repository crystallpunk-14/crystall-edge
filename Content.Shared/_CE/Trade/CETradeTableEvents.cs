using Content.Shared._CE.EntityEffect.Effects;

namespace Content.Shared._CE.Trade;

/// <summary>
/// Puts a new offer into one of the trade table's empty slots - see <see cref="CESharedTradeSystem.TryRestock"/>.
/// </summary>
public sealed partial class CERestockTradeTableEvent : CEEntityEffectRaisedEvent;

/// <summary>
/// Removes a random offer from the trade table - see <see cref="CESharedTradeSystem.TryClearOne"/>.
/// </summary>
public sealed partial class CEClearTradeTableEvent : CEEntityEffectRaisedEvent;
