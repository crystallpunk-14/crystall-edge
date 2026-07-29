using Content.Shared._CE.MagicVision;
using Content.Shared._CE.MagicVision.Components;
using Content.Shared.Eye;
using Robust.Shared.GameObjects;

namespace Content.Server._CE.MagicVision;

public sealed partial class CEMagicVisionSystem : CESharedMagicVisionSystem
{
    [Dependency] private SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMagicVisionComponent, GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<CEMagicVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CEMagicVisionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnGetVisMask(Entity<CEMagicVisionComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.CEMagicVision;
    }

    private void OnStartup(Entity<CEMagicVisionComponent> ent, ref ComponentStartup args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnShutdown(Entity<CEMagicVisionComponent> ent, ref ComponentShutdown args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }
}
