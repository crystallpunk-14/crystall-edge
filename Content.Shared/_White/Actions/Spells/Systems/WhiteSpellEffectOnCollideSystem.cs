using Content.Shared._CE.Actions.Spells;
using Content.Shared._White.Actions.Spells.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Shared._White.Actions.Spells.Systems;

public sealed class WhiteSpellEffectOnCollideSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhiteSpellEffectOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<WhiteSpellEffectOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (!_random.Prob(ent.Comp.Prob))
            return;

        if (ent.Comp.Whitelist is not null && !_whitelist.IsValid(ent.Comp.Whitelist, args.OtherEntity))
            return;

        var spellArgs = new CESpellEffectBaseArgs(null, ent, args.OtherEntity, Transform(args.OtherEntity).Coordinates);
        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(EntityManager, spellArgs);
        }
    }
}
