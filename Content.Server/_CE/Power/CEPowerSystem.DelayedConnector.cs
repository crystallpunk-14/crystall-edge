using Content.Server._CE.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.NodeContainer;
using Content.Shared.Verbs;

namespace Content.Server._CE.Power;

public sealed partial class CEPowerSystem
{
    private void InitializeDelayedConnector()
    {
        SubscribeLocalEvent<CEDelayedConnectorComponent, PowerConsumerReceivedChanged>(OnDelayedPowerChanged);
        SubscribeLocalEvent<CEDelayedConnectorComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<CEDelayedConnectorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnGetVerb(Entity<CEDelayedConnectorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (ent.Comp.AvailableDelays.Count == 0)
            return;

        foreach (var option in ent.Comp.AvailableDelays)
        {
            var v = new Verb
            {
                Priority = (int)option.TotalSeconds,
                Category = VerbCategory.Lever,
                Text = Loc.GetString("ce-power-delayed-connector-verb", ("count", option.TotalSeconds)),
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    ent.Comp.SelectedDelay = option;
                    _popup.PopupEntity(Loc.GetString("ce-power-delayed-connector-delay-set",
                            ("count", option.TotalSeconds)),
                        ent.Owner);
                },
            };

            args.Verbs.Add(v);
        }
    }

    private void OnExamined(Entity<CEDelayedConnectorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("ce-power-delayed-connector-examined", ("count", ent.Comp.SelectedDelay.TotalSeconds)));
    }

    private void OnDelayedPowerChanged(Entity<CEDelayedConnectorComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        var powered = args.ReceivedPower >= args.DrawRate;

        if (powered == ent.Comp.Active)
            return;

        ent.Comp.NextChangeTime = _timing.CurTime + ent.Comp.SelectedDelay;
    }

    private void UpdateDelayedConnectors(float frameTime)
    {
        var query = EntityQueryEnumerator<CEDelayedConnectorComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var connector, out var nodeContainer))
        {
            if (connector.NextChangeTime == TimeSpan.Zero)
                continue;

            if (_timing.CurTime < connector.NextChangeTime)
                continue;

            var newState = !connector.Active;
            connector.Active = newState;
            ToggleConnector((uid, nodeContainer), newState);
            connector.NextChangeTime = TimeSpan.Zero;
        }
    }
}
