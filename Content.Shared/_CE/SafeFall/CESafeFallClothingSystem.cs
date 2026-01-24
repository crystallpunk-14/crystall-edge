using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;

namespace Content.Shared._CE.SafeFall;

public sealed class CESafeFallClothingSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESafeFallClothingComponent, CEZLevelChasmAttempt>(OnZLevelFall);
        SubscribeLocalEvent<CESafeFallClothingComponent, InventoryRelayedEvent<CEZLevelChasmAttempt>>(OnZLevelRelayedFall);
    }

    private void OnZLevelRelayedFall(Entity<CESafeFallClothingComponent> ent, ref InventoryRelayedEvent<CEZLevelChasmAttempt> args)
    {
        OnZLevelFall(ent, ref args.Args);
    }

    private void OnZLevelFall(Entity<CESafeFallClothingComponent> ent, ref CEZLevelChasmAttempt args)
    {
        if (args.Cancelled)
            return;

        args.Cancel();

        if (_statusEffect.TrySetStatusEffectDuration(args.Falled, ent.Comp.StatusEffect, out _, ent.Comp.Duration))
        {
            PredictedQueueDel(ent);
            _popup.PopupPredictedCoordinates(Loc.GetString("ce-zlevels-safe-fall-amulet-crack", ("name", MetaData(ent).EntityName)), Transform(args.Falled).Coordinates, args.Falled, PopupType.MediumCaution);
            _stun.TryKnockdown(args.Falled, ent.Comp.StunDuration);
        }
    }
}
