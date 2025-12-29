using Content.Shared._CE.Humanoid;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client._CE.Humanoid;

public sealed class CESimpleSkinColorationSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESkinColoredLayersComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<CESkinColoredLayersComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        foreach (var map in ent.Comp.Maps)
        {
            var index = _sprite.LayerMapGet((ent, sprite), map);
            _sprite.LayerSetColor((ent, sprite), index, humanoid.SkinColor);
        }
    }
}
