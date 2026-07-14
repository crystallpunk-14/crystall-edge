using Content.Shared._CE.Humanoid;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Humanoid;

public sealed partial class CESimpleSkinColorationSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    //CrystallEdge: any non-eye organ carries the body's general skin tone; Torso is always present on a humanoid
    private readonly ProtoId<OrganCategoryPrototype> _skinToneOrganCategory = "Torso";

    public override void Initialize()
    {
        base.Initialize();

        //CrystallEdge: hook the organ's own startup, not the body's - Robust applies an entity's networked state
        //(Profile.SkinColor, OrganComponent.Body) before running its ComponentStartup, even for entities received
        //over the network, so the data is already valid here. The body's own lifecycle events fire too early instead,
        //before its organs exist.
        SubscribeLocalEvent<VisualOrganComponent, ComponentStartup>(OnOrganStartup);
    }

    private void OnOrganStartup(Entity<VisualOrganComponent> ent, ref ComponentStartup args)
    {
        var organ = Comp<OrganComponent>(ent);
        if (organ.Category != _skinToneOrganCategory || organ.Body is not { } body)
            return;

        if (!TryComp<CESkinColoredLayersComponent>(body, out var layers) || !TryComp<SpriteComponent>(body, out var sprite))
            return;

        foreach (var map in layers.Maps)
        {
            if (!_sprite.LayerMapTryGet((body, sprite), map, out var index, true))
                continue;

            _sprite.LayerSetColor((body, sprite), index, ent.Comp.Profile.SkinColor);
        }
    }
}
