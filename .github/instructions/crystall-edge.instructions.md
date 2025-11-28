---
applyTo: "**"
---

# CrystallEdge Project Instructions

This document provides guidelines for contributing to the CrystallEdge project, a game built on the [RobustToolbox](https://github.com/space-wizards/RobustToolbox) engine, based on [Space Station 14](https://github.com/space-wizards/space-station-14).

## Project Structure

### Core Modules
- `Content.Server/` - Server-side C# code
- `Content.Client/` - Client-side C# code
- `Content.Shared/` - Shared code between client and server
- `Content.Tests/` - Unit tests
- `Content.IntegrationTests/` - Integration tests
- `RobustToolbox/` - Engine submodule (do not modify directly)
- `Resources/` - Game assets (prototypes, locale, textures, audio, maps)

### CrystallEdge-Specific Code
Code specific to CrystallEdge (not from upstream Space Station 14) is located in `_CE` folders:
- `Content.Server/_CE/`
- `Content.Client/_CE/`
- `Content.Shared/_CE/`
- `Resources/Prototypes/_CE/`
- `Resources/Locale/*/_CE/`

## Coding Conventions

### C# Code Style
- Use **file-scoped namespaces** (`namespace Content.Shared.Example;`)
- Use **4 spaces** for indentation (not tabs)
- Private fields: `_camelCase` with underscore prefix
- Public members: `PascalCase`
- Local variables and parameters: `camelCase`
- Interfaces: `IPascalCase` (prefix with `I`)
- Type parameters: `TPascalCase` (prefix with `T`)
- Use `var` when the type is apparent from the right side
- Use expression-bodied members for simple properties and accessors
- Maximum line length: **120 characters**
- Always include final newline in files
- Braces on new lines (Allman style)
- You should try to minimize the use of LINQ in heavy areas where frequent allocation can affect performance.

### Entity-Component-System (ECS) Architecture
CrystallEdge uses an ECS architecture:
- **Components**: Data containers (`[RegisterComponent]`, `[NetworkedComponent]`)
- **Systems**: Logic handlers that operate on components
- Components should not contain logic; systems should process component data
- Use `[Dependency]` for dependency injection
- Use `EntityQuery<T>` for performance-critical component lookups

### Example System Structure
```csharp
namespace Content.Shared.Example;

public sealed class ExampleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExampleComponent, SomeEvent>(OnSomeEvent);
    }

    private void OnSomeEvent(EntityUid uid, ExampleComponent component, SomeEvent args)
    {
        // Logic here
    }
}
```

### Component Structure
```csharp
namespace Content.Shared.Example;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExampleComponent : Component
{
    [DataField]
    public string SomeField = string.Empty;

    [DataField, AutoNetworkedField]
    public int NetworkedValue;
}
```

## Prototypes (YAML)

### File Location
- General prototypes: `Resources/Prototypes/`
- CrystallEdge-specific: `Resources/Prototypes/_CE/`

### Prototype Style
- Use 2-space indentation for YAML
- Follow existing naming conventions for IDs
- Entity IDs: `PascalCase` (e.g., `FoodBreadPlain`)
- Prototype type prefix in ID (e.g., `ActionToggle...`, `Recipe...`)
- It is good practice to use inheritance for entity-type prototypes, with an abstract parent prototype containing common data.

### Example Entity Prototype
```yaml
- type: entity
  id: ExampleEntity
  parent: ExampleParentEntity
  abstract: true #For abstract parents
  name: example entity
  description: An example entity.
  components:
  - type: Sprite
    sprite: Objects/example.rsi
    state: icon
  - type: Example
    someField: value
```

## Localization (Fluent)

### File Location
- English: `Resources/Locale/en-US/`
- Russian: `Resources/Locale/ru-RU/`
- CrystallEdge-specific: `Resources/Locale/*/_CE/`

### Fluent Syntax
```fluent
example-message = This is an example message
example-with-variable = Hello, { $name }!
example-component-examine = The { $item } looks { $condition }.
```

## Testing

### Running Tests
```bash
dotnet test Content.Tests
dotnet test Content.IntegrationTests
```

### Test Conventions
- Test classes: `*Tests.cs`
- Test methods: descriptive names using `Should` pattern
- Use fixtures from `Robust.UnitTesting`

## Building

### Prerequisites
- .NET SDK (see `global.json` for version)
- Run `RUN_THIS.py` to initialize submodules

### Build Commands
```bash
dotnet build              # Build all projects
dotnet build -c Release   # Build in Release mode
```

### Running
```bash
./runserver.sh   # Start server (Linux/Mac)
./runclient.sh   # Start client (Linux/Mac)
runserver.bat    # Start server (Windows)
runclient.bat    # Start client (Windows)
```

## Pull Request Guidelines

### Required Information
1. **About the PR**: Describe what changed
2. **Why / Balance**: Explain reasoning and game balance impact
3. **Media**: Screenshots/videos for visual changes
4. **Changelog**: Use changelog format for player-facing changes

### Changelog Format
```
:cl:
- add: Added new feature
- remove: Removed old feature
- tweak: Changed existing behavior
- fix: Fixed a bug
```

## Licensing

- CrystallEdge-specific code (`_CE` folders): See `LICENSE_CE.TXT` - All rights reserved
- Upstream Space Station 14 code: MIT License (`LICENSE.TXT`)
- Assets: CC-BY-SA 4.0 unless specified otherwise
- **Do not use CrystallEdge code in other projects without explicit permission**

## Additional Resources

- [RobustToolbox Documentation](https://docs.spacestation14.com/)
- [Space Station 14 Development Docs](https://docs.spacestation14.com/en/general-development/setup.html)
- [Discord](https://discord.gg/Sud2DMfhCC)
