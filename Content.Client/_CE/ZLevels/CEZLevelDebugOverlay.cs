using System.Numerics;
using Content.Shared._CE.ZLevels;
using Content.Shared._CE.ZLevels.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client._CE.ZLevels;

public sealed class CEZLevelDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    private readonly CESharedZLevelsSystem _zLevels;
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly Font _font;

    public CEZLevelDebugOverlay()
    {
        IoCManager.InjectDependencies(this);

        _zLevels = _entityManager.System<CESharedZLevelsSystem>();

        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entityManager.EntityQueryEnumerator<CEZPhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zPhys, out var xform))
        {
            var screenPos = args.ViewportControl?.WorldToScreen(xform.WorldPosition) ?? Vector2.Zero;
            var depthText = $"Z position: {zPhys.LocalPosition}\nVelocity: {zPhys.Velocity}";
            args.ScreenHandle.DrawString(_font, screenPos, depthText, Color.White);

            //And draw lines from ent to ground
            args.ScreenHandle.DrawDottedLine(screenPos, screenPos - new Vector2(0, zPhys.LocalPosition), Color.White);
            args.ScreenHandle.DrawCircle(screenPos, 0.1f, Color.White);
        }
    }
}
