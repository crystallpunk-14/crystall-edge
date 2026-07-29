# YAML Prototypes Guidelines

CrystallEdge-specific files go in `Resources/Prototypes/_CE/`. Upstream prototypes go in `Resources/Prototypes/`.

- 2-space indentation
- Entity IDs: `PascalCase` — e.g., `FoodBreadPlain`

```yaml
- type: entity
  id: CEExampleItem
  parent: BaseItem
  name: example item
  description: An example CE item.
  components:
  - type: Sprite
    sprite: Objects/_CE/example.rsi
    state: icon
  - type: CEExample
    someField: value
```
