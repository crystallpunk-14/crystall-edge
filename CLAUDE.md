# CrystallEdge — Claude Code Instructions

CrystallEdge (CE) is a fork of Space Station 14 built on RobustToolbox. It uses an ECS architecture with C# on both server and client.

## Building

The project takes ~5 minutes for a full build. 

The raw build output is flooded with RobustToolbox warnings. To see only errors (PowerShell):
```
$out = dotnet build -c Tools -clp:ErrorsOnly -nologo 2>&1; $out | Select-String -Pattern ": error |Build succeeded|FAILED|Time Elapsed|\d+ Error" | Select-Object -Unique
```

Never build only shared: build server, client or both via full dotnet build.

`MSB3027` / "file is locked" errors mean the user's Content.Server or Content.Client is running and holds the DLLs in `bin/`. Ask the user to close them; rebuilding won't help.

## Testing

Reuse the Tools build and target the tests you need; without `-c Tools --no-build` the test run rebuilds everything in Debug (~5 min):
```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c Tools --no-build --filter "FullyQualifiedName~TestName"
```

## Searching

Repo-wide Grep times out (~54k files, mostly textures and maps). Always pass a path: `Content.*`, `Resources/Prototypes`, `Resources/Locale`, or `Resources/Maps` when maps are the target.

## C# Code Style

- File-scoped namespaces: `namespace Content.Shared.Example;`
- 4 spaces indentation (no tabs)
- Private fields: `_camelCase` (underscore prefix)
- Public members: `PascalCase`
- Local variables/parameters: `camelCase`
- Interfaces: `IPascalCase`, type parameters: `TPascalCase`
- Use `var` when type is apparent from the right side
- Allman braces (opening brace on new line)
- Expression-bodied members for simple properties
- Always include final newline in files
- Minimize LINQ in performance-critical paths (allocations)

## CE Code Organization Rules

**Always place CE code in `_CE/` subfolders** and prefix class names with `CE`:
- `Content.Shared._CE.MyFeature.CEMySystem`
- `Content.Server._CE.MyFeature.CEMyServerSystem`

**When editing upstream (non-_CE) code**, wrap changes in comments:
```csharp
// CrystallEdge: reason for the edit
... changed multiline ...
... code ...
// CrystallEdge end

or

changed inline 1-line edit // CrystallEdge: reason for the edit
```

never add //CrystallEdge comments for using blocks

## ECS Architecture

- **Components**: Pure data containers — no logic
  ```csharp
  [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
  public sealed partial class CEExampleComponent : Component
  {
      [DataField]
      public string SomeField = string.Empty;

      [DataField, AutoNetworkedField]
      public int NetworkedValue;
  }
  ```

- **Systems**: All logic lives here
  ```csharp
  namespace Content.Shared._CE.Example;

  public sealed partial class CEExampleSystem : EntitySystem
  {
      [Dependency] private IGameTiming _timing = default!;

      [SubscribeLocalEvent]
      private void OnSomeEvent(Entity<CEExampleComponent> ent, ref SomeEvent args)
      {
          // logic
      }
  }
  ```

- Use `[Dependency]` for dependency injection WITHOUT readonly key.
- Use `[Dependency] EntityQuery<T>` for performance-critical component lookups
- All classes that use [Dependency] should be partial.

**Event subscriptions:** put `[SubscribeLocalEvent]` / `[SubscribeNetworkEvent]` / `[EventSubscription]` (= SubscribeAllEvent) on the handler method itself — comp/event are inferred from the signature. Don't call `SubscribeLocalEvent<...>()` in `Initialize()`. Use `[SubscribeLocalEvent(before: [typeof(X)], after: [typeof(Y)])]` for ordering. Fall back to a manual `Subscribe*Event()` call only where the attribute can't work: generic systems, `UIController`s, lambda handlers, conditional subscriptions.

**Critical ECS rules:**
- Never store mutable state inside systems — it is not saved/loaded with the game save. All persistent data belongs in components.
- Before subscribing a `Comp+Event` pair (attribute or manual call), verify it is not already subscribed — the engine throws on duplicate subscriptions.
