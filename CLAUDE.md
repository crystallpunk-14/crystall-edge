# CrystallEdge — Claude Code Instructions

CrystallEdge (CE) is a fork of Space Station 14 built on RobustToolbox. It uses an ECS architecture with C# on both server and client.

## Building

The project takes ~5 minutes for a cold build. Always build as a background process.

```powershell
Start-Process -FilePath "dotnet" `
  -ArgumentList "build","Content.Server/Content.Server.csproj","-v","q" `
  -Wait -NoNewWindow `
  -RedirectStandardOutput "build_out.txt" `
  -RedirectStandardError "build_err.txt"
```

Check results:
```powershell
$errs = Get-Content build_err.txt | Select-String "error CS"
Write-Host "ERRORS=$($errs.Count)"
$errs | ForEach-Object { $_.Line }
Get-Content build_out.txt | Select-Object -Last 5
Remove-Item build_out.txt, build_err.txt -ErrorAction SilentlyContinue
```

Build output is in Russian: `Ошибок: 0` = success, `Предупреждений: N` = N warnings (100+ upstream warnings from RobustToolbox are expected and not errors).

| Target | Command |
|--------|---------|
| Server | `dotnet build Content.Server/Content.Server.csproj` |
| Client | `dotnet build Content.Client/Content.Client.csproj` |
| Full | `dotnet build` |

Incremental builds (CE files only changed) take ~15-30s. Never run `dotnet build` as a foreground command with short timeouts — it will be cancelled.

## Testing

```powershell
dotnet test Content.Tests/Content.Tests.csproj
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj
```

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

      public override void Initialize()
      {
          base.Initialize();
          SubscribeLocalEvent<CEExampleComponent, SomeEvent>(OnSomeEvent);
      }

      private void OnSomeEvent(Entity<CEExampleComponent> ent, ref SomeEvent args)
      {
          // logic
      }
  }
  ```

- Use `[Dependency]` for dependency injection WITHOUT readonly key.
- Use `[Dependency] EntityQuery<T>` for performance-critical component lookups
- All classes that use [Dependency] should be partial.

**Critical ECS rules:**
- Never store mutable state inside systems — it is not saved/loaded with the game save. All persistent data belongs in components.
- Before adding `SubscribeLocalEvent<Comp, Event>()`, verify that exact Comp+Event pair is not already subscribed — the engine throws on duplicate subscriptions.
