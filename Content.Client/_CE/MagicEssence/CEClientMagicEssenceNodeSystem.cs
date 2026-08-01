using Content.Shared._CE.MagicEssence.Components;
using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.MagicEssence;

public sealed partial class CEClientMagicEssenceNodeSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMagicEssenceNodeComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<CEMagicEssenceNodeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnAfterHandleState(Entity<CEMagicEssenceNodeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnMapInit(Entity<CEMagicEssenceNodeComponent> ent, ref MapInitEvent args)
    {
        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<CEMagicEssenceNodeComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        SetLayerColor((ent, sprite), ent.Comp.EssenceALayer, ent.Comp.EssenceA);
        SetLayerColor((ent, sprite), ent.Comp.EssenceBLayer, ent.Comp.EssenceB);
        SetLayerColor((ent, sprite), ent.Comp.EssenceCLayer, ent.Comp.EssenceC);
    }

    private void SetLayerColor(Entity<SpriteComponent?> sprite, string layer, ProtoId<CEMagicEssenceTypePrototype>? essenceId)
    {
        if (essenceId is not { } id || !_proto.TryIndex(id, out var essence))
            return;

        _sprite.LayerSetColor(sprite, layer, essence.Color);
    }
}
