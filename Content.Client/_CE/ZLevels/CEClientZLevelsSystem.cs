using System.Numerics;
using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._CE.ZLevels;

public sealed partial class CEClientZLevelsSystem : CESharedZLevelsSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public static float ZLevelOffset = 0.7f;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new CEZLevelOverlay());

        SubscribeLocalEvent<CEZLevelPhysicsComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<CEZLevelPhysicsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (sprite.NoRotation || sprite.SnapCardinals)
            return;

        sprite.NoRotation = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEZLevelPhysicsComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var zPhys, out var sprite))
        {
            _sprite.SetOffset((uid, sprite), new Vector2(0, zPhys.LocalHeight * ZLevelOffset));
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CEZLevelOverlay>();
    }
}
