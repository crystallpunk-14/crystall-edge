using Content.Shared._CE.WildMagic;
using Content.Shared._CE.WildMagic.Components;
using Content.Shared._CE.WildMagic.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.WildMagic;

public sealed class CEClientWildMagicSystem : CESharedWildMagicSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEWildMagicNodeComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<CEWildMagicNodeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnAfterHandleState(Entity<CEWildMagicNodeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnMapInit(Entity<CEWildMagicNodeComponent> ent, ref MapInitEvent args)
    {
        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<CEWildMagicNodeComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        foreach (var key in ent.Comp.RevealedLayers)
        {
            _sprite.RemoveLayer((ent, sprite), key);
        }

        ent.Comp.RevealedLayers.Clear();

        var counter = 0;
        foreach (var typeId in ent.Comp.Types.Keys)
        {
            if (!_proto.TryIndex<CEWildMagicTypePrototype>(typeId, out var type))
                continue;

            foreach (var layer in type.Icon)
            {
                var keyCode = $"ce-wild-magic-layer-{counter}";
                ent.Comp.RevealedLayers.Add(keyCode);

                _sprite.AddBlankLayer((ent, sprite), counter);
                _sprite.LayerMapSet((ent, sprite), keyCode, counter);
                _sprite.LayerSetData((ent, sprite), counter, layer);

                counter++;
            }
        }
    }
}
