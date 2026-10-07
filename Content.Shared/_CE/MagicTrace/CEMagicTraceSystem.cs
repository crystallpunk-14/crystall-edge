using System.Text;
using Content.Shared._CE.MagicTrace.Components;
using Content.Shared._CE.MagicVision.Components;
using Content.Shared._CE.TimedDespawn;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Robust.Shared.Analyzers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._CE.MagicTrace;

/// <summary>
/// Spawns <see cref="CEMagicTraceComponent"/> traces and handles examining them and creatures' auras.
/// </summary>
public sealed partial class CEMagicTraceSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MetaDataSystem _meta = default!;

    private static readonly EntProtoId TraceProto = "CEMagicTrace";

    /// <summary>
    /// How long a trace of a mana-costing action lasts, per point of its mana cost.
    /// </summary>
    private static readonly TimeSpan SpellTraceDurationPerMana = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan MobStateTraceDuration = TimeSpan.FromMinutes(10);

    private static readonly SpriteSpecifier CritIcon =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Actions/magic_trace.rsi"), "skull");

    private static readonly SpriteSpecifier DeadIcon =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Actions/magic_trace.rsi"), "skull_red");

    private const char ObscuredChar = '~';

    /// <summary>
    /// Spawns an invisible magic trace at <paramref name="position"/>, visible only with magic vision.
    /// Does nothing on the client.
    /// </summary>
    /// <param name="description">Already localized text shown when the trace is examined.</param>
    /// <param name="author">Creature whose aura imprint is left on the trace, if it has one.</param>
    public void SpawnMagicTrace(EntityCoordinates position,
        SpriteSpecifier? icon,
        string description,
        TimeSpan duration,
        EntityUid? author = null)
    {
        if (_net.IsClient)
            return;

        var trace = SpawnAtPosition(TraceProto, position);

        var comp = EnsureComp<CEMagicTraceComponent>(trace);
        comp.Icon = icon;

        if (TryComp<CEAuraImprintComponent>(author, out var aura) && aura.Imprint != string.Empty)
        {
            comp.AuraImprint = aura.Imprint;
            comp.AuraColor = aura.ImprintColor;
        }

        Dirty(trace, comp);

        _meta.SetEntityDescription(trace, description);

        // The prototype's randomized lifetime is only a placeholder - the real one depends on what left the trace.
        // It must be overridden after spawning, since CERandomizedTimedDespawn rolls it on map init.
        var despawn = EnsureComp<CERandomizedTimedDespawnComponent>(trace);
        despawn.SpawnTime = _timing.CurTime;
        despawn.Lifetime = duration;
        Dirty(trace, despawn);

        EnsureComp<TimedDespawnComponent>(trace).Lifetime = (float)duration.TotalSeconds;
    }

    /// <summary>
    /// Leaves a trace of a mana-costing action, lasting longer the more mana it costs.
    /// </summary>
    public void SpawnSpellTrace(EntityUid performer, EntityUid action, int manaCost)
    {
        if (_net.IsClient)
            return;

        SpriteSpecifier? icon = null;
        if (MetaData(action).EntityPrototype is { } actionProto)
            icon = new SpriteSpecifier.EntityPrototype(actionProto.ID);

        SpawnMagicTrace(Transform(performer).Coordinates,
            icon,
            Loc.GetString("ce-magic-trace-used-spell", ("name", MetaData(action).EntityName)),
            SpellTraceDurationPerMana * manaCost,
            performer);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CEAuraImprintComponent> ent, ref MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Critical:
                SpawnMagicTrace(Transform(ent).Coordinates,
                    CritIcon,
                    Loc.GetString("ce-magic-trace-crit"),
                    MobStateTraceDuration,
                    ent);
                break;
            case MobState.Dead:
                SpawnMagicTrace(Transform(ent).Coordinates,
                    DeadIcon,
                    Loc.GetString("ce-magic-trace-dead"),
                    MobStateTraceDuration,
                    ent);
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnAuraExamined(Entity<CEAuraImprintComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Imprint == string.Empty || !HasComp<CEMagicVisionComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("ce-magic-trace-aura",
            ("color", ent.Comp.ImprintColor.ToHex()),
            ("imprint", FormattedMessage.EscapeText(ent.Comp.Imprint))));
    }

    [SubscribeLocalEvent]
    private void OnTraceExamined(Entity<CEMagicTraceComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<CERandomizedTimedDespawnComponent>(ent, out var despawn))
            return;

        var passed = _timing.CurTime - despawn.SpawnTime;
        if (passed < TimeSpan.Zero)
            passed = TimeSpan.Zero;

        args.PushMarkup(Loc.GetString("ce-magic-trace-time-passed",
            ("time", $"{(int)passed.TotalMinutes}:{passed.Seconds:D2}")));

        if (string.IsNullOrEmpty(ent.Comp.AuraImprint))
            return;

        var progress = despawn.Lifetime > TimeSpan.Zero
            ? Math.Clamp(passed / despawn.Lifetime, 0.0, 1.0)
            : 1.0;

        var seed = GetNetEntity(ent).Id;
        var imprint = ObscureImprint(ent.Comp.AuraImprint, progress, seed);

        args.PushMarkup(Loc.GetString("ce-magic-trace-aura",
            ("color", ent.Comp.AuraColor.ToHex()),
            ("imprint", FormattedMessage.EscapeText(imprint))));
    }

    /// <summary>
    /// Replaces a <paramref name="progress"/> share of the imprint's characters with <see cref="ObscuredChar"/>.
    /// Which characters get obscured is deterministic per <paramref name="seed"/>, so a trace erodes
    /// gradually instead of reshuffling on every examine, and looks the same to every client.
    /// </summary>
    private static string ObscureImprint(string imprint, double progress, int seed)
    {
        var toObscure = (int)Math.Round(imprint.Length * progress);
        if (toObscure <= 0)
            return imprint;

        var order = new int[imprint.Length];
        var keys = new uint[imprint.Length];
        for (var i = 0; i < imprint.Length; i++)
        {
            order[i] = i;
            keys[i] = Hash(unchecked((uint)seed * 2654435761u + (uint)i));
        }

        Array.Sort(keys, order);

        var sb = new StringBuilder(imprint);
        for (var i = 0; i < toObscure && i < order.Length; i++)
        {
            sb[order[i]] = ObscuredChar;
        }

        return sb.ToString();
    }

    private static uint Hash(uint x)
    {
        x ^= x >> 16;
        x *= 0x7feb352d;
        x ^= x >> 15;
        x *= 0x846ca68b;
        x ^= x >> 16;
        return x;
    }
}
