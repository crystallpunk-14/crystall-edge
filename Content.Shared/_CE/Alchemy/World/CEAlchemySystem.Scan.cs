using System.Globalization;
using Content.Shared._CE.Alchemy.Data;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Alchemy.World;

/// <summary>
/// Raised by the solution scan (alchemy glasses, the Solution examine verb) once the chemical list is written,
/// so that CE can append its own lines to the same examine window.
/// </summary>
[ByRefEvent]
public record struct CESolutionScanEvent(Solution Solution, FormattedMessage Message);

public sealed partial class CEAlchemySystem
{
    private const string MixtureStage = "Bloodstream";
    private const float DefaultMetabolismRate = 0.5f;

    [Dependency] private IEntitySystemManager _entSys = default!;

    /// <summary>
    /// How long a dose of this mixture works: it is metabolized at a fixed rate multiplied by the tuning.
    /// </summary>
    public TimeSpan GetDuration(CEAlchemyData data, FixedPoint2 dose)
    {
        var rate = DefaultMetabolismRate;
        if (_proto.TryIndex<ReagentPrototype>(MixtureReagent, out var mixture)
            && mixture.Metabolisms is { } metabolisms
            && metabolisms.Metabolisms.TryGetValue(MixtureStage, out var entry))
        {
            rate = entry.MetabolismRate.Float();
        }

        rate *= data.Multiplier;
        return rate <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(dose.Float() / rate);
    }

    public string FormatDuration(TimeSpan duration)
    {
        var seconds = duration.TotalSeconds;
        return seconds >= 60
            ? Loc.GetString("ce-guidebook-alchemy-duration-minutes", ("value", Math.Round(seconds / 60, 1)))
            : Loc.GetString("ce-guidebook-alchemy-duration-seconds", ("value", (int) Math.Round(seconds)));
    }

    /// <summary>
    /// Player-facing lines of what a hit zone does to whoever takes the mixture. Effects without a description are skipped.
    /// </summary>
    public List<string> DescribeHit(CEAlchemyHit hit, TimeSpan duration)
    {
        var lines = new List<string>();
        var placeholder = Loc.GetString("ce-entity-effect-guidebook-none");
        var durationText = FormatDuration(duration);

        foreach (var effect in hit.Zone.Effects)
        {
            var text = effect.EntityEffectGuidebookText(_proto, _entSys, hit.Strength);
            if (string.IsNullOrWhiteSpace(text) || text == placeholder)
                continue;

            // A status effect is refreshed every second and has its own duration, the rest works every second.
            text = text.TrimEnd('.', ' ');
            lines.Add(Loc.GetString(effect is ApplyStatusEffect
                    ? "ce-guidebook-alchemy-effect-line-status"
                    : "ce-guidebook-alchemy-effect-line",
                ("effect", text),
                ("duration", durationText)));
        }

        return lines;
    }

    [SubscribeLocalEvent]
    private void OnSolutionScan(ref CESolutionScanEvent args)
    {
        // Without the round world the zones would be read at their base positions, which is a false hint.
        if (!TryGetWorld(out _))
            return;

        foreach (var quantity in args.Solution.Contents)
        {
            if (quantity.Reagent.Data is not { } dataList)
                continue;

            foreach (var data in dataList)
            {
                if (data is not CEAlchemyData alchemy)
                    continue;

                AppendScan(args.Message, alchemy, quantity.Quantity);
            }
        }
    }

    private void AppendScan(FormattedMessage message, CEAlchemyData data, FixedPoint2 quantity)
    {
        var duration = GetDuration(data, quantity);
        message.PushNewline();
        message.AddMarkupOrThrow(Loc.GetString("ce-alchemy-scan-header", ("amount", quantity)));

        var sign = data.Tune > FixedPoint2.Zero ? "+" : string.Empty;
        message.PushNewline();
        message.AddMarkupOrThrow(Loc.GetString("ce-alchemy-scan-tune",
            ("tune", sign + data.Tune.Float().ToString("0.00", CultureInfo.InvariantCulture)),
            ("multiplier", data.Multiplier.ToString("0.00", CultureInfo.InvariantCulture))));

        var any = false;
        foreach (var hit in Analyze(data))
        {
            any = true;
            message.PushNewline();
            message.AddMarkupOrThrow(Loc.GetString("ce-alchemy-scan-zone",
                ("zone", Loc.GetString(hit.Zone.Name)),
                ("effects", string.Join(" ", DescribeHit(hit, duration)))));
        }

        if (any)
            return;

        message.PushNewline();
        message.AddMarkupOrThrow(Loc.GetString("ce-alchemy-scan-none"));
    }
}
