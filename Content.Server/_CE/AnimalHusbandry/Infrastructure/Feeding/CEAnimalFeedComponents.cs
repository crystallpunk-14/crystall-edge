using Robust.Shared.Localization;

namespace Content.Server._CE.AnimalHusbandry.Infrastructure.Feeding;

/// <summary>
/// Domain rules for a feed trough backed by reusable fixed entity slots.
/// </summary>
[RegisterComponent]
public sealed partial class CEFeedTroughComponent : Component
{
    [DataField(required: true)]
    public LocId ExamineMessage;

    [DataField(required: true)]
    public int NutritionPrecision;

    /// <summary>
    /// Runtime claims prevent multiple consumers from resolving the same visible occupant.
    /// The value identifies the consumer that may keep resolving its own claim across
    /// independently deserialized source strategies. Slot membership remains authoritative.
    /// </summary>
    public readonly Dictionary<EntityUid, EntityUid> ReservedFood = new();
}
