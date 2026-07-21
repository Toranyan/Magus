# Modifier System Design

## Purpose

A Modifier is the internal simulation primitive that answers **"what changes?"** — it alters a stat or value used by a spell or unit (speed, damage, cast speed, cooldown, projectile count, area of effect, etc.).

A Modifier has no player-facing representation of its own: no icon, no name, no tooltip, no duration. It is created and owned by something that *does* have that presentation — a Buff, a Debuff, a Passive Skill, or an Item. That owner answers **"why is it changing, and what does the player see?"** and is specified separately in [Buff and Debuff System](Buff%20and%20Debuff%20System.md).

This separation keeps the simulation math (this doc) independent from presentation and lifecycle (the other doc).

---

# Goals

* Fully data-driven
* Deterministic, order-independent-feeling resolution (same inputs always fold to the same output)
* No stack limits — every Modifier instance is independent
* Support both unit-wide (Global) and filtered (Tagged) application
* Support both additive and multiplicative stat changes in one formula
* No ownership logic baked into gameplay code — Modifiers are inert data, folded by a resolver
* Avoid back-referencing live objects across a snapshot boundary (e.g. a projectile should not hold a live pointer back to its caster's modifier list)

---

# Responsibilities

The Modifier System is responsible for:

* Defining the shape of a single Modifier (type, operation, value, tags, priority)
* Resolving a final stat value from a base value plus every applicable active Modifier
* Determining which Modifiers apply to a given resolution (Global vs Tagged matching)
* Resolving conflicts between mutually exclusive (Override) Modifiers

The Modifier System is **not** responsible for:

* Duration or expiry — that belongs to the owning Buff/Debuff/Skill/Item
* Removal triggers — Modifiers are removed only when their owner is removed
* Icons, names, descriptions, tooltips, or any player-facing display
* Deciding *when* a Buff/Debuff is granted or revoked

---

# Architecture

```text
Buff / Debuff / Passive Skill / Item   (owner — see Buff and Debuff System)
      │  creates & registers
      ▼
Modifier                                (this doc — inert data)
      │  added to
      ▼
ModifierCollection                      (attached to a Unit and/or a UnitSpellInstance)
      │  queried by
      ▼
Stat Resolution (SpellInfo / Unit stat getters)
```

A `ModifierCollection` can exist at more than one level — e.g. a Unit-level collection (affects everything the unit does) and, once a spell is cast, a snapshotted resolved value baked onto that one cast (see [Snapshotting](#snapshotting)).

---

# Components

## ModifierType

The stat or value being altered. Mirrors the `Base*` fields already on `SpellMasterData`/`SpellInfo`, plus unit-level stats, plus boolean state gates (see [Flag Modifiers](#flag-modifiers)):

* CastTime
* Cooldown
* Speed
* Range
* Size (area of effect)
* ProjectileCount
* Damage
* MoveSpeed
* Stunned, Silenced, Rooted, Disarmed, Invulnerable *(Flag-only types — see below)*
* *(extend as new alterable stats are identified)*

## ModifierOperation

How the Modifier's `Value` combines with the base value. See [Resolution Formula](#resolution-formula).

* `AddToBase` — flat value added before percent modifiers are applied
* `Percent` — percentage added to every other active `Percent` modifier, then applied once as a single multiplier
* `AddToFinal` — flat value added after the percent multiplier
* `Override` — replaces the resolved value outright; resolved via [Priority](#priority--override-resolution) when more than one competes
* `Flag` — boolean gate, not a value in the numeric pipeline; resolved via [Flag Modifiers](#flag-modifiers)

## ModifierTag

Used for Tagged (filtered) application. Work in progress — extend as needed. Currently:

* Fire, Water, Wind, Earth, Light, Dark *(element tags)*
* Projectile
* AoE
* Melee

## Modifier

The data record itself:

```text
Modifier
    ModifierType Type
    ModifierOperation Operation
    float Value
    ModifierTag[] Tags       // empty = Global, applies to every resolution
    int Priority             // only meaningful for Override
    long AcquiredAt          // timestamp, used as the Override tiebreak
    OwnerId Owner             // opaque reference used only for removal, never read during resolution
```

`Owner` exists solely so the Buff/Debuff/Skill/Item that created a Modifier can find and remove exactly the Modifiers it registered — it is not consulted by the resolution formula.

## ModifierCollection

Holds every active Modifier for one context (a Unit, or a specific spell instance). Provides:

* `Add(Modifier)`
* `RemoveAll(OwnerId)` — called when an owner is removed
* `Resolve(ModifierType type, float baseValue, ModifierTag[] contextTags) → float`
* `HasFlag(ModifierType type, ModifierTag[] contextTags) → bool`

---

# Resolution Formula

Every stat resolves through the same three-stage pipeline, regardless of `ModifierType`:

```text
1. intermediateBase = BaseValue + Σ(AddToBase modifiers)
2. afterPercent      = intermediateBase × (1 + Σ(Percent modifiers))
3. final             = afterPercent + Σ(AddToFinal modifiers)
```

`Override` modifiers bypass this pipeline entirely and replace `final` directly, subject to priority resolution below.

This ordering is the specification, not an implementation detail — any future engine change must preserve stage ordering (all AddToBase, then all Percent as one multiplier, then all AddToFinal, then Override last).

---

# Flag Modifiers

Some changes aren't a number at all — they're a boolean gate on whether something can happen: Stunned, Silenced, Rooted, Disarmed, Invulnerable. These use `ModifierOperation.Flag` and skip the [Resolution Formula](#resolution-formula) entirely:

```text
HasFlag(type, contextTags) = OR across every active Flag modifier
                              of that Type whose Tags match the context
```

`Value`, `Priority`, and `AcquiredAt` are unused for `Flag` modifiers — presence is binary, so stacking is naturally "is any instance active," not add/multiply. As with everything else in this system, a Flag modifier is only ever removed when its owning Buff/Debuff/Skill/Item is removed.

**Out of scope:** behavioral changes that alter *what a spell does* rather than gate an action or resolve a number — e.g. a projectile piercing through enemies, homing onto a target, splitting/bouncing on impact, or a spell applying a secondary effect on hit — are **not** Modifiers. They're spell-authoring concepts (parameters or hooks on the `ISpellExecutor`/projectile itself) and are out of scope for this doc. The line: if it can be expressed as "true/false, checked by the FSM or a gameplay gate," it's a Flag Modifier; if it changes the spell's own behavior/logic, it belongs to spell authoring, not this system.

---

# Application Scope — Global vs Tagged

* **Global** — a Modifier with no Tags applies to every resolution query regardless of context, e.g. a "+10% cast speed" buff on the caster affects all of that unit's spells.
* **Tagged** — a Modifier with one or more Tags only applies when the resolution context provides at least one matching tag (any-match, not all-match), e.g. a Modifier tagged `Fire` only affects resolutions for spells that are themselves tagged `Fire`.

Both kinds live in the same `ModifierCollection` and are folded by the same formula — Tagged is just a filter applied before folding.

---

# Priority & Override Resolution

Priority only matters when two or more `Override` modifiers target the same `ModifierType` and matching scope:

* Priority is a plain `int` — higher wins.
* Ties are broken by `AcquiredAt` — the most recently acquired Modifier wins ("last applied wins").

Priority has no effect on `AddToBase`/`Percent`/`AddToFinal` modifiers — those always fold together per the formula above regardless of order or priority.

---

# Stacking

There are no stack limits. Multiple Modifiers of the same `ModifierType`/`Operation`, from the same or different owners, are independent entries in the `ModifierCollection` and all fold into the resolution formula every time. There is no special-casing of "the same modifier twice" — the owner (Buff/Debuff) decides its own stacking behavior (refresh vs. stack) as covered in [Buff and Debuff System](Buff%20and%20Debuff%20System.md); the Modifier System itself just keeps folding whatever is currently registered.

---

# Snapshotting

Modifiers on a Unit are live and can change at any time (a Buff expires, a Debuff is applied). A spell cast, however, needs a stable set of values once it starts acting independently of its caster — e.g. a launched projectile.

**Heuristic:** snapshot the caster's resolved values at the point of *visual separation* — the moment a projectile launches, an effect detaches, or a cast otherwise stops being "the caster doing something" and starts being "an independent object in the world." At that point, resolve every relevant `ModifierType` against the caster's current `ModifierCollection` and bake the results into the spell/projectile's own data. After snapshotting, changes to the caster's Modifiers do not retroactively affect the already-launched instance.

This is decided case-by-case per spell/executor rather than mandated as one universal rule, but the default assumption is: **snapshot at launch, avoid back-referencing the caster.** A projectile should carry resolved floats, not a live reference to `Caster.ModifierCollection`. Spells that don't detach from the caster (channeled effects, auras) may instead re-resolve live each tick — that choice is documented per `ISpellExecutor` implementation.

---

# Removal

Modifiers are never removed directly by gameplay logic. The only removal path is: an owner (Buff, Debuff, Passive Skill, Item) is itself removed, and its removal calls `ModifierCollection.RemoveAll(ownerId)` for every collection it registered into. There is no Modifier-level duration or timer — see [Buff and Debuff System](Buff%20and%20Debuff%20System.md) for where that lives.

---

# Separation of Responsibilities

**Modifier** — what changes in the simulation, and how it resolves.

**Buff / Debuff / Passive Skill / Item** — why it's changing, when it's active, and what the player sees.

---

# Future Extensions

Potential future features include:

* Element-altering modifiers (deferred — the current single-valued `ElementType` on `SpellInfo` makes this nontrivial; revisit once multi-element spells are needed)
* Conditional modifiers (only active while some condition holds)
* Modifiers that alter other modifiers' potency
* Networked modifier replication

The architecture supports these additions without changing the core resolution formula.
