using System.Text;
using Content.Server._CE.GameTicking.Components;
using Content.Server.Antag;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Temperature.Systems;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Vampire;
using Content.Shared._CE.Vampire.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Temperature.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._CE.Vampire;

public sealed partial class CEVampireSystem : CESharedVampireSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly CEDayCycleSystem _dayCycle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    private static readonly EntProtoId DefaultVampireRule = "CEGameRuleVampires";
    private static readonly EntProtoId VFX = "CEImpactEffectBloodEssence";

    public override void Initialize()
    {
        base.Initialize();
        InitializeAnnounces();

        SubscribeLocalEvent<CEVampireClanHeartComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<CEVampireClanHeartComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<CEVampireComponent, CETransformIntoVampireActionEvent>(OnTransformIntoVampire);

        SubscribeLocalEvent<CEActionVampireCandlesComponent, ActionAttemptEvent>(OnVampireCandlesCastAttempt);
    }

    private void OnStartCollide(Entity<CEVampireClanHeartComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<CEVampireTreeCollectableComponent>(args.OtherEntity, out var collectable))
            return;

        var collect = collectable.Essence;

        if (TryComp<StackComponent>(args.OtherEntity, out var stack))
            collect *= stack.Count;

        AddEssence(ent, collect);
        Del(args.OtherEntity);

        _audio.PlayPvs(collectable.CollectSound, ent);
    }

    private void OnExamined(Entity<CEVampireClanHeartComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.VampireOwner is null)
            return;
        if (!HasComp<CEVampireComponent>(args.Examiner) && !HasComp<GhostComponent>(args.Examiner))
            return;

        var sb = new StringBuilder();

        // Are they friend or foe?
        if (TryComp<CEVampireComponent>(args.Examiner, out var examinerVampire))
        {
            if (args.Examiner == ent.Comp.VampireOwner)
                sb.Append(Loc.GetString("ce-vampire-tree-examine-friend") + "\n");
            else
                sb.Append(Loc.GetString("ce-vampire-tree-examine-enemy") + "\n");
        }

        //Progress
        sb.Append(Loc.GetString("ce-vampire-tree-examine-level",
            ("level", ent.Comp.Level),
            ("essence", ent.Comp.EssenceFromLevelStart),
            ("left", ent.Comp.EssenceToNextLevel?.ToString() ?? "∞")) + "\n"+ "\n");

        var query = EntityQueryEnumerator<CEVampireClanHeartComponent>();

        sb.Append(Loc.GetString("ce-vampire-tree-other-title") + "\n");
        while (query.MoveNext(out var uid, out var heart))
        {
            if (uid == ent.Owner)
                continue;

            sb.Append(Loc.GetString("ce-vampire-tree-other-info",
                ("essence", heart.EssenceFromLevelStart),
                ("left", heart.EssenceToNextLevel?.ToString() ?? "∞"),
                ("lvl", heart.Level)) + "\n");
        }

        args.PushMarkup(sb.ToString());
    }

    private void OnTransformIntoVampire(Entity<CEVampireComponent> ent, ref CETransformIntoVampireActionEvent args)
    {
        if (HasComp<CEVampireComponent>(args.Target)) //Target already a vampire
            return;

        if (!TryComp<ActorComponent>(args.Target, out var actor))
            return;

        args.Handled = true;

        _antag.ForceMakeAntag<CEVampireRuleComponent>(actor.PlayerSession, DefaultVampireRule);

        var vamp = EnsureComp<CEVampireComponent>(args.Target);
        vamp.HigherVampire = false;
        Dirty(args.Target, vamp);
        EnsureComp<CEVampireVisualsComponent>(args.Target); //Auto reveal vampire form for fun
        Spawn(VFX, Transform(args.Target).Coordinates);
    }

    private void OnVampireCandlesCastAttempt(Entity<CEActionVampireCandlesComponent> ent, ref ActionAttemptEvent args)
    {
        //Flammable code server-only so we cant predict this, and put in server
        if (args.Cancelled)
            return;

        var ignited = 0;
        foreach (var candle in _lookup.GetEntitiesInRange<CEVampireCandleComponent>(Transform(args.User).Coordinates, ent.Comp.Range))
        {
            if (!TryComp<FlammableComponent>(candle, out var flammable))
                continue;

            if (!flammable.OnFire)
                continue;

            ignited++;

            if (ignited >= ent.Comp.CandleCount)
                break;
        }

        if (ignited < ent.Comp.CandleCount)
        {
            _popup.PopupEntity(
                Loc.GetString("ce-magic-spell-need-ignited-vamp-candles", ("count", ent.Comp.CandleCount)),
                args.User,
                args.User);
            args.Cancelled = true;
        }
    }

    private void AddEssence(Entity<CEVampireClanHeartComponent> ent, FixedPoint2 amount)
    {
        if (ent.Comp.VampireOwner is null)
            return;

        var level = ent.Comp.Level;
        ent.Comp.CollectedEssence += amount;
        Dirty(ent);

        if (level < ent.Comp.Level) //Level up!
        {
            _appearance.SetData(ent, VampireClanLevelVisuals.Level, ent.Comp.Level);
            AnnounceToEnemyVampires(ent.Comp.VampireOwner.Value, Loc.GetString("ce-vampire-tree-growing", ("level", ent.Comp.Level)));
            AnnounceToVampire(ent.Comp.VampireOwner.Value, Loc.GetString("ce-vampire-tree-growing-self", ("level", ent.Comp.Level)));

            SpawnAtPosition(ent.Comp.LevelUpVfx, Transform(ent).Coordinates);
        }
    }

    protected override void OnVampireInit(Entity<CEVampireComponent> ent, ref MapInitEvent args)
    {
        base.OnVampireInit(ent, ref args);

        //Metabolism
        foreach (var (organUid, _) in _body.GetBodyOrgans(ent))
        {
            if (TryComp<MetabolizerComponent>(organUid, out var metabolizer) && metabolizer.MetabolizerTypes is not null)
            {
                metabolizer.MetabolizerTypes.Clear();
                metabolizer.MetabolizerTypes.Add(ent.Comp.MetabolizerType);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //Vampire under sun heating
        var query = EntityQueryEnumerator<CEVampireComponent, CEVampireVisualsComponent, TemperatureComponent, FlammableComponent>();
        while (query.MoveNext(out var uid, out var vampire, out var visuals, out var temperature, out var flammable))
        {
            if (_timing.CurTime < vampire.NextHeatTime)
                continue;

            vampire.NextHeatTime = _timing.CurTime + vampire.HeatFrequency;

            if (!_dayCycle.UnderSunlight(uid))
                continue;

            _temperature.ChangeHeat(uid, vampire.HeatUnderSunTemperature);
            _popup.PopupEntity(Loc.GetString("ce-heat-under-sun"), uid, uid, PopupType.SmallCaution);

            if (temperature.CurrentTemperature > vampire.IgniteThreshold && !flammable.OnFire)
            {
                _flammable.AdjustFireStacks(uid, 1, flammable);
                _flammable.Ignite(uid, uid, flammable);
            }
        }

        var heartQuery = EntityQueryEnumerator<CEVampireClanHeartComponent>();
        //regen essence over time
        while (heartQuery.MoveNext(out var uid, out var heart))
        {
            if (_timing.CurTime < heart.NextRegenTime)
                continue;

            heart.NextRegenTime = _timing.CurTime + heart.RegenFrequency;

            AddEssence((uid, heart), heart.EssenceRegen);
        }
    }
}
