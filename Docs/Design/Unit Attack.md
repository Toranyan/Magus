# Unit Attack

## Purpose

Defines how any Unit executes an attack — melee, projectile, AOE, or otherwise — through a shared interface. The controller (enemy AI, player input) decides when to attack. The attack component decides how.

---

## IUnitAttack

**Namespace:** `magus.battle`
**File:** `Scripts/Battle/IUnitAttack.cs`

```csharp
public interface IUnitAttack
{
    float Range    { get; }    // maximum range at which this attack can be used
    bool CanAttack { get; }    // false while on cooldown

    void SetOwner(Unit owner);            // called on setup so the attack knows its unit
    void Attack(Vector3 targetPosition);  // execute the attack toward a world position
}
```

Any MonoBehaviour that implements `IUnitAttack` can be dropped onto a character prefab and used by `EnemyController` or a future player melee system. The controller never references a concrete attack type.

---

## ProjectileAttackComponent

**File:** `Scripts/Chara/ProjectileAttackComponent.cs`

Fires a pooled projectile toward a world position after an optional windup delay.

Fields:

| Field | Purpose |
|---|---|
| `_attackRange` | Distance at which the attack is usable |
| `_attackCooldown` | Seconds between attacks |
| `_attackDelay` | Windup before the projectile spawns |
| `_initialSpeed` | Launch speed passed to ProjectileBase.Setup |
| `_lockY` | Flatten the direction vector — use for 2.5D or top-down games |
| `_projectileId` | Addressable key for the projectile prefab |
| `_spawnPosition` | Transform from which the projectile spawns |
| `_projectileManager` | Injected via SerializeField — no singleton access |

`Attack(Vector3 targetPosition)` is a no-op if `CanAttack` is false. The cooldown timer starts when the attack is initiated, not when the projectile spawns, so the delay is absorbed inside the cooldown window.

If the unit dies during the windup delay, the fire is cancelled.

---

## EnemyController

**File:** `Scripts/Chara/EnemyController.cs`

Drives enemy behaviour through a three-state machine.

```
Idle ──(target found)──► Chase ──(in range)──► Attack
 ▲                          │                     │
 └──────(target lost)───────┘◄───(out of range)───┘
```

### States

**Idle** — no target. Stands still, scans for targets each frame via `FindTarget()`.

**Chase** — has a valid target outside attack range. Moves toward the target each frame.

**Attack** — target is within attack range. Stops moving, calls `_attack.Attack(targetPosition)` each frame. The attack component's `CanAttack` guard prevents firing faster than the cooldown allows.

### Target tracking

`FindTarget()` does an `OverlapSphere` on the `Character` layer and returns the nearest live `Unit` on a different team. Targets are `Unit` references — not `GameCharaController`, not `Transform`.

A target becomes invalid when it is null or `!unit.IsAlive`. Both Chase and Attack check validity each frame and fall back to Idle immediately.

### Fields

| Field | Purpose |
|---|---|
| `_unit` | The enemy's own Unit — source of team, IsAlive, Killed |
| `_charaController` | Handles movement and death animation |
| `_detectRange` | Radius for target scanning — AI parameter, not a combat stat |
| `_attackBehaviour` | MonoBehaviour implementing IUnitAttack, assigned in inspector |

### Adding a second attack type

Assign a different MonoBehaviour to `_attackBehaviour` in the inspector. No code changes required. For enemies with multiple attack types (e.g. melee at close range, projectile at long range), expose multiple `IUnitAttack` slots and select between them in `UpdateAttack` based on distance or other conditions.

---

## Adding a new attack type

1. Create a MonoBehaviour that implements `IUnitAttack`
2. Implement `Range`, `CanAttack`, `SetOwner`, and `Attack`
3. Assign it to `_attackBehaviour` on the enemy prefab

Examples: `MeleeAttackComponent`, `AOEAttackComponent`, `BurstProjectileAttackComponent`.

---

## What was removed

| Removed | Reason |
|---|---|
| `IsRecoiling` on `ProjectileAttackComponent` | Movement lock is a controller concern, not an attack concern |
| `AttackNoTarget` / `AttackEmpty` | Simplified to a single `Attack(Vector3)` path |
| `GameCharaController` as attack target | Attacks target a world position — no dependency on character type |
| `BattleController.Instance` in attack component | `ProjectileManager` injected via SerializeField |
| `_detectRange` on `GameCharaController` | Detect range is an AI parameter — moved to `EnemyController` |
| Dead `Attack()` and `State` enum in old `EnemyController` | State machine now properly implemented |
