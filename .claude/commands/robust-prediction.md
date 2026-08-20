# robust-prediction: Client-Side Prediction in RobustToolbox

Use this command when converting a server-only system to a predicted (client-side) system, fixing prediction bugs, or implementing new predicted mechanics.

## How Prediction Works

Without prediction: client input → server simulates → result sent back (visible latency).

With prediction: client runs its own simulation immediately for local feedback. When server state arrives (from the past due to latency), client rewinds to that point, applies server corrections, and resimulates forward while replaying inputs. This means your code may execute multiple times per tick during resimulation.

**Key concepts:**
- **Time travel**: Client constantly rewinds and resimulates
- **Authoritative server**: Server state is always truth
- **Local prediction only**: Client predicts own inputs, not other players'
- **Reconciliation**: Client corrects disagreements with server state

## Quick Conversion Checklist

1. **Move to Shared** — components and systems go in `Content.Shared._CE.*`
2. **Network the component** — add `[NetworkedComponent]` and `[AutoGenerateComponentState]`
3. **Mark changing fields** — add `[AutoNetworkedField]` to fields that change
4. **Update system** — create shared system or abstract base + client/server implementations
5. **Use predicted APIs** — replace popup/audio/delete methods with predicted variants
6. **Convert dependencies** — all systems your system calls must also be shared/predicted
7. **Call `Dirty()`** — after changing any networked field, call `Dirty(uid, component)`

## Component Setup

```csharp
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CEYourComponent : Component
{
    [DataField, AutoNetworkedField]
    public int NetworkedValue;

    [DataField]
    public string ServerOnlyField = string.Empty; // not AutoNetworkedField = not synced
}
```

## System Architecture Options

```csharp
// Option 1: Single shared system (preferred for simple cases)
public sealed class CEYourSystem : EntitySystem { }

// Option 2: Abstract base with server/client implementations
public abstract class CESharedYourSystem : EntitySystem { }
public sealed class CEYourSystem : CESharedYourSystem { }       // Server
public sealed class CEClientYourSystem : CESharedYourSystem { } // Client
```

## Predicted API Replacements

| Instead of | Use |
|-----------|-----|
| `_popup.PopupEntity(...)` | `_popup.PopupPredicted(...)` |
| `Del(uid)` / `DeleteEntity(uid)` | `_entities.PredictedDeleteEntity(uid)` |
| `_audio.PlayPvs(...)` | `_audio.PlayPredicted(...)` |
| `_random.Next(...)` in shared | Use predicted RNG — see `references/randomness-determinism.md` |

## Dependency Rule

**Shared code can ONLY call other shared code.** Before predicting a system, convert all its dependencies to shared first.

## Common Pitfalls

- Using `PopupEntity` instead of `PopupPredicted` → client sees popup multiple times during resimulation
- Using `DeleteEntity` instead of `PredictedDeleteEntity` → networking errors, entity deleted on client but not server
- Forgetting `Dirty()` after field changes → client has stale data
- Putting `[NetworkedComponent]` on server-only components → silent failures
- Missing client system implementation → prediction silently disabled
- Using `_random` directly in shared code → mispredicts (different random values on client vs server)

## Reference Guides

For detailed implementation steps, read the relevant file from `.github/skills/robust-prediction/references/`:

| Topic | File |
|-------|------|
| Component networking, field deltas, NetSync | `component-networking.md` |
| Predicted entity spawn/delete, WeakEntityReference | `entity-management.md` |
| IsFirstTimePredicted, ApplyingState, event handling | `timing-state.md` |
| Efficient update loops, timestamp patterns | `update-loops-performance.md` |
| Player Visibility System, PVS overrides, nullspace | `pvs-system.md` |
| Predicted popups/audio, BUI conversion | `ui-interfaces.md` |
| Predicted RNG, determinism, security | `randomness-determinism.md` |
| SendOnlyToOwner, SessionSpecific, cheat prevention | `security-sessions.md` |
| Diagnostic checklist, common issues | `troubleshooting.md` |

Start with `troubleshooting.md` if you're debugging an issue, or `component-networking.md` if starting a new implementation.
