# Systems Checklist

Status of every system needed for the vertical slice.

- [x] = exists and functional
- [~] = exists but incomplete or needs rework
- [ ] = missing

---

## Core Architecture

- [x] Singleton base (SingletonComponent<T>)
- [x] Scene management (SceneController)
- [x] Input system (InputManager, callback-based, new Input System)
- [x] Master data system (ScriptableObject + Addressables)
- [x] Object pooling (ObjectPooler<T>, ObjectPoolManager, PoolableHandler)
- [~] Battle FSM (BattleFSM exists, only Init state stubbed, Battle and Result missing)

---

## Player

- [x] Character movement (ModelController, CharacterController, gravity, ground check)
- [x] Player controller (PlayerController, input callbacks, closest-enemy detection)
- [x] Prepared spell slots (dictionary on PlayerController)
- [ ] Player FSM (states: Idle, Moving, Casting, Dodging, HitStun, Dead)
- [ ] Dodge / evasion
- [ ] Cast animation integration
- [ ] Counter input window

---

## Spells & Abilities

- [x] Ability data (AbilityMasterData, AbilityInfo, AbilityExecutionContext)
- [x] Ability executor pattern (IAbilityExecutor, AbilityExecutorFactory)
- [x] Projectile ability executor (ProjectileAbilityExecutor)
- [x] Spell data (SpellMasterData)
- [~] Spell instance pattern (SpellInstance, SpellFactory exist; FireballSpellInstance empty)
- [ ] Spell system consolidated (SpellInstance and AbilityExecutor are parallel — pick one)
- [ ] Spell casting pipeline (cast time, animation, resource check, execution)
- [ ] Spell targeting system (target position, target object, area)
- [ ] All 18 Tier 1 spells implemented (6 elements × 3 categories)

---

## Projectiles & Areas

- [x] Projectile base (ProjectileBase, velocity, lifetime, collision, team ID)
- [x] Fireball projectile (inherits ProjectileBase)
- [x] DOT area base (DOTAreaBase, periodic damage, HashSet tracking)
- [x] Projectile manager (pool-based spawning)
- [ ] Homing projectile
- [ ] Reflect / redirect projectile (needed for counter outcomes)
- [ ] Elemental projectile variants (Water, Wind, Earth, Light, Dark)

---

## Combat & Damage

- [x] Damage info (DamageInfo, DamageType)
- [x] Damage receiver (DamageReceiver, local + global events, team ID)
- [x] Damage indicators (DamageIndicatorManager, world-to-UI)
- [x] Health management (GameCharaController)
- [x] Death handling (death event, ragdoll fallback)
- [ ] Counter mechanic (counterable window on incoming attacks, resolution logic)
- [ ] Counter outcome system (extinguish, reflect, vulnerable state, beneficial interaction)
- [ ] Elemental opposition resolution (Fire vs Water interaction, etc.)

---

## Status Effects

- [ ] Status effect base class
- [ ] Status handler component (apply, stack, tick, remove)
- [ ] Burning
- [ ] Wet
- [ ] Frozen (requires Wet + Cold interaction)
- [ ] Charged
- [ ] Corrupted
- [ ] Revealed
- [ ] Status interaction resolution (e.g. Wet + Fire = Steam, Wet + Cold = Frozen)

---

## Enemy

- [x] Enemy controller (EnemyController, Idle/Follow/Attack states inline)
- [x] Projectile attack component (ProjectileAttackComponent, delay, cooldown, recoil)
- [~] Enemy FSM (manual state machine in EnemyController, not using ToraLib FSM)
- [ ] EnemyBaseAgent (stub only)
- [ ] Enemy archetypes (at least 3 distinct designs for vertical slice)
- [ ] Enemy telegraph system (signal incoming attack type before it fires)
- [ ] Enemy counterable attack flag
- [ ] Elemental enemy identity (which element an enemy uses/is vulnerable to)

---

## Resource System

- [ ] Mana (BaseManaCost exists in SpellMasterData, nothing tracks it at runtime)
- [ ] Mana regeneration
- [ ] Mana display (UI)

---

## Effects & Feedback

- [x] Effect manager (pool-based)
- [x] Explosion effect (auto-returns to pool after particle duration)
- [ ] Hit effect (per element)
- [ ] Counter success effect (visual + audio feedback)
- [ ] Status apply / tick effect
- [ ] Screen shake
- [ ] Hit stop (brief freeze on impactful hits)

---

## UI

- [x] UI manager (UIManager)
- [x] Damage numbers (DamageIndicatorManager)
- [ ] Health bar (player)
- [ ] Health bar (enemy, world-space)
- [ ] Mana bar
- [ ] Prepared spell display (show 3 equipped spells)
- [ ] Active status effect display
- [ ] Counter prompt / indicator
- [ ] Battle result screen

---

## Camera

- [x] Follow camera (FollowCamera, referenced in BattleController)
- [ ] Lock-on system
- [ ] Camera shake

---

## Roguelite Layer

- [ ] Run structure (room progression or arena waves)
- [ ] Death / run end handling
- [ ] Spell acquisition during run
- [ ] Meta-progression (what persists between runs)
- [ ] Spell pool / draft system

---

## Audio

- [ ] Audio manager
- [ ] Spell cast sounds (per element)
- [ ] Hit sounds
- [ ] Counter success sound
- [ ] Ambient / music system

---

## Debug & Tools

- [x] Debug scripts present
- [ ] In-game stat viewer
- [ ] Spell test harness (spawn spells in isolation to test interactions)
