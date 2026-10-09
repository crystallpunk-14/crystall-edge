namespace Content.Server._CE.GameTicking;

/// <summary>
/// Broadcast once <see cref="CEMurkConsumingRuleSystem"/> has rolled the round's prices, so anything
/// that lists them (e.g. the city objective's description) can be rewritten.
/// </summary>
public sealed class CEMainQuestPricesRolledEvent : EntityEventArgs;
