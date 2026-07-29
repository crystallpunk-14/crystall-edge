using Content.Shared._CE.MagicVision.Components;
using Content.Shared._CE.MagicVision.Events;
using Robust.Shared.Network;

namespace Content.Shared._CE.MagicVision;

public abstract partial class CESharedMagicVisionSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeClothing();
    }

    /// <summary>
    /// Recomputes whether <see cref="uid"/> currently has magic vision by asking every subscribed
    /// source via <see cref="CECheckMagicVisionEvent"/>, then adds or removes
    /// <see cref="CEMagicVisionComponent"/> to match. Sources (clothing, skills, etc.) should call
    /// this whenever their own contribution to magic vision might have changed - e.g. on equip/unequip
    /// or on skill learned/forgotten.
    /// </summary>
    public void RefreshMagicVision(EntityUid uid)
    {
        if (_net.IsClient)
            return;

        var ev = new CECheckMagicVisionEvent();
        RaiseLocalEvent(uid, ev);

        if (ev.HasVision == HasComp<CEMagicVisionComponent>(uid))
            return;

        if (ev.HasVision)
            EnsureComp<CEMagicVisionComponent>(uid);
        else
            RemComp<CEMagicVisionComponent>(uid);
    }
}
