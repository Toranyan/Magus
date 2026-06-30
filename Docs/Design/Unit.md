# Unit

## Purpose

`Unit` is the root combat object. It is the authoritative source of identity, HP, and mana for any entity that participates in combat — players, enemies, and future summoned entities.

All combat systems that need to know "who" something belongs to, or "how much life/mana it has", go through Unit.

---

## What Unit Is Not

Unit does not handle:

* Movement or animation — those live in `GameCharaController` and `ModelController`
* Collision detection — that lives in `DamageReceiver` and `DamageDealer`
* AI — that lives in `EnemyController`
* Input — that lives in `PlayerController`

Unit owns resources and identity. Everything else is a separate concern.

---

## Component

**Namespace:** `magus.battle`
**File:** `Scripts/Battle/Unit.cs`

```csharp
public class Unit : MonoBehaviour, IBattleEntity
{
    // --- Identity ---
    [SerializeField] int _teamId;

    // --- HP ---
    [SerializeField] float _maxHp;
    public float CurrentHp { get; private set; }
    public float MaxHp => _maxHp;
    public bool IsAlive => CurrentHp > 0f;

    // --- Mana ---
    [SerializeField] float _maxMana;
    public float CurrentMana { get; private set; }
    public float MaxMana => _maxMana;

    // --- Events ---
    public event Action Killed;
    public event Action<DamageInfo> DamageReceived;
    public event Action<float> ManaChanged;

    // --- Methods ---
    public void Setup();                        // reset HP and mana to max
    public void ReceiveDamage(DamageInfo info); // entry point for all incoming damage
    public void Heal(float amount);
    public bool TrySpendMana(float amount);     // returns false if insufficient
    public void RestoreMana(float amount);
}
```

---

## HP

`CurrentHp` is reduced by `ReceiveDamage`. When it reaches zero, `Killed` fires and `IsAlive` becomes false.

`ReceiveDamage` is the single entry point for all incoming damage. It is called by `DamageReceiver`, which sits on colliders and forwards hits here.

Rules:

* `ReceiveDamage` is a no-op when `IsAlive` is false — dead units cannot take additional damage.
* `Heal` is also a no-op when `IsAlive` is false — units cannot be healed back from death.
* HP is clamped between 0 and `_maxHp`.

Future: resistances, shields, and damage modifiers should be applied inside `ReceiveDamage` before reducing `CurrentHp`, using data from `DamageInfo` (type, element, etc.).

---

## Mana

`CurrentMana` is the resource consumed by spell casting.

`TrySpendMana(amount)` returns false if `CurrentMana < amount`. Callers (e.g. `UnitSpellInstance.TryCast`) should check the return value and abort the cast if mana is insufficient.

`RestoreMana(amount)` is used by regen ticks, pickups, and future mana-restoration effects.

Both methods fire `ManaChanged` with the new current value, so UI bars can subscribe directly without polling.

---

## Team System

Each Unit owns an integer `TeamId`.

```
Player  = 0
Enemy   = 1
```

`DamageDealer` reads the owner's `TeamId` and the target's `TeamId` to skip friendly-fire. Future multiplayer and multiple factions extend this without changing the API — add new integer values.

---

## Events

| Event | When it fires |
|---|---|
| `Killed` | HP reaches zero |
| `DamageReceived` | Any successful hit (fires even on the killing blow) |
| `ManaChanged` | Mana is spent or restored |

`GameCharaController` subscribes to `Killed` to start the death animation.
UI systems subscribe to `ManaChanged` for mana bar updates.
Future status effect systems subscribe to `DamageReceived` for proc logic.

---

## Setup in Prefabs

Add `Unit` as a component on the character root GameObject alongside `GameCharaController`.

Required wiring:

* `_teamId` — set per prefab (0 = player, 1 = enemy)
* `_maxHp` — max hit points
* `_maxMana` — max mana (set 0 for enemies that do not cast)
* `DamageReceiver._unit` — assign the Unit on this character so hits are routed here
* `GameCharaController._unit` — assign the Unit so the controller can subscribe to `Killed`

---

## Future Extensions

The following systems should extend Unit without modifying its core HP/mana logic:

* **Status effects** — a `StatusController` component subscribes to `DamageReceived` and maintains a list of active effects. Unit does not need to know about individual statuses.
* **Shields / damage absorption** — applied inside `ReceiveDamage` before reducing `CurrentHp`, reading `DamageInfo.Type` and `DamageInfo.Element`.
* **Armor / resistances** — a multiplier applied to `info.Amount` in `ReceiveDamage`, sourced from a future `Stats` component.
* **Mana-on-hit** — subscribe to `DamageReceived` and call `RestoreMana`.
