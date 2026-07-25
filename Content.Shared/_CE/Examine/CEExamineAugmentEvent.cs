namespace Content.Shared._CE.Examine;

/// <summary>
/// Broadcast once per <see cref="Content.Shared.Examine.ExaminedEvent"/>, letting any number of
/// unrelated CE features append their own markup line to the examine text without colliding on
/// ExaminedEvent's one-subscriber-per-component limit.
/// </summary>
/// <remarks>
/// Subscribe with a broadcast <c>SubscribeLocalEvent&lt;CEExamineAugmentEvent&gt;(Handler)</c> (no
/// component filter) — broadcast subscriptions aren't restricted to one per event type, unlike
/// directed component subscriptions. Do your own component/condition checks on
/// <see cref="Examined"/>/<see cref="Examiner"/> inside the handler, then call
/// <see cref="AddMarkup"/> if you have something to show.
/// </remarks>
public sealed class CEExamineAugmentEvent : EntityEventArgs
{
    public readonly EntityUid Examined;
    public readonly EntityUid Examiner;

    private readonly List<string> _markup = new();
    public IReadOnlyList<string> Markup => _markup;

    public CEExamineAugmentEvent(EntityUid examined, EntityUid examiner)
    {
        Examined = examined;
        Examiner = examiner;
    }

    public void AddMarkup(string markup)
    {
        _markup.Add(markup);
    }
}
