using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.WildMagic.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.WildMagic;

public sealed partial class CEClientWildMagicSystem : EntitySystem
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

        SetLayerColor((ent, sprite), ent.Comp.EssenceALayer, ent.Comp.EssenceA);
        SetLayerColor((ent, sprite), ent.Comp.EssenceBLayer, ent.Comp.EssenceB);
        SetLayerColor((ent, sprite), ent.Comp.EssenceCLayer, ent.Comp.EssenceC);
    }

    private void SetLayerColor(Entity<SpriteComponent?> sprite, string layer, ProtoId<CEMagicEssenceTypePrototype> essenceId)
    {
        if (essenceId.Id is null || !_proto.TryIndex(essenceId, out var essence))
            return;

        _sprite.LayerSetColor(sprite, layer, essence.Color);
    }
}
