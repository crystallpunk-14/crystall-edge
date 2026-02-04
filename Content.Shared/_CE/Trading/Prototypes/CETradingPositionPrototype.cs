using Content.Shared._CE.Workbench.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Prototypes;

[Prototype("tradingPosition")]
public sealed partial class CETradingPositionPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Service Title. If you leave null, the name will try to generate from first Service.GetName()
    /// </summary>
    [DataField]
    public LocId? Name;

    /// <summary>
    /// Service Description. If you leave null, the description will try to generate from first Service.GetDescription()
    /// </summary>
    [DataField]
    public LocId? Desc;

    [DataField(required: true)]
    public ProtoId<CETradingFactionPrototype> Faction;

    [DataField(required: true)]
    public CEStoreBuyService Service = default!;

    [DataField]
    public int PriceMarkup = 1;

    /// <summary>
    /// each round prices will differ within +X percent of the calculated value
    /// </summary>
    [DataField]
    public float PriceFluctuation = 0.6f;

    [DataField]
    public ProtoId<CEWorkbenchRecipeCategoryPrototype>? Category;
}

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEStoreBuyService
{
    public abstract void Buy(EntityManager entManager, IPrototypeManager prototype,  EntityUid platform);

    public abstract string GetName(IPrototypeManager protoMan);

    public abstract string GetDesc(IPrototypeManager protoMan);

    public abstract EntProtoId GetTexture(IPrototypeManager protoMan);
}
