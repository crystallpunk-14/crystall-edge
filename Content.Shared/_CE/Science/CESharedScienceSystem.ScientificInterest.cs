using System.Linq;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.Science.Components;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedScienceSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private CEMagicEssenceSystem _essence = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly SoundSpecifier KnowledgeLearnedSound =
        new SoundPathSpecifier("/Audio/_CE/Effects/knowledge_learned.ogg");

    private void InitializeScientificInterest()
    {
        SubscribeLocalEvent<CEThaumaturgicMagnifyingGlassComponent, AfterInteractEvent>(OnMagnifyingGlassInteract);
        SubscribeLocalEvent<CEScientificInterestComponent, CEScientificInterestDoAfterEvent>(
            OnScientificInterestDoAfter);
        SubscribeLocalEvent<CEScienceRandomPointsComponent, MapInitEvent>(OnRandomPointsMapInit);
    }

    private void OnRandomPointsMapInit(Entity<CEScienceRandomPointsComponent> ent, ref MapInitEvent args)
    {
        if (!_net.IsServer)
            return;

        var interest = EnsureComp<CEScientificInterestComponent>(ent.Owner);
        interest.Points.Clear();

        var essenceA = _essence.GetRandomEssenceType();
        var essenceB = _essence.GetRandomEssenceType();
        var essenceC = _essence.GetRandomEssenceType();

        DistributeInterestPoints(_random, interest.Points, ent.Comp.Amount, essenceA, essenceB, essenceC);

        Dirty(ent.Owner, interest);
    }

    /// <summary>
    /// Rolls a random total from <paramref name="amount"/> and distributes it across three essence
    /// types 70%/20%/10%, adding the result into <paramref name="points"/>. Static so any domain
    /// that needs this same weighting (e.g. magic essence nodes rolling their own scientific
    /// interest) can reuse it without a system reference.
    /// </summary>
    public static void DistributeInterestPoints(
        IRobustRandom random,
        Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> points,
        MinMax amount,
        ProtoId<CEMagicEssenceTypePrototype> essenceA,
        ProtoId<CEMagicEssenceTypePrototype> essenceB,
        ProtoId<CEMagicEssenceTypePrototype> essenceC)
    {
        var total = amount.Next(random);
        for (var i = 0; i < total; i++)
        {
            var roll = random.NextFloat();
            var essence = roll < 0.7f ? essenceA : roll < 0.9f ? essenceB : essenceC;
            points[essence] = points.GetValueOrDefault(essence) + 1;
        }
    }

    private void OnMagnifyingGlassInteract(Entity<CEThaumaturgicMagnifyingGlassComponent> ent,
        ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<CEScientificInterestComponent>(target, out var interest) || interest.StudiedBy.Contains(args.User))
            return;

        var duration = interest.Time * ent.Comp.StudyTimeMultiplier;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            duration,
            new CEScientificInterestDoAfterEvent(),
            ent,
            target: args.User,
            used: args.Used)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = _hands.IsHolding(args.User, args.Used),
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnScientificInterestDoAfter(Entity<CEScientificInterestComponent> ent,
        ref CEScientificInterestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!ent.Comp.StudiedBy.Add(target))
            return;

        Dirty(ent);

        var data = EnsureComp<CEScienceResearchDataComponent>(target);
        GrantPoints((target, data), ent.Comp.Points);

        _audio.PlayLocal(KnowledgeLearnedSound, target, target);

        var pointsText = string.Join(", ",
            ent.Comp.Points.Select(kv =>
            {
                var essenceName = _proto.TryIndex(kv.Key, out var essence) ? essence.Name : kv.Key.Id;
                return $"{kv.Value} {essenceName}";
            }));

        _popup.PopupPredicted(
            Loc.GetString("ce-scientific-interest-popup-success", ("target", ent.Owner), ("points", pointsText)),
            null,
            ent,
            target);
    }
}

[Serializable, NetSerializable]
public sealed partial class CEScientificInterestDoAfterEvent : SimpleDoAfterEvent;
