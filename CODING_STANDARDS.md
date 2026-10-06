# CE Coding Standards

Review rules for CE changes. Basic style and ECS rules are in `CLAUDE.md`; this file covers what review must catch on top of them.

## Reuse before adding

- Extend an existing component, system, or prototype before adding a parallel one. A new component that duplicates the job of an existing one with a different target (e.g. a second perceptor for objects next to the eye perceptor) is a finding: the existing one gets a config field instead.
- Shared prototype data (shop slots, AI blocks, factions) goes into a parent prototype or a reusable prototype, not copied between siblings.

## File layout

- `CEEntityEffect` / `CEEntityCondition` types live in `Content.Shared/_CE/EntityEffect/Effects` (or `/Conditions`): the data class and its `CEEntityEffectSystem<T>` / `CEEntityConditionSystem<T>` in one file, even when the effect serves one feature. Server-only work is guarded with `_net.IsClient` inside the shared system; move the needed API into shared rather than putting the effect system on the server. Client/server-only systems are acceptable only when the API cannot exist in shared (animations, chat).
- Events of a feature go at the end of its shared system file, not in a separate events file.

## Comments

- One-line comments on what a type or function does. Comments name no content or callers ("when the shop closes at night", "used by trade tables"); such context rots as content changes.

## Localization

- Strings shown through `PopupEntity`/`PopupClient`/`PopupPredicted` are plain text: no `[color]`, `[bold]` or other markup in their ftl values. Strip markup from interpolated values with `FormattedMessage.RemoveMarkupPermissive`.
