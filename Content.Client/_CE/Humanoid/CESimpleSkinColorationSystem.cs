using Content.Shared._CE.Humanoid;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Humanoid;

public sealed class CESimpleSkinColorationSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    //CrystallEdge: any non-eye organ carries the body's general skin tone; Torso is always present on a humanoid
    private readonly ProtoId<OrganCategoryPrototype> _skinToneOrganCategory = "Torso";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESkinColoredLayersComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<CESkinColoredLayersComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_visualBody.TryGatherMarkingsData((ent.Owner, null), null, out var profiles, out _, out _) ||
            !profiles.TryGetValue(_skinToneOrganCategory, out var bodyProfile))
            return;

        foreach (var map in ent.Comp.Maps)
        {
            var index = _sprite.LayerMapGet((ent, sprite), map);
            _sprite.LayerSetColor((ent, sprite), index, bodyProfile.SkinColor);
        }
    }
}
