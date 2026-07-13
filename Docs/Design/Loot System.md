# Loot System Design

## Purpose

The Loot System is responsible for determining which pickups are spawned when a world object is destroyed.

The system is completely data-driven, allowing enemies, chests, breakable objects, bosses, and future content to share the same loot generation logic without requiring custom code.

The Loot System determines **what** should drop. A separate Pickup Spawner is responsible for **how** those pickups appear in the world.

---

# Goals

* Fully data-driven
* Reusable across all destructible objects
* Supports guaranteed and random drops
* Supports weighted loot pools
* Supports nested loot tables
* Easy to balance without code changes
* Extensible for future Luck/Difficulty modifiers

---

# Architecture

```
Unit
 ├── Health
 ├── DamageReceiver
 ├── LootDropper
 │     └── LootTable
 └── ...
```

When a Unit dies:

```
Unit Dies
      │
      ▼
LootDropper
      │
      ▼
LootTable.Roll(context)
      │
      ▼
List<PickupSpawnRequest>
      │
      ▼
PickupSpawner
      │
      ▼
Spawn Pickups
```

The Loot System never instantiates GameObjects directly. It only generates spawn requests.

---

# Components

## LootDropper

Component attached to any object capable of dropping loot.

Examples:

* Enemy
* Boss
* Treasure Chest
* Breakable Crate
* Ore Node
* Barrel
* Destructible Environment

### Responsibilities

* Listen for owner's death/destruction
* Execute assigned LootTable
* Pass LootContext into the roll
* Send generated PickupSpawnRequests to the PickupSpawner

---

## LootTable

ScriptableObject containing a collection of LootEntries.

A LootTable describes every possible reward that can be generated.

LootTables are reusable assets and may reference other LootTables.

Example:

```
Common Enemy Loot

• XP Orb
• Mana Orb
• Coin
```

Bosses can simply include the Common Enemy Loot table rather than duplicating entries.

---

## LootEntry

Represents a single entry inside a LootTable.

A LootEntry may reference either:

* A PickupEffect (Kind + Amount, inline - not an asset)
* Another LootTable

The amount lives on the entry itself, not on a prefab or asset. PickupSpawner picks which prefab to spawn from the effect's Kind, so the same prefab (e.g. "XP Orb") is reused for every amount - a Slime's small XP drop and a Boss's large one are two LootEntries with different Amounts, not two prefabs.

This enables hierarchical loot generation.

---

# Loot Entry Types

## Guaranteed

Always generated.

Example:

```
XP Orb x3
```

---

## Chance

Generated only if a probability check succeeds.

Example:

```
Health Orb

Chance:
20%
```

---

## Weighted Pool

A group where only one entry is selected.

Example:

```
Rare Chest

Sword     Weight 50
Staff     Weight 30
Ring      Weight 15
Relic     Weight 5
```

Higher weights increase selection probability.

Weights do not need to total 100.

---

## Nested Loot Table

Instead of spawning a Pickup, execute another LootTable.

Example:

```
Boss Loot

Guaranteed

Roll Common Enemy Loot

Guaranteed

Roll Rare Boss Loot

5%

Roll Legendary Loot
```

This removes duplication and encourages reusable content.

---

# Example Loot Tables

## Slime

```
Guaranteed

XP Orb x1

20%

Mana Orb

5%

Health Orb
```

---

## Goblin

```
Guaranteed

XP Orb x2

40%

Coin

5%

Rare Essence
```

---

## Boss

```
Guaranteed

XP Orb x50

Guaranteed

Roll Common Loot

Guaranteed

Roll Boss Loot

5%

Roll Legendary Loot
```

---

# Pickup Spawn Request

Loot generation produces spawn requests instead of GameObjects.

```
PickupSpawnRequest

PickupEffect Effect

int Quantity

Vector3 Position
```

`Effect` (Kind + Amount) is inline data, not an asset reference - the amount is decided per LootEntry, not baked into a prefab. `LootTable.Roll` fills in `Position` from the `LootContext.Victim`'s location before the request is handed to the `PickupSpawner`. This is the single canonical definition of `PickupSpawnRequest`, shared with the Pickup System doc.

The PickupSpawner determines:

* Which prefab represents Effect.Kind
* Spawn spread around the given position
* Spawn animation
* Object pooling
* Magnet behavior
* Network synchronization (future)

---

# Loot Context

Every loot roll receives contextual information.

```
LootContext

Unit Killer

Unit Victim

float Luck

float Difficulty

int Wave
```

The initial implementation may ignore these values, but they provide a stable API for future systems.

Potential future uses include:

* Luck stat increases rare drop chance
* Difficulty scaling
* Elite enemy bonuses
* Character-specific loot modifiers
* Seasonal or event bonuses

---

# Design Principles

The Loot System follows a strict separation of responsibilities.

**LootTable**

Determines what rewards should be generated.

**LootDropper**

Triggers loot generation when an object is destroyed.

**PickupSpawner**

Creates the pickups in the world.

**Pickup**

Handles player interaction after spawning.

Each component owns a single responsibility, making the system modular, reusable, and easy to extend.

---

# Future Extensions

Possible future additions include:

* Quantity ranges
* Multiple rolls per LootTable
* Conditional entries
* Biome-specific drops
* Difficulty modifiers
* Luck modifiers
* Time-limited event drops
* Per-player loot
* Drop streak protection
* Loot filters
* Currency bundles
* Guaranteed drops after N kills
* Scripted boss rewards

These features can be added without changing the overall architecture.
