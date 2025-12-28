using Content.Shared._CE.NightVision;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._CE.NightVision;

public sealed class CEClientNightVisionSystem : CESharedNightVisionSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CENightVisionComponent, CEToggleNightVisionEvent>(OnToggleNightVision);
        SubscribeLocalEvent<CENightVisionComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    protected override void OnRemove(Entity<CENightVisionComponent> ent, ref ComponentRemove args)
    {
        base.OnRemove(ent, ref args);

        NightVisionOff(ent);
    }

    private void OnPlayerDetached(Entity<CENightVisionComponent> ent, ref PlayerDetachedEvent args)
    {
        NightVisionOff(ent);
    }

    private void OnToggleNightVision(Entity<CENightVisionComponent> ent, ref CEToggleNightVisionEvent args)
    {
        NightVisionToggle(ent);
    }

    private void NightVisionOn(Entity<CENightVisionComponent> ent)
    {
        if (_playerManager.LocalSession?.AttachedEntity != ent)
            return;

        var nightVisionLight = Spawn(ent.Comp.LightPrototype, Transform(ent).Coordinates);
        _transform.SetParent(nightVisionLight, ent);
        _transform.SetWorldRotation(nightVisionLight, _transform.GetWorldRotation(ent));
        ent.Comp.LocalLightEntity = nightVisionLight;
    }

    private void NightVisionOff(Entity<CENightVisionComponent> ent)
    {
        QueueDel(ent.Comp.LocalLightEntity);
        ent.Comp.LocalLightEntity = null;
    }

    private void NightVisionToggle(Entity<CENightVisionComponent> ent)
    {
        if (ent.Comp.LocalLightEntity == null)
        {
            NightVisionOn(ent);
        }
        else
        {
            NightVisionOff(ent);
        }
    }
}
