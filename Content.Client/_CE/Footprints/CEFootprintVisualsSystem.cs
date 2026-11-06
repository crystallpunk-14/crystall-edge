using Content.Client._CE.TimedDespawnFadeout;
using Content.Shared._CE.Footprints;
using Robust.Client.GameObjects;

namespace Content.Client._CE.Footprints;

/// <summary>
/// Applies the networked sprite, color, offset and rotation of a footprint to its sprite.
/// </summary>
public sealed partial class CEFootprintVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private CETimedDespawnFadeoutSystem _fadeout = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CEFootprintComponent> ent, ref ComponentStartup args)
    {
        UpdateSprite(ent);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<CEFootprintComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    private void UpdateSprite(Entity<CEFootprintComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        Entity<SpriteComponent?> spriteEnt = (ent.Owner, sprite);

        if (ent.Comp.Sprite is { } specifier && _sprite.LayerExists(spriteEnt, 0))
            _sprite.LayerSetSprite(spriteEnt, 0, specifier);

        _sprite.SetColor(spriteEnt, ent.Comp.Color);
        _fadeout.SetBaseAlpha(ent.Owner, ent.Comp.Color.A);
        _sprite.SetOffset(spriteEnt, ent.Comp.Offset);
        _sprite.SetRotation(spriteEnt, ent.Comp.Rotation);
    }
}
