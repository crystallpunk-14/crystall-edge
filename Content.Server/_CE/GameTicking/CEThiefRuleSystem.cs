using Content.Server._CE.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared._CE.Roles;
using Content.Shared._CE.Thief;
using Content.Shared.Foldable;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Containers;

namespace Content.Server._CE.GameTicking;

public sealed partial class CEThiefRuleSystem : GameRuleSystem<CEThiefRuleComponent>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEThiefHideoutComponent, FoldedEvent>(OnFolded);
    }

    /// <summary>
    /// When spawning, we look for the nearest thief player and try to attach ourselves to them.
    /// </summary>
    private void OnFolded(Entity<CEThiefHideoutComponent> ent, ref FoldedEvent args)
    {
        if (args.IsFolded || ent.Comp.ThiefMind is not null)
            return;

        var minds = _lookup.GetEntitiesInRange<MindContainerComponent>(Transform(ent).Coordinates, ent.Comp.ScanRange);

        foreach (var mindContainer in minds)
        {
            if (!_mind.TryGetMind(mindContainer, out var mindId, out var mindComp, mindContainer.Comp))
                continue;
            if (!_role.MindHasRole<CEThiefRoleComponent>(mindId))
                continue;

            ent.Comp.ThiefMind = mindId;
            return;
        }
    }
}
