# Localization (Fluent) Guidelines

Files: `en-US/` and `ru-RU/`, CrystallEdge-specific under `*/_CE/`.

```fluent
ce-example-message = This is a CE message
ce-example-with-variable = Hello, { $name }!
```

- Keep key names identical across all locale folders
- Keep `{$variable}` placeholders consistent

**Important**: EntityPrototype `name`, `description`, and `suffix` are localized via a separate script inside _PROTO folder:
- English: write directly in the prototype YAML
- Russian: do NOT add ru-RU localization for entity names/descriptions/suffixes — the script handles that separately
- For Russian, only localize everything _other_ than entity name/description/suffix
