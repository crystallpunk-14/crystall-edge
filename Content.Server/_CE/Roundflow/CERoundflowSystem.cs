using Content.Shared._CE.Roundflow;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server._CE.RoundFlow;

public sealed class CERoundflowSystem : EntitySystem
{
    public override void Initialize()
    {
        //SubscribeLocalEvent<MetaDataComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<MetaDataComponent> ent, ref MobStateChangedEvent args)
    {
        SendPopup(ent.Owner);
    }

    public void SendPopup(Entity<ActorComponent?> target)
    {
        if (!Resolve(target, ref target.Comp, false))
            return;

        RaiseNetworkEvent(new CEScreenPopupShowEvent("Вау", "охуеть", new SoundPathSpecifier("/Audio/Animals/bear.ogg")));
    }
}
