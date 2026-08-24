using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicFocus.Components;
using Content.Shared._CE.MagicFocus.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._CE.MagicFocus;

public sealed partial class CEMagicFocusChargeEffectSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Predicted local spawn for the client performing the charge.
        SubscribeLocalEvent<CEMagicFocusComponent, CEMagicFocusChargedEvent>(OnCharged);
        // Networked broadcast so everyone else nearby sees the same effect.
        SubscribeNetworkEvent<CEMagicFocusChargeEffectEvent>(OnChargedNetwork);
    }

    private void OnCharged(Entity<CEMagicFocusComponent> ent, ref CEMagicFocusChargedEvent args)
    {
        SpawnEffects(ent.Owner, ent.Comp, args.Types);
    }

    private void OnChargedNetwork(CEMagicFocusChargeEffectEvent args)
    {
        var focus = GetEntity(args.Focus);
        if (!TryComp<CEMagicFocusComponent>(focus, out var comp))
            return;

        SpawnEffects(focus, comp, args.Types);
    }

    private void SpawnEffects(EntityUid focus, CEMagicFocusComponent comp, List<ProtoId<CEMagicEssenceTypePrototype>> types)
    {
        foreach (var type in types)
        {
            if (!_proto.TryIndex(type, out var essenceType))
                continue;

            var vfx = SpawnAtPosition(comp.ChargeEffect, Transform(focus).Coordinates);
            _transform.SetLocalRotation(vfx, _random.NextAngle());
            _sprite.SetColor(vfx, essenceType.Color);
        }
    }
}
