---
applyTo: "**/*.cs"
---

# C# Code Style & ECS Guidelines

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
- Minimize LINQ in performance-critical areas to avoid allocations

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
