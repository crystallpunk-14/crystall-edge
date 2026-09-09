using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._CE.GOAP.Actions;
using Content.Server._CE.GOAP.Navigation;
using Content.Server._CE.GOAP.Selectors;
using Content.Shared._CE.Actions;
using Content.Shared._CE.GOAP;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CECheckedActionsTest : GameTest
{
    // Fixture prototypes are loaded per test, outside the global prototype registry.
    private readonly EntProtoId EntityAction = "CECheckedEntityAction";
    private readonly EntProtoId WorldAction = "CECheckedWorldAction";
    private readonly EntProtoId DelayedAction = "CECheckedDelayedAction";
    private readonly EntProtoId RepeatingAction = "CECheckedRepeatingAction";
    public override PoolSettings PoolSettings => new() { Connected = false };

    [TestPrototypes]
    private const string Prototypes = """
- type: Tag
  id: CECheckedTarget
- type: entity
  id: CECheckedActor
  components:
  - type: CECheckedActionProbe
  - type: Actions
  - type: DoAfter
  - type: CEGOAP
    startSleeping: true
  - type: CEGOAPTargetBackoff
    duration: 2
- type: entity
  id: CECheckedTarget
  components:
  - type: Tag
    tags: [CECheckedTarget]
- type: entity
  id: CECheckedEntityAction
  parent: BaseAction
  components:
  - type: Action
    checkCanInteract: false
    checkConsciousness: false
    useDelay: 0
  - type: TargetAction
    range: 1
    checkCanAccess: false
  - type: EntityTargetAction
    event: !type:CECheckedEntityEvent
- type: entity
  id: CECheckedWorldAction
  parent: BaseAction
  components:
  - type: Action
    checkCanInteract: false
    checkConsciousness: false
    useDelay: 0
  - type: TargetAction
    range: 1
    checkCanAccess: false
  - type: WorldTargetAction
    event: !type:CECheckedWorldEvent
- type: entity
  id: CECheckedDelayedAction
  parent: CECheckedEntityAction
  components:
  - type: DoAfterArgs
    delay: 0.2
    needHand: false
    requireCanInteract: false
- type: entity
  id: CECheckedRepeatingAction
  parent: CECheckedDelayedAction
  components:
  - type: DoAfterArgs
    repeat: true
""";

    [Test]
    public async Task RejectedEntityAndWorldTargetsDoNotReusePreviousSuccessfulEvent()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var actions = Server.System<SharedActionsSystem>();
            var actor = SEntMan.SpawnEntity("CECheckedActor", map.GridCoords);
            var near = SEntMan.SpawnEntity("CECheckedTarget", map.GridCoords.Offset(new Vector2(0.5f, 0)));
            var far = SEntMan.SpawnEntity("CECheckedTarget", map.GridCoords.Offset(new Vector2(5, 0)));
            var action = actions.AddAction(actor, EntityAction)!.Value;
            var probe = SComp<CECheckedActionProbeComponent>(actor);
            var accepted = new RequestPerformActionEvent(SEntMan.GetNetEntity(action), SEntMan.GetNetEntity(near));
            var rejected = new RequestPerformActionEvent(SEntMan.GetNetEntity(action), SEntMan.GetNetEntity(far));

            Assert.That(actions.TryPerformActionChecked(accepted, actor), Is.EqualTo(CEActionExecutionResult.Performed));
            Assert.That(actions.TryPerformActionChecked(rejected, actor), Is.EqualTo(CEActionExecutionResult.InvalidTarget));
            Assert.That(probe.Calls, Is.EqualTo(1));
            Assert.That(probe.LastTarget, Is.EqualTo(near));

            SEntMan.DeleteEntity(near);
            Assert.That(actions.TryPerformActionChecked(accepted, actor), Is.EqualTo(CEActionExecutionResult.InvalidTarget));
            Assert.That(probe.Calls, Is.EqualTo(1));

            var world = actions.AddAction(actor, WorldAction)!.Value;
            var nearbyPoint = new RequestPerformActionEvent(SEntMan.GetNetEntity(world), SEntMan.GetNetCoordinates(map.GridCoords));
            var distantPoint = new RequestPerformActionEvent(SEntMan.GetNetEntity(world), SEntMan.GetNetCoordinates(map.GridCoords.Offset(new Vector2(5, 0))));
            Assert.That(actions.TryPerformActionChecked(nearbyPoint, actor), Is.EqualTo(CEActionExecutionResult.Performed));
            Assert.That(actions.TryPerformActionChecked(distantPoint, actor), Is.EqualTo(CEActionExecutionResult.InvalidTarget));
            var stalePoint = new RequestPerformActionEvent(SEntMan.GetNetEntity(world), new NetCoordinates(NetEntity.Invalid, Vector2.Zero));
            Assert.That(actions.TryPerformActionChecked(stalePoint, actor), Is.EqualTo(CEActionExecutionResult.InvalidTarget));
            Assert.That(probe.Calls, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GoapRejectsFailedTargetButKeepsTargetWhenActorIsUnavailable()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var actions = Server.System<SharedActionsSystem>();
            var backoff = Server.System<CEGOAPTargetBackoffSystem>();
            var actor = SEntMan.SpawnEntity("CECheckedActor", map.GridCoords);
            var target = SEntMan.SpawnEntity("CECheckedTarget", map.GridCoords.Offset(new Vector2(5, 0)));
            var granted = actions.AddAction(actor, EntityAction)!.Value;
            var definition = new CEGOAPUseAction
            {
                ActionPrototype = EntityAction,
                Selector = new CEGOAPSelectorNearestEntity { Range = 10, Whitelist = new EntityWhitelist { Tags = new() { "CECheckedTarget" } } },
            };

            actions.SetEnabled(granted, false);
            var unavailable = new CEGOAPActionUpdateEvent<CEGOAPUseAction>(definition, 0.1f);
            SEntMan.EventBus.RaiseLocalEvent(actor, ref unavailable);
            Assert.That(unavailable.Status, Is.EqualTo(CEGOAPActionStatus.Failed));
            Assert.That(backoff.IsRejected(actor, target), Is.False);

            actions.SetEnabled(granted, true);
            var badTarget = new CEGOAPActionUpdateEvent<CEGOAPUseAction>(definition, 0.1f);
            SEntMan.EventBus.RaiseLocalEvent(actor, ref badTarget);
            Assert.That(badTarget.Status, Is.EqualTo(CEGOAPActionStatus.Failed));
            Assert.That(backoff.IsRejected(actor, target), Is.True);
            Assert.That(SComp<CECheckedActionProbeComponent>(actor).Calls, Is.Zero);
        });
    }

    [Test]
    public async Task SynchronousCallerCannotSkipDoAfterAndUnhandledEventIsNotSuccess()
    {
        var map = await Pair.CreateTestMap();
        EntityUid actor = default;
        await Server.WaitAssertion(() =>
        {
            var actions = Server.System<SharedActionsSystem>();
            actor = SEntMan.SpawnEntity("CECheckedActor", map.GridCoords);
            var target = SEntMan.SpawnEntity("CECheckedTarget", map.GridCoords.Offset(new Vector2(0.5f, 0)));
            var immediate = actions.AddAction(actor, EntityAction)!.Value;
            var probe = SComp<CECheckedActionProbeComponent>(actor);
            var request = new RequestPerformActionEvent(SEntMan.GetNetEntity(immediate), SEntMan.GetNetEntity(target));
            Assert.That(actions.TryPerformActionChecked(request, actor), Is.EqualTo(CEActionExecutionResult.Performed));
            probe.Accept = false;
            Assert.That(actions.TryPerformActionChecked(request, actor), Is.EqualTo(CEActionExecutionResult.Unhandled));
            Assert.That(probe.Calls, Is.EqualTo(1));
            probe.Accept = true;

            var delayed = actions.AddAction(actor, DelayedAction)!.Value;
            var delayedRequest = new RequestPerformActionEvent(SEntMan.GetNetEntity(delayed), SEntMan.GetNetEntity(target));
            Assert.That(actions.TryPerformActionChecked(delayedRequest, actor, allowDoAfter: false), Is.EqualTo(CEActionExecutionResult.Unavailable));
            Assert.That(probe.Calls, Is.EqualTo(1));
            Assert.That(actions.TryPerformActionChecked(delayedRequest, actor), Is.EqualTo(CEActionExecutionResult.Started));
            Assert.That(probe.Calls, Is.EqualTo(1));
            var pending = SComp<DoAfterComponent>(actor).DoAfters.Values.Single();
            var pendingEvent = (ActionDoAfterEvent) pending.Args.Event;
            Assert.That(pendingEvent.Predicted, Is.True);
            Assert.That(pendingEvent.ShowPopups, Is.True);
        });
        await Server.WaitRunTicks(30);
        await Server.WaitAssertion(() => Assert.That(SComp<CECheckedActionProbeComponent>(actor).Calls, Is.EqualTo(2)));
    }

    [Test]
    public async Task RepeatingDoAfterSurvivesUnhandledEventsAndRetainsRequestOptions()
    {
        var map = await Pair.CreateTestMap();
        EntityUid actor = default;
        EntityUid action = default;
        Content.Shared.DoAfter.DoAfter pending = default!;
        await Server.WaitAssertion(() =>
        {
            var actions = Server.System<SharedActionsSystem>();
            actor = SEntMan.SpawnEntity("CECheckedActor", map.GridCoords);
            var target = SEntMan.SpawnEntity("CECheckedTarget", map.GridCoords.Offset(new Vector2(0.5f, 0)));
            action = actions.AddAction(actor, RepeatingAction)!.Value;
            SComp<CECheckedActionProbeComponent>(actor).Accept = false;
            var request = new RequestPerformActionEvent(SEntMan.GetNetEntity(action), SEntMan.GetNetEntity(target));

            Assert.That(actions.TryPerformActionChecked(request, actor, predicted: false, showPopups: false),
                Is.EqualTo(CEActionExecutionResult.Started));
            pending = SComp<DoAfterComponent>(actor).DoAfters.Values.Single();
            var pendingEvent = (ActionDoAfterEvent) pending.Args.Event;
            Assert.That(pendingEvent.Predicted, Is.False);
            Assert.That(pendingEvent.ShowPopups, Is.False);
        });

        await Server.WaitRunTicks(30);
        await Server.WaitAssertion(() =>
        {
            var probe = SComp<CECheckedActionProbeComponent>(actor);
            // Completion re-enters the legacy bool wrapper. Unhandled must still accept the repeat.
            Assert.That(probe.Dispatches, Is.GreaterThanOrEqualTo(2));
            Assert.That(probe.Calls, Is.Zero);
            Assert.That(pending.Cancelled, Is.False);
            Assert.That(pending.Completed, Is.False);
            Assert.That(SComp<DoAfterComponent>(actor).DoAfters.Values.Single(), Is.SameAs(pending));
            var pendingEvent = (ActionDoAfterEvent) pending.Args.Event;
            Assert.That(pendingEvent.Predicted, Is.False);
            Assert.That(pendingEvent.ShowPopups, Is.False);
            probe.Accept = true;
        });

        await Server.WaitRunTicks(15);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SComp<CECheckedActionProbeComponent>(actor).Calls, Is.GreaterThan(0));
            Server.System<SharedActionsSystem>().SetEnabled(action, false);
        });
        await Server.WaitRunTicks(15);
        await Server.WaitAssertion(() => Assert.That(pending.Cancelled, Is.True));
    }
}

[RegisterComponent]
public sealed partial class CECheckedActionProbeComponent : Component
{
    public int Calls;
    public int Dispatches;
    public EntityUid? LastTarget;
    public bool Accept = true;
}

public sealed partial class CECheckedEntityEvent : EntityTargetActionEvent;
public sealed partial class CECheckedWorldEvent : WorldTargetActionEvent;

public sealed partial class CECheckedActionProbeSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<CECheckedEntityEvent>(OnEntity);
        SubscribeLocalEvent<CECheckedWorldEvent>(OnWorld);
    }

    private void OnEntity(CECheckedEntityEvent args)
    {
        if (!TryComp<CECheckedActionProbeComponent>(args.Performer, out var probe))
            return;
        probe.Dispatches++;
        if (!probe.Accept)
            return;
        probe.Calls++;
        probe.LastTarget = args.Target;
        args.Handled = true;
    }

    private void OnWorld(CECheckedWorldEvent args)
    {
        if (!TryComp<CECheckedActionProbeComponent>(args.Performer, out var probe) || !probe.Accept)
            return;
        probe.Calls++;
        args.Handled = true;
    }
}
