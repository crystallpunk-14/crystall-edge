# CE GOAP

Goal-oriented action planning for CE mobs. Data types live here in `Content.Shared/_CE/GOAP/`, all logic in `Content.Server/_CE/GOAP/`, prototypes in `Resources/Prototypes/_CE/GOAP/`. HTN/`ActiveNPCComponent`/vanilla NPC steering are not used by GOAP agents.

## Agent tick

`CEGOAPSystem` updates every agent with `CEActiveGOAPComponent`:

1. **Knowledge** — perceptors (`Perceptors/`, e.g. eyes, pain) call `Remember`/`Forget`. Entries expire after `MemoryDuration`. Changes of the known set are flushed once per tick as `CEGOAPKnowledgeUpdatedEvent`.
2. **Sensors** write `WorldState` keys. They re-evaluate on knowledge updates, on their own events, or by polling.
3. **Replan** every `PlanCooldown`: actions that pass `CanExecute` and whose target slot resolves are costed (`Cost` + `DistanceCost` per tile to the target) and fed to `CEGOAPPlanner` (forward A* over a bitmask of keys). Goals are tried by descending `Priority`; a goal is active when its `Preconditions` match and its `DesiredState` doesn't yet.
4. **Execute** the current action. If it has `range`, the orchestrator first walks the agent there via steering and pauses the action whenever the target leaves range.

Sleeping (`CEGOAPSleepingComponent`, `CEGOAPSleepingSystem`, `CEGOAPSystem.Wake`) removes `CEActiveGOAPComponent`; agents wake on player proximity or damage. A player attached to the body puts GOAP to sleep.

## Modules

| Module | Role |
|---|---|
| `CEGOAPComponent` | Per-agent data: behaviors, target slots, goals, actions, knowledge, world state, current plan. |
| Condition (`GOAPCondition` prototype) | A world state key. Every key used in YAML must be declared in `conditions.yml`. |
| Target slot (`GOAPTarget` prototype + selector) | Named target (`Enemy`, `DissolvedSoul`) resolved by a `CEGOAPTargetSelector` (`Selectors/`) from knowledge, filtered by `CEEntityCondition`s. Actions and sensors reference slots, never entities. |
| Action (`CEGOAPAction`) | Preconditions, effects, cost, optional `target` slot and `range`. Logic in a `CEGOAPActionSystem<T>` (`Actions/`). |
| Goal (`CEGOAPGoal`) | Desired state, activation preconditions, priority. |
| Sensor (`CEGOAPSensorEntry` + component + `CEGOAPSensorSystem<,>`) | Evaluates one key from its (optional) target slot. |
| Behavior package (`GOAPBehavior` prototype, `behaviors.yml`) | Reusable bundle of slots, goals, actions, sensors, with `includes`. Mobs list them in `behaviors:`; merged on MapInit. |
| Steering (`Content.Server/_CE/GOAP/Steering/`) | CE copy of vanilla context steering. API: `Navigate(uid, coords/entity, range)`, `Continue`, `Stop`; handles freeing (unbuckle/unpull/escape storage), climb, smash, pry, Z-levels. |

## Design rules

- **Sensors own world state.** Keys change only through sensors. Action `effects` are planning promises: the orchestrator does not write them, so a goal completes when a sensor observes the result.
- **Movement is not a plan step.** An action that needs to be near its target declares `target` + `range`; never add "move to" actions or "in range" keys. All GOAP movement goes through `CEGOAPSteeringSystem`.
- **A target is an implicit precondition.** An action with a `target` is unplannable while the slot resolves to nothing; don't mirror that with "has target" keys.
- **Shared data is immutable.** Goals, actions, selectors and sensor entries are shared by every mob using the prototype/package. Per-agent runtime state goes into components (keyed per entry if needed), never into these objects or into systems.
- **Reuse before inventing.** Prefer composing a behavior package from existing actions, selectors, sensors and `CEEntityCondition`s; repeated AI blocks across mobs belong in `behaviors.yml`.
- **Generic over specific.** New sensors/selectors/actions are parameterised by data (slot, conditions, thresholds), not by the mob they were written for.
- **Other systems talk to GOAP through its API** (`Remember`, steering API, events); they don't edit `WorldState`, plans or steering fields directly.

## Extending

- **Action:** class `CEGOAPXAction : CEGOAPActionBase<CEGOAPXAction>` with `[DataField]` settings + `CEGOAPXActionSystem : CEGOAPActionSystem<CEGOAPXAction>`. Override only the hooks you need (`OnCanExecute`, `OnActionStartup`, `OnActionUpdate` → set `args.Status`, `OnActionShutdown` — always clean up there). Return `Running` while waiting (cooldowns, animations), `Failed` only when the action cannot succeed.
- **Sensor:** entry `: CEGOAPSensorEntry<TEntry, TComp>`, component `: CEGOAPSensorComponent<TEntry>`, system `: CEGOAPSensorSystem<TComp, TEntry>` implementing `Evaluate`. Trigger via events and call `EvaluateAll`; poll in `Update` only when there is no event, with an interval field.
- **Selector:** `: CEGOAPTargetSelectorBase<T>` + `CEGOAPTargetSelectorSystem<T>`; set `ev.Entity` and/or `ev.Position`. A selector must be deterministic for the same knowledge: it is resolved every tick.
- **Filter for selectors:** add a `CEEntityCondition` (`Content.Shared/_CE/EntityEffect/Conditions/`) rather than a selector field.
- **Generic class chains** (base → generic middle → concrete) need `[ImplicitDataDefinitionForInheritors]` on the root, otherwise root `[DataField]`s are silently dropped.

## Verify

`Content.IntegrationTests/Tests/_CE/CEGOAPRatAttackTest.cs` is the end-to-end check (perception → slot → plan → steering over a fence → bite). Run it after changes to the orchestrator, planner, steering or combat packages:

```
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c Tools --no-build --filter "FullyQualifiedName~CEGOAPRatAttackTest"
```
