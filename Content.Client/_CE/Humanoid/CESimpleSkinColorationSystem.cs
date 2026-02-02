using Content.Shared._CE.Humanoid;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

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
        Color skinColor = Color.White;

        // Try to find an organ with a visual profile and use its skin color.
        if (TryComp<BodyComponent>(ent, out var body) && body.Organs is { } organs)
        {
            foreach (var organ in organs.ContainedEntities)
            {
                if (TryComp<VisualOrganComponent>(organ, out var vOrg))
                {
                    skinColor = vOrg.Profile.SkinColor;
                    break;
                }
            }
        }

        foreach (var map in ent.Comp.Maps)
        {
            var index = _sprite.LayerMapGet((ent, sprite), map);
            _sprite.LayerSetColor((ent, sprite), index, skinColor);
        }
    }
}
