# Damage & Combat System

## Goal

Implement a modular combat framework for Unity.

The system must support:

* Melee attacks
* Projectiles
* Explosions
* Area of effect attacks
* Future multiplayer
* Object pooling
* Combat effects
* Future combat mechanics such as status effects, shields, armor, counters, and elemental interactions.

The system should be component-based and avoid tightly coupling gameplay systems together.

---

# Architecture

Combat consists of three primary gameplay components.

```
DamageDealer
        │
        ▼
DamageReceiver
        │
        ▼
Unit
```

Presentation is handled separately.

```
Combat Events
        │
        ├── EffectService
        ├── Audio
        ├── Damage Numbers
        ├── Camera Shake
        └── Hit Stop
```

Gameplay should never directly spawn visual effects.

---

# Components

## Unit

Represents an entity that participates in combat.

Responsibilities:

* Team ownership
* HP and mana
* Cast state
* Receive incoming damage
* Coordinate combat systems

HP and mana live directly on Unit as fields — they are intrinsic to the entity and do not warrant separate components. Adding a resource (e.g. stamina) means adding fields to Unit, not adding a new component.

A Unit is the root combat object. See `Unit.md` for full design.

---

## DamageReceiver

Attached to colliders that can receive damage.

Responsibilities:

* Hold a reference to the owning Unit
* Optionally identify hit location

Examples:

* Body
* Head
* Weak Point

DamageReceiver should not contain gameplay logic.

It simply forwards damage to its Unit.

---

## DamageDealer

Represents something capable of dealing damage.

Examples:

* Sword hitbox
* Projectile
* Explosion
* Beam
* Trap
* Poison cloud

Responsibilities:

* Receive collision information
* Filter valid targets
* Prevent duplicate hits if configured
* Construct DamageInfo
* Deliver damage to the target Unit

DamageDealer should not contain movement logic.

DamageDealer should expose methods such as BeginAttack() and EndAttack() rather than relying on enabling/disabling the component directly.

---

## DamageInfo

Immutable struct describing a combat hit.

Should contain:

* Damage amount
* Damage type
* Element
* Source Unit
* Source Team ID
* Hit Position
* Hit Normal
* Knockback Direction
* Knockback Force
* Critical Hit

The target is implicit — DamageInfo is always delivered directly to the target's DamageReceiver, so there is no need to carry a target reference inside the struct.

---

# Team System

Each Unit owns an integer TeamId.

Examples:

Player = 0

Enemy = 1

Future multiplayer and multiple factions should be supported without changing the API.

DamageDealer should ignore friendly targets unless configured otherwise.

---

# Collision

DamageDealer should work with Unity colliders.

Collision detection is intentionally separated from movement.

Examples:

ProjectileMovement detects collision and calls DamageDealer.

Sword animation enables the attack window and forwards collisions to DamageDealer.

Explosion performs an overlap query and forwards each result to DamageDealer.

DamageDealer should not care how a collision was detected.

---

# Projectiles

Projectile is not a DamageDealer.

Projectile is an object composed of components.

Example:

Projectile

* ProjectileMovement
* DamageDealer
* Lifetime
* Visuals

DamageDealer should therefore be reusable by melee weapons, explosions, beams, hazards, and projectiles.

---

# Combat Events

Gameplay should be separated from presentation.

Successful hits should generate combat events.

Systems may subscribe independently.

Examples:

* Effects
* Audio
* Damage numbers
* Camera shake
* Hit stop

Gameplay should function correctly even if all presentation systems are disabled.

---

# EffectService

Visual effects should be spawned through a dedicated service.

Responsibilities:

* Spawn pooled effects
* Spawn attached effects
* Spawn decals
* Spawn damage numbers if desired

Gameplay code should never communicate directly with object pools.

EffectService should own pooled effect creation.

---

# Object Pooling

Projectiles and visual effects should support object pooling.

Gameplay systems should not know whether an object came from Instantiate() or a pool.

Pooling implementation should remain hidden behind services.

---

# Performance

Avoid allocations during gameplay.

Avoid hierarchy traversal during hit detection.

DamageReceiver caches references to its owning Unit.

Support hundreds of simultaneous combat objects.

---

# Extensibility

The architecture should easily support:

* Shields
* Armor
* Status effects
* Damage over time
* Elemental interactions
* Weak points
* Boss hit regions
* Multiplayer
* Network authority
* Additional damage types

The architecture should not require rewriting DamageDealer or DamageReceiver to support these systems.

---

# Code Style

Use MonoBehaviour components.

Use composition instead of inheritance.

Use namespaces.

Use XML documentation.

Always use braces on control statements.

Keep each component focused on a single responsibility.

Avoid singleton dependencies inside gameplay systems.
