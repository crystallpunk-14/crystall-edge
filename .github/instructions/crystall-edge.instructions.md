# CrystallEdge Project Instructions (overview)

This repository contains several focused instruction files under `.github/instructions/` which provide rules for particular file types. The rules are split so the guidance only applies where relevant (code, prototypes, localization, etc.).

See the specific instruction blocks below for targeted rules that will be applied automatically based on file type.

## Instruction blocks

- `code.instructions.md` — C# coding conventions and ECS guidelines. applyTo: `**/*.cs`
- `prototypes.instructions.md` — YAML prototype style and examples. applyTo: `Resources/Prototypes/**`
- `localization.instructions.md` — localization file guidelines (Fluent). applyTo: `Resources/Locale/**`
- `general.instructions.md` — testing, building and general project notes. applyTo: `**`

Each of those files contains the detailed guidance previously in this document; splitting them keeps the rules targeted and avoids irrelevant rules being applied to unrelated files.
