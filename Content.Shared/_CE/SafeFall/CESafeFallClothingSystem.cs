using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._CE.SafeFall;

public sealed class CESafeFallClothingSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

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
        args.Cancel();
        _statusEffect.TrySetStatusEffectDuration(args.Falled, ent.Comp.StatusEffect, out var effect , ent.Comp.Duration);
    }
}
