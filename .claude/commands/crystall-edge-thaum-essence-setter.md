# crystall-edge-thaum-essence-setter: Placing CEMagicEssenceStructure on CE Entities

Use when adding `CEMagicEssenceStructure` (thaumaturgical essence) to `_CE` entity prototypes, or fixing `CEMagicEssenceStructureTest` failures.

## Facts

- YAML type: `CEMagicEssenceStructure` — **no** `Component` suffix (that throws `UnknownComponentException`).
- System: `Content.Shared/_CE/MagicEssence/Systems/CEMagicEssenceSystem.cs`. Rolls once per prototype ID per round (shared by all instances), multiplies by `Stack` count, sums up contained items automatically. A second, independent path adds essence from solution reagents that are an essence's liquid embodiment (`CEEssenceWater` etc.) — only when actually present in a solution.
- Tests: `Content.IntegrationTests/Tests/_CE/CEMagicEssenceStructureTest.cs`.

## Two examples that cover 90% of cases

**1. Attach to the shared abstract base, not every child:**

```yaml
- type: entity
  id: CEClothingHeadBase
  abstract: true
  components:
  - type: CEMagicEssenceStructure
    essences:
      Cloth: { min: 1, max: 3 }
```
Every hat/cap/beret/fedora that `parent: CEClothingHeadBase` now has essence — zero extra edits. Only override on a child when its material really differs (a component block on a child **replaces** the parent's, it doesn't merge):

```yaml
- type: entity
  id: CEClothingHeadHelmetGuard
  parent: CEClothingHeadBase
  components:
  - type: CEMagicEssenceStructure
    essences:
      Metal: { min: 1, max: 3 }   # helmets are metal, not cloth — full override
```

**2. Container vs. contents — essence the object, not what might fill it. Empty vessels also get Void:**

```yaml
# CEDrinkBottleBase spawns EMPTY.
- type: CEMagicEssenceStructure
  essences:
    Crystal: { min: 1, max: 4 }   # the glass itself
    Void: { min: 0, max: 2 }      # emptiness — fits any container that spawns empty
# Water essence appears automatically, only once actually poured in, via the
# separate reagent system. Don't hand-add liquid essence to a container.
```

**3. The one bug that bit repeatedly — every block needs a guaranteed entry:**

```yaml
# WRONG: both can roll 0 → entity can spawn with zero essence, fails the test
essences:
  Order: { min: 0, max: 2 }
  Plant: { min: 0, max: 1 }

# RIGHT: one entry guarantees at least something
essences:
  Order: { min: 1, max: 2 }
  Plant: { min: 0, max: 1 }
```

Stack items (bars, sheets, coins...): put essence on the `count: 1` prototype only, the stack multiplies it — don't repeat on the 5/10/30 children.

## Scope

Skip: abstract-only helper mixins with no thematic identity, the essence-orb entities themselves (`Specific/Thaumaturgy/essence.yml`), and any `CE*` prototype that isn't actually under `Resources/Prototypes/_CE/` (e.g. `CEPDA`/`CEIDCard` are vanilla "Chief Engineer" items — coincidental prefix). Add false positives like that to the test's `IgnoredProto` set instead of essencing them.

## All essence types — tier, and what's been used for it so far

| ID (Name) | Tier | Used for |
|---|---|---|
| Earth (Terra) | 0 | stone, dirt, ore, minerals, bones-adjacent |
| Fire (Ignis) | 0 | flame, coal, forges, candles, torches |
| Water (Aqua) | 0 | filled liquid containers (via reagent, not hand-added), snowballs |
| Air (Aer) | 0 | wind, flight items, wind instruments |
| Order (Ordo) | 0 | bureaucracy, books, crafted precision, dice |
| Chaos (Perditio) | 0 | randomness, scrap/junk, alcohol, dice, monster meat |
| Frost (Gelum) | 1 | snow, ice |
| Light (Lux) | 1 | candles, lanterns, sunflowers |
| Motion (Motus) | 1 | vehicles, moving mechanisms |
| Cycle (Permutatio) | 1 | *unused — transformation/change themes* |
| Energia (Energia) | 1 | brass, power cells, batteries, magic-tech |
| Void (Vacuos) | 1 | empty containers/vessels (empty bottles, buckets) |
| Poison (Venenum) | 1 | garlic, onion, toxins |
| Life (Victus) | 1 | food, healing items, organic bodies |
| Crystal (Vitreus) | 1 | glass, gems, lenses |
| Weather (Tempestas) | 1 | *unused — weather control* |
| Magic (Praecantatio) | 2 | alchemy gear, magic items/staves |
| Beast (Bestia) | 2 | leather, fur, animal-humanoid races |
| Hunger (Fames) | 2 | *unused* |
| Plant (Herba) | 2 | vegetables, seeds, non-tree flora |
| Travel (Iter) | 2 | *unused* |
| Slime (Limus) | 2 | *unused* |
| Metal (Metallum) | 2 | iron/steel/generic metal items |
| Death (Mortuus) | 2 | bones |
| Healing (Sano) | 2 | *rarely used — Life covers most healing items* |
| Darkness (Tenebrae) | 2 | spooky/jack-o-lantern themes |
| Trap (Vinculum) | 2 | locks, keys, restraints/handcuffs |
| Flight (Volatus) | 2 | flying items, bird race, levitation |
| Eldritch (Alienis) | 3 | *unused* |
| Tree (Arbor) | 3 | wood, logs, forest race |
| Aura (Auram) | 3 | ambient/elven magic |
| Flesh (Corpus) | 3 | *unused* |
| Undead (Exanimis) | 3 | skeleton/undead-flavored items |
| Soul (Spiritus) | 3 | vampire items |
| Taint (Vitium) | 3 | demonic/corrupted themes (tiefling), mycelium |
| Mind (Cognitio) | 4 | scholarly/science books |
| Senses (Sensus) | 4 | detection/scanning gear (thaumaturgy glasses) |
| Human (Humanus) | 5 | human race, identity documents |
| Tool (Instrumentum) | 6 | generic hand tools |
| Crop (Messis) | 6 | seeds, harvestable produce |
| Greed (Lucrum) | 6 | coins, gold variants, wealth |
| Mining (Perfodio) | 6 | pickaxes, mining gear |
| Craft (Fabrico) | 7 | *unused* |
| Machine (Machina) | 7 | machine cores/parts |
| Cloth (Pannus) | 7 | fabric, clothing, bags |
| Weapon (Telum) | 7 | melee weapons, guns |
| Armor (Tutamen) | 7 | armor/outer protective clothing |
| Harvest (Meto) | 7 | *unused* |

Higher tier = rarer = smaller `max` (tier 0-1 items: max 3-8; tier 6-7: max 1-3). Most entities carry 1-3 essence types total.

## Verify

```powershell
dotnet test --filter "FullyQualifiedName~CEMagicEssenceStructureTest"
```
Failure messages list exact offending prototype IDs — `grep -rn "id: <ProtoId>" Resources/Prototypes/_CE` to trace them.
