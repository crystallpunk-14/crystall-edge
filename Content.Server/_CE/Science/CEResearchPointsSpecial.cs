using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.Science.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science;

/// <summary>
/// Grants a flat amount of research points per essence type, as configured in yaml. Used to give
/// science-adjacent jobs (e.g. Academy students/professors) a head start on research without
/// pre-revealing any part of the map.
/// </summary>
public sealed partial class CEResearchPointsSpecial : JobSpecial
{
    [DataField(required: true)]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Points = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var science = entMan.System<CEScienceSystem>();

        var data = entMan.EnsureComponent<CEScienceResearchDataComponent>(mob);

        science.GrantPoints((mob, data), Points);
    }
}
