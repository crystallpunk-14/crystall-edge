using Content.Shared.Examine;

namespace Content.Shared._CE.Examine;

/// <summary>
/// The single, canonical subscriber of (MetaDataComponent, ExaminedEvent) for CE features that
/// need to conditionally add text to ANY entity's examine tooltip (essence composition, price,
/// etc.). Fans the examine out to <see cref="CEExamineAugmentEvent"/> so any number of unrelated
/// systems can contribute without fighting over the engine's one-subscriber-per-component-per-
/// event restriction — add your feature by subscribing to that event instead of to ExaminedEvent
/// directly.
/// </summary>
public sealed partial class CEExamineAugmentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaDataComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<MetaDataComponent> ent, ref ExaminedEvent args)
    {
        var ev = new CEExamineAugmentEvent(args.Examined, args.Examiner);
        RaiseLocalEvent(ent.Owner, ev, broadcast: true);

        foreach (var markup in ev.Markup)
        {
            args.PushMarkup(markup);
        }
    }
}
