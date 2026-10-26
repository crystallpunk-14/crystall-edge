# CrystallEdge — Cursor Instructions

This fork is becoming an action RPG using the D&D SRD 5.2.1 rules. CrystallEdge (`_CE`) and upstream SS14 stay in the tree as the engine and as reference. The new game is `_DND`.

`SRD_CC_v5.2.1.pdf` in the repo root is the rules reference. Implement the mechanics. Do not paste long SRD prose into code, prototypes, or locale files.

There is no turn order. Do not add initiative rounds, and do not write "turn-based" into code, UI, or notes.

## Rules for this game

### English only

The D&D game is English. Do not add or edit `Resources/Locale/ru-RU` for it, and do not run the CrystallEdge Russian localization skill for `_DND` strings.

Write player text in `Resources/Locale/en-US/_DND/`.

Reading `ru-RU` is fine when it is the only write-up of something that is not in the English files yet. Use that to learn what the content is. The D&D version of it, if we need one, is written in English under `_DND`.

### `_CE` maps and jobs are reference

Do not extend CrystallEdge maps or the old job list for this game. That includes `Resources/Maps`, `Resources/Prototypes/_CE/Maps`, and `Resources/Prototypes/_CE/Roles/Jobs`. Sheriff, townfolk, adventurer, guild, tavern, and the other `_CE` jobs are not D&D classes.

New prototypes, maps, and roles go in `Resources/Prototypes/_DND/`. New C# goes in `Content.Shared._DND`, `Content.Server._DND`, and `Content.Client._DND`, with a `Dnd` type prefix (`DndClassSystem`, `DndAbilityPrototype`).

`_CE` is there to read. Match its engine patterns when they are the way RobustToolbox works. Do not add new gameplay inside `_CE`.

The character screen is the exception already in the tree: the lobby opens `CEHumanoidProfileEditor`, so D&D tabs are added on that screen. New systems behind those tabs still live in `_DND`.

## Where the work stands

Character creation keeps the appearance tab. The roles, traits, and flavor-text tabs are still in the editor and hidden. Do not delete them.

Visible D&D tabs:

