using Content.Shared._White.AuraImrint;
using Content.Shared._White.MagicVision;
using Content.Shared._White.MagicVision.Components;
using Content.Shared.Mobs;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._White.AuraImprint;

/// <summary>
/// This system handles the basic mechanics of spell use, such as doAfter, event invocation, and energy spending.
/// </summary>
public sealed partial class WhiteAuraImprintSystem : WhiteSharedAuraImprintSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly WhiteSharedMagicVisionSystem _vision = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhiteAuraImprintComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WhiteHideMagicAuraStatusEffectComponent, StatusEffectAppliedEvent>(OnShuffleStatusApplied);
        SubscribeLocalEvent<WhiteAuraImprintComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnShuffleStatusApplied(Entity<WhiteHideMagicAuraStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Imprint = GenerateAuraImprint(args.Target);
        Dirty(ent);
    }

    private void OnMapInit(Entity<WhiteAuraImprintComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Imprint = GenerateAuraImprint((ent.Owner, ent.Comp));
        Dirty(ent);
    }

    public string GenerateAuraImprint(Entity<WhiteAuraImprintComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return string.Empty;

        var letters = new[] { "ä", "ã", "ç", "ø", "ђ", "œ", "Ї", "Ћ", "ў", "ž", "Ћ", "ö", "є", "þ"};
        var imprint = string.Empty;

        for (var i = 0; i < ent.Comp.ImprintLength; i++)
        {
            imprint += letters[_random.Next(letters.Length)];
        }

        return $"[color={ent.Comp.ImprintColor.ToHex()}]{imprint}[/color]";
    }

    private void OnMobStateChanged(Entity<WhiteAuraImprintComponent> ent, ref MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Critical:
            {
                _vision.SpawnMagicTrace(
                    Transform(ent).Coordinates,
                    new SpriteSpecifier.Rsi(new ResPath("_White/Actions/Spells/misc.rsi"), "skull"),
                    Loc.GetString("white-magic-vision-crit"),
                    TimeSpan.FromMinutes(10),
                    ent);
                break;
            }
            case MobState.Dead:
            {
                _vision.SpawnMagicTrace(
                    Transform(ent).Coordinates,
                    new SpriteSpecifier.Rsi(new ResPath("_White/Actions/Spells/misc.rsi"), "skull_red"),
                    Loc.GetString("white-magic-vision-dead"),
                    TimeSpan.FromMinutes(10),
                    ent);
                break;
            }
        }
    }
}
