---
applyTo: "Resources/Prototypes/**"
---

# Prototypes (YAML) Guidelines

### File Location
- General prototypes: `Resources/Prototypes/`
- CrystallEdge-specific: `Resources/Prototypes/_CE/`

### Prototype Style
- Use 2-space indentation for YAML
- Follow existing naming conventions for IDs
- Entity IDs: `PascalCase` (e.g., `FoodBreadPlain`)
- Use inheritance for entity-type prototypes; create abstract parent prototypes for common data
- If categories are used in IDs, put the most general category first (e.g., `CEWeaponDagger`)

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
