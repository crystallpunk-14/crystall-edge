using Content.Server._CE.MagicEssence.Components;
using Content.Server.Audio;
using Content.Shared.Foldable;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server._CE.MagicEssence.Systems;

public sealed partial class CEMagicEssenceAttractorSystem
{
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private FoldableSystem _foldable = default!;
    [Dependency] private IGameTiming _timing = default!;

    private void InitializePortable()
    {
        SubscribeLocalEvent<CEPortableMagicEssenceAttractorComponent, FoldedEvent>(OnPortableFolded);
        SubscribeLocalEvent<CEPortableMagicEssenceAttractorComponent, BatteryStateChangedEvent>(OnPortableBatteryChanged);
    }

    private void OnPortableFolded(Entity<CEPortableMagicEssenceAttractorComponent> ent, ref FoldedEvent args)
    {
        RefreshPortableAttracting(ent, args.IsFolded);
    }

    private void OnPortableBatteryChanged(Entity<CEPortableMagicEssenceAttractorComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.NewState != BatteryState.Empty)
            return;

        SetPortableAttracting(ent, false);
    }

    private void RefreshPortableAttracting(Entity<CEPortableMagicEssenceAttractorComponent> ent, bool folded)
    {
        var hasCharge = TryComp<BatteryComponent>(ent, out var battery) && battery.LastCharge > 0f;
        SetPortableAttracting(ent, !folded && hasCharge);
    }

    private void SetPortableAttracting(EntityUid uid, bool attracting)
    {
        if (attracting)
            EnsureComp<CEMagicEssenceAttractingComponent>(uid);
        else
            RemCompDeferred<CEMagicEssenceAttractingComponent>(uid);

        _ambient.SetAmbience(uid, attracting);
        _appearance.SetData(uid, PowerDeviceVisuals.Powered, attracting);
    }

    private void UpdatePortable()
    {
        var query = EntityQueryEnumerator<CEPortableMagicEssenceAttractorComponent, FoldableComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var portable, out var foldable, out var battery))
        {
            if (_timing.CurTime < portable.NextConsumeTime)
                continue;

            portable.NextConsumeTime = _timing.CurTime + portable.EnergyConsumeFrequency;

            if (_foldable.IsFolded(uid, foldable) || battery.LastCharge <= 0f)
            {
                SetPortableAttracting(uid, false);
                continue;
            }

            SetPortableAttracting(uid, true);
            _battery.UseCharge((uid, battery), portable.EnergyDraw);
        }
    }
}
