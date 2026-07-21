# Buff and Debuff System Design

## Purpose

A Buff or Debuff (collectively, a **Status Effect**) is the player-facing gameplay object responsible for granting and later revoking [Modifiers](Modifier%20System.md). Where a Modifier answers "what changes in the simulation," a Buff/Debuff answers **"why is it changing, and what does the player see?"**

Passive Skills and equipped Items are covered by this same doc — they follow the identical ownership contract (register Modifiers on acquire, deregister on removal) even though they are usually permanent rather than timed and may not always need full status-effect presentation.

This doc implements the "Status Effects" line item in [SystemsChecklist.md](SystemsChecklist.md). The elemental conditions listed there (Burning, Wet, Frozen, Charged, Corrupted, Revealed) are Status Effect definitions built on this system. How they interact with each other (e.g. Wet + Fire, Wet + Cold) is not yet decided and is intentionally left out of this doc.

---

# Goals

* Fully data-driven, with player-facing metadata (name, icon, description) separated from simulation logic
* Own the full lifecycle — apply, tick, expire, remove — of every Modifier a Status Effect grants
* Support both timed (Buff/Debuff) and permanent (Passive Skill, equipped Item) owners under one contract
* Per-definition stacking behavior (refresh / stack / ignore), independent from Modifier stacking
* Consistent player-facing presentation (icon, tooltip, stack count)

---

# Responsibilities

A Status Effect owner (Buff, Debuff, Passive Skill, Item) is responsible for:

* Owning one or more Modifier instances
* Defining its own Duration (or Permanent)
* Defining its own stacking rule
* Registering its Modifiers with the target's `ModifierCollection` on activation
* Deregistering exactly the Modifiers it registered, on expiry or removal
* Presenting Icon, Name, Description, and stack count to UI
* Driving any non-Modifier side effects it has (e.g. Burning's periodic damage tick)

A Status Effect owner is **not** responsible for:

* The stat resolution formula or operation semantics — see [Modifier System](Modifier%20System.md)
* Deciding which tags a Modifier matches against — authored on the Modifier itself

---

# Architecture

```text
Source (spell cast, item equip, skill learned)
      │
      ▼
StatusEffectInstance created from StatusEffectMasterData
      │
      ▼
Registers its Modifiers with target's ModifierCollection
      │
      ▼
Duration ticks (skipped if Permanent) / periodic side effects tick
      │
      ▼
Expiry or explicit removal (dispel, unequip, unlearn)
      │
      ▼
Deregisters its Modifiers from target's ModifierCollection
      │
      ▼
UI notified, instance destroyed
```

---

# Components

## StatusEffectMasterData / StatusEffectMasterDataUnity

Authored, static data — mirrors the `SpellMasterData`/`SpellMasterDataUnity` split. `StatusEffectMasterData` (in `magus.master`, extends `BaseMasterData`) holds the plain serialized fields; `StatusEffectMasterDataUnity` is the `MasterDataUnity<StatusEffectMasterData>` ScriptableObject wrapper loaded via Addressables. Never modified at runtime.

* Name, Description, Icon
* Category: Buff / Debuff
* Modifiers — one or more Modifier templates this definition grants
* Duration — `0`/negative means Permanent (Passive Skill, Item)
* TickInterval — for definitions with a periodic side effect (e.g. Burning's damage tick)
* StackRule — see [Stacking Rules](#stacking-rules)

## StatusEffectInstance (runtime)

* Reference to its `StatusEffectMasterData`
* Target
* Remaining Duration, Stack Count
* The concrete Modifier instances it actually registered (so removal is exact, not a re-derivation)

## StatusEffectController (per-Unit)

Owns every active `StatusEffectInstance` on a Unit. Responsible for:

* Applying new Status Effects, enforcing `StackRule` against existing instances of the same `StatusEffectMasterData`
* Ticking Duration and TickInterval
* Removing expired or dispelled instances
* Exposing the active list to UI

This corresponds to the `StatusController` extension point already noted in [Unit.md](Unit.md).

---

# Stacking Rules

This is the owner's own stacking behavior — distinct from Modifier stacking, which never has a limit. Each `StatusEffectMasterData` declares one:

* **Refresh** — reapplying resets Duration on the single active instance; its Modifiers are unchanged
* **Stack** — reapplying adds another independent instance (and therefore another independent Modifier, per the Modifier System's no-stack-limit rule); each instance tracks and expires on its own Duration
* **Stack (duration only)** — a single instance; reapplying extends its remaining Duration rather than adding a new instance
* **Ignore** — reapplying while already active has no effect

---

# Passive Skills & Items as Modifier Owners

Passive Skills and equipped Items follow the same ownership contract as Buffs/Debuffs — register Modifiers on acquire, deregister on removal — but are typically:

* Permanent-duration (no `TickInterval`, no expiry timer; removed only when the skill is unlearned or the item unequipped)
* Still presented to the player (a passive skill list, an equipped item panel) even without duration/stack-count UI

They do not need a `StatusEffectController` tick loop, only the acquire/remove registration calls.

---

# Lifecycle

1. **Acquire** — a buff-granting spell lands, an item is equipped, or a passive skill is learned.
2. `StatusEffectController` checks the target's existing instances of the same `StatusEffectMasterData` against its `StackRule`.
3. A new or updated `StatusEffectInstance` registers its Modifiers with the target's `ModifierCollection`.
4. **Tick** — Duration counts down; `TickInterval` side effects (e.g. Burning's damage) fire if defined.
5. **Expire or explicit removal** — Duration reaches zero, or a dispel/unequip/unlearn action fires.
6. The `StatusEffectInstance` deregisters exactly the Modifiers it registered.
7. UI is notified to remove the icon/stack display.

---

# Separation of Responsibilities

**Modifier System** — what changes in the simulation, and how it resolves. See [Modifier System](Modifier%20System.md).

**Buff / Debuff / Passive Skill / Item** — why it's changing, when it's active, what the player sees, and any side effects beyond stat changes.

---

# Future Extensions

Potential future features include:

* Elemental status interaction resolution (e.g. Wet + Fire, Wet + Cold) — not yet decided
* Dispel/cleanse categories (which Debuffs a given Cleanse effect can strip)
* Save/load persistence for long-duration effects
* Networked replication
* Aura-style Status Effects (owner is an area/zone rather than a unit)

The architecture supports these additions without changing the core apply/tick/remove lifecycle.
