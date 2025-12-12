---
applyTo: "Resources/Locale/**"
---

# Localization (Fluent) Guidelines

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

### Notes
- Keep keys consistent across locales (same key names in each language folder)
- Keep variable placeholders consistent (`{$name}`, `{$count}` etc.)
- Prefer short and clear messages; translation teams may adjust style
