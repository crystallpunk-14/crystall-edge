using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science;

/// <summary>
/// Reveals a (2 * <see cref="Radius"/> + 1) square centered on (0,0) for every existing science
/// area, on top of the base reveal every entity gets when its research data component is
/// created. Used to give science-adjacent jobs (e.g. Academy students/professors) a head start
/// on the research map.
/// </summary>
public sealed partial class CEResearchDataSpecial : JobSpecial
{
    [DataField(required: true)]
    public int Radius;

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var science = entMan.System<CEScienceSystem>();

        var data = entMan.EnsureComponent<CEScienceResearchDataComponent>(mob);

        foreach (var area in proto.EnumeratePrototypes<CEScienceAreaPrototype>())
        {
            science.RevealArea((mob, data), area.ID, default, Radius);
        }
    }
}
