using System.Text;
using Content.Shared._CE.MagicTrace.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.Random;

namespace Content.Server._CE.MagicTrace;

/// <summary>
/// Rolls a random <see cref="CEAuraImprintComponent.Imprint"/> for each creature.
/// </summary>
public sealed partial class CEAuraImprintSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    private static readonly char[] ImprintLetters =
        ['ä', 'ã', 'ç', 'ø', 'ђ', 'œ', 'Ї', 'Ћ', 'ў', 'ž', 'ö', 'є', 'þ'];

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEAuraImprintComponent> ent, ref MapInitEvent args)
    {
        var sb = new StringBuilder(ent.Comp.ImprintLength);
        for (var i = 0; i < ent.Comp.ImprintLength; i++)
        {
            sb.Append(_random.Pick(ImprintLetters));
        }

        ent.Comp.Imprint = sb.ToString();
        Dirty(ent);
    }
}
