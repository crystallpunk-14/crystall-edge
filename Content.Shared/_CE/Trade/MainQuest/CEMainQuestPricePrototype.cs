using Content.Shared._CE.ResourceManager;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trade.MainQuest;

/// <summary>
/// One entry of the price pool: what the city brings to a quest postament for a shard or the restoration book.
/// </summary>
[Prototype("mainQuestPrice")]
public sealed partial class CEMainQuestPricePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Department whose work produces this price. The round picks prices from different departments when it can.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Department;

    [DataField]
    public List<CEResourceRequirement> Cost = new();

    /// <summary>
    /// Coins paid on top of <see cref="Cost"/>.
    /// </summary>
    [DataField]
    public int Pay;
}