- **Class.** One choice. Barbarian, Bard, Cleric, Druid, Fighter, Monk, Paladin, Ranger, Rogue, Sorcerer, Warlock, Wizard, and NPC. Default is Fighter. Prototype kind `dndClass`, ids like `Fighter` and `Npc` (no extra prefix).
- **Abilities.** Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma. Prototype kind `dndAbility`. The modifier is `floor((score - 10) / 2)`, and division rounds down, including negative halves, so 1 is −5 and 20 is +5. A score of 30 is +10. The PDF row that looks like "+1" for 30 is that +10 split across a line.
- **Ability generation** is Point Buy (default) or Standard Array. Do not roll dice for scores. Point Buy has 27 points, scores 8–15, and the cost table 8=0, 9=1, 10=2, 11=3, 12=4, 13=5, 14=7, 15=9. Standard Array assigns 15, 14, 13, 12, 10, 8 once each.
- **Background** is applied after those base scores. SRD backgrounds are Acolyte (Int, Wis, Cha), Criminal (Dex, Con, Int), Sage (Con, Int, Wis), and Soldier (Str, Dex, Con). The player raises one listed score by 2 and a different one by 1, or all three by 1. The increase cannot push a score above 20. Modifiers are written from the final score.
- **Proficiency bonus** comes from level, not from an ability score. Level 1 is +2. It rises by 1 at levels 5, 9, 13, and 17, up to +6 at level 17–20. `DndAbilityScores.ProficiencyBonus`.
- **Base AC** is 10 + the Dexterity modifier. That is the only base calculation unless a class feature replaces it. Barbarian Unarmored Defense, while no body armor is worn, is 10 + Dexterity + Constitution. A shield does not turn that off. Armor and other modifiers come after that base. A creature can instead carry `DndArmorClassComponent` when its AC is not that base.
- **Sides.** Adventurers, Goblins, and Wolves each form a party. A creature does not harm its own side, and NPC targeting treats that side as friendly. A swing at a different side lands. `Neutral` starts no fight, and a swing against it still lands. A class body is `Adventurers`. A monster prototype carries `DndAllegiance` with `side: Goblins` or `side: Wolves`. `DndAllegianceShift` replaces that side while the component is on the creature, which is how a charm or a domination joins another party. Removing the shift puts the creature back.
- **Attacks roll 1d20** + Strength or Dexterity (the higher of the two for a finesse weapon) + proficiency, against the target's AC. A natural 20 hits. A natural 1 misses. A hit rolls the weapon's damage dice and adds the same ability modifier. A fixed amount adds the modifier only when that weapon says so, as an unarmed strike does (1 + Strength). Damage does not go below 0. A critical hit rolls the damage dice twice and adds the modifier once.
- **One tile is 5 feet.** Melee reach is 5 feet, so the stored CE weapon range is half a tile because those rays travel twice the stored range. Ranged weapons stop at their long range in feet.
- **Damage types** are the SRD list. Resistance halves (round down), vulnerability doubles, and several of the same kind count once. Order is modifiers, then resistance, then vulnerability. Immunity deals none of that type. `DndDamageReductionComponent` stores them.
- **Hit points** are the class hit die, Constitution modifier, and level. Level 1 is the die's maximum plus Constitution. Each later level adds the die's average, rounded up, plus Constitution, and never less than 1. That total is the damage that drops the character. Barbarian d12, Fighter/Paladin/Ranger d10, Sorcerer/Wizard d6, the other classes d8.
- **Barbarian, level 1.** Primary ability Strength. Saves are Strength and Constitution. The player chooses 2 skills from Animal Handling, Athletics, Intimidation, Nature, Perception, and Survival. Weapon training is simple and martial. Armor training is light, medium, and shields. Starting gear is a greataxe, 4 handaxes, an explorer's pack, and 15 GP, or 75 GP. Features are Rage, Unarmored Defense, and Weapon Mastery (2 melee kinds; greataxe and handaxe by default).
- **Bard, level 1.** Primary ability Charisma. Hit die d8. Saves are Dexterity and Charisma. The player chooses any 3 skills, simple weapons, light armor, and 3 musical instruments. Starting gear is leather armor (AC 11 + Dexterity), 2 daggers, the first chosen instrument, an entertainer's pack, and 19 GP, or 90 GP. Bardic Inspiration is a bonus action (3 seconds) that gives one creature within 60 feet a d6. On a failed d20 test within the next hour, that die is rolled when it could turn the failure into a success, and then it is gone. Uses equal the Charisma modifier, at least 1, and a long rest returns them. Spellcasting knows 2 cantrips and prepares 4 level 1 bard spells. Charisma is the spellcasting ability. Leveled spells share 2 slots, shown on each spell button, and a long rest returns them. Cantrips wait 6 seconds and spend no slot. Healing Word is a bonus action. A spell that needs a focus requires a musical instrument on the bard.
- **Cleric, level 1.** Primary ability Wisdom. Hit die d8. Saves are Wisdom and Charisma. Choose 2 skills from History, Insight, Medicine, Persuasion, and Religion. Simple weapons. Light armor, medium armor, and shields. Gear is a chain shirt (AC 13 + Dexterity, max +2), a shield (+2), a mace, a holy symbol, a priest's pack, and 7 GP, or 110 GP. Spellcasting knows 3 cantrips and prepares 4 level 1 cleric spells. Wisdom is the spellcasting ability. A holy symbol focuses a spell that needs one. Two spell slots, restored on a long rest. For one minute after that rest, right-click yourself or the bed to change prepared spells. Divine Order is Protector (martial weapons and heavy armor) or Thaumaturge (one extra cantrip, and Arcana and Religion checks add the Wisdom modifier, at least +1).
- **Druid, level 1.** Primary ability Wisdom. Hit die d8. Saves are Intelligence and Wisdom. Choose 2 skills from Animal Handling, Arcana, Insight, Medicine, Nature, Perception, Religion, and Survival. Simple weapons, light armor, shields, and an herbalism kit. Gear is leather armor, a shield, a sickle, a druidic focus (quarterstaff), an explorer's pack, an herbalism kit, and 9 GP, or 50 GP. Spellcasting knows 2 cantrips and prepares 4 level 1 druid spells. Wisdom is the spellcasting ability. A druidic focus focuses a spell that needs one. Two spell slots, restored on a long rest, with the same one-minute window to change prepared spells. Speak with Animals is always prepared and does not count against the four. Druidic lets you leave a hidden sign: another druid reads it, and anyone else only notices it with a DC 15 Intelligence (Investigation) check. Primal Order is Magician (one extra cantrip, and Arcana and Nature checks add the Wisdom modifier, at least +1) or Warden (martial weapons and medium armor). Magician is the default.
- **Spells** are filed by school, then by level: `Resources/Prototypes/_DND/Spells/<School>/level-<n>.yml`. The schools are Abjuration, Conjuration, Divination, Enchantment, Evocation, Illusion, Necromancy, and Transmutation. Level 0 is a cantrip. `lists` says which class lists include the spell. The folder is the school.
- **Rage** is a bonus action, so it waits 3 seconds before it can be pressed again. The button shows that countdown and a second number for uses left. Level 1 has 2 uses and +2 damage. Pressing it again while it is up refreshes the rage and does not spend a use. It does not start in heavy armor. While it lasts, bludgeoning, piercing, and slashing damage are resisted, Strength attacks (including a finesse tie) add the rage bonus, and spells cannot be cast. It lasts 6 seconds unless an attack roll against an enemy, or the rage action again, refreshes it. It ends after 10 minutes, or earlier if you are incapacitated or put on heavy armor. A short rest is one quiet minute beside a bonfire or anything you can buckle to, and returns one use. A long rest is ten quiet minutes in a bed and returns all of them. Right-click the object and choose the rest. Damage interrupts a rest.
- **Weapon mastery** lets the practiced kinds use their property. Cleave and Nick each happen once per 6 seconds. Graze, Push (10 feet, Large or smaller), Sap, Slow (10 feet for 6 seconds), Topple (Constitution save), and Vex last through the next attack or 6 seconds. A long rest allows one mastery to be changed. That change is an action, so the drill waits 6 seconds, and its number is how many changes are left.
- **Action pace.** An action waits 6 seconds. A bonus action waits 3 seconds. A feature action with a limited number of uses shows that count on the button next to the countdown. A short rest is the right-click menu on a bonfire (`DndBonfire`) or anything with `Strap` (chairs, stools, benches, beds, and other seats). A long rest is that same menu on a bed (`HealOnBuckle`).

