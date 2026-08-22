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

        SubscribeLocalEvent<CEMagicFocusComponent, CEMagicFocusChargedEvent>(OnCharged);
    }

    private void OnCharged(Entity<CEMagicFocusComponent> ent, ref CEMagicFocusChargedEvent args)
    {
        foreach (var type in args.Types)
        {
            if (!_proto.TryIndex(type, out var essenceType))
                continue;

            var vfx = SpawnAtPosition(ent.Comp.ChargeEffect, Transform(ent).Coordinates);
            _transform.SetLocalRotation(vfx, _random.NextAngle());
            _sprite.SetColor(vfx, essenceType.Color);
        }
    }
}