NPC (`spawnAsGhost: true`) joins as an observer so the player can take a ghost role. The other classes spawn a body, and the class is stored on `DndClassComponent`.

Spawn still borrows the CrystallEdge overflow job `CETownfolk` so the ticker has a body slot. That is a temporary bridge, not a pattern. Do not add more `_CE` jobs or map slots to make classes spawn. The next spawn path should be `_DND`.

## Building

A full build is about 5 minutes. Build server, client, or both. Do not build only `Content.Shared`.

On Linux, errors only:

```bash
dotnet build Content.Client/Content.Client.csproj -c Tools -clp:ErrorsOnly -nologo --verbosity quiet
dotnet build Content.Server/Content.Server.csproj -c Tools -clp:ErrorsOnly -nologo --verbosity quiet
```

`MSB3027` / "file is locked" means Content.Server or Content.Client is running and holding DLLs in `bin/`. Ask the user to close them.

## Testing

Reuse the Tools build. Without `-c Tools --no-build` the test run rebuilds Debug.

```bash
dotnet test Content.Tests/Content.Tests.csproj -c Tools --filter "FullyQualifiedName~TestName"
```

`DndAbilityScoresTest` covers the modifier table.

## Searching

Repo-wide search times out (textures and maps). Pass a path: `Content.Shared/_DND`, `Content.Server/_DND`, `Content.Client/_CE/Lobby`, `Resources/Prototypes/_DND`, or `Resources/Locale/en-US/_DND`.

## C# style

Same bar as the engine code:

- File-scoped namespaces
- 4 spaces, no tabs
- Private fields `_camelCase`, public members `PascalCase`, locals `camelCase`
- `var` when the right side shows the type
- Allman braces
- Final newline
- `[Dependency]` without `readonly`, on a `partial` class
- `[SubscribeLocalEvent]` on the handler. Do not subscribe again in `Initialize()` unless the attribute cannot express it
- No mutable state stored on systems. Persistent data belongs on components
- Little LINQ on hot paths

Upstream edits (files outside `_DND` and `_CE`) get a wrapped comment:

```csharp
// Dnd: reason for the edit
... changed lines ...
// Dnd end
```

Do not put that comment on using lines.

## Prototypes

`Resources/Prototypes/_DND/`, 2-space YAML.

- Kind we declared (`dndAbility`, `dndClass`): plain id, `Strength`, `Fighter`, `Npc`
- Kind declared by the engine (`entity`, `job`, `Tag`, ...): prefix the id so it does not collide, if we ever have to declare one
- Check the kind name is not already an upstream prototype kind

## ECS

Components are data. Systems are logic.

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndExampleComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Value;
}
```

```csharp
public sealed partial class DndExampleSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnSpawn(PlayerSpawnCompleteEvent ev)
    {
    }
}
```
