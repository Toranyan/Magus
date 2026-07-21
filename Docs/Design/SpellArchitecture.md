# Spell Architecture

This document defines the spell system architecture. The existing `Ability` system (AbilityMasterData, AbilityInfo, AbilityExecutorFactory, IAbilityExecutor) is superseded by this design. The spell system consolidates both into a single pipeline.

---

## Overview

A spell flows through four layers:

```
SpellMasterData         — static data, defined in ScriptableObject
    ↓
SpellInfo               — runtime copy, created from master data, owned by nobody
    ↓
UnitSpellInstance       — spell owned by a specific unit, tracks cooldown and level
    ↓
ISpellExecutor          — created on cast, executes one specific cast
```

---

## SpellMasterData

**Namespace:** `magus.master`  
**File:** `Scripts/Master/SpellMasterData.cs` *(exists)*

The raw definition of a spell. Loaded via Addressables. Never modified at runtime.

```csharp
public class SpellMasterData : BaseMasterData
{
    public string Name;
    public string Description;

    public SpellType SpellType;         // drives SpellExecutorFactory
    public ElementType Element;         // Fire, Water, Wind, Earth, Light, Dark
    public StrategicCategory Category;  // Offense, Defense, Support

    public float BaseCastTime;
    public float BaseManaCost;
    public float BaseCooldown;
    public float BaseDuration;
    public float BaseSpeed;
    public float BaseRange;
    public float BaseSize;
    public float BaseCount;

    public string[] AssetIds;           // prefab references (projectile, effect, etc.)

    public float[] Params;              // spell-specific extra parameters

    public string[] StatusEffectIds;    // StatusEffectMasterData ids applied by Buff/Debuff executor types
}
```

**Changes from current:** Add `Element`, `StrategicCategory`, `BaseCooldown`, `AssetIds` (replacing separate ability asset system). Remove `BaseManaCost` duplication once ability system is retired.

---

## SpellInfo

**Namespace:** `magus.battle`  
**File:** `Scripts/Battle/Spells/SpellInfo.cs` *(new)*

A runtime-safe copy of master data. Created from `SpellMasterData`. Exists independently of any unit — used for spell pool drafting, UI display, and constructing `UnitSpellInstance`.

```csharp
public class SpellInfo
{
    public string Id;
    public string Name;
    public SpellType SpellType;
    public ElementType Element;
    public StrategicCategory Category;

    public float CastTime;
    public float ManaCost;
    public float Cooldown;
    public float Duration;
    public float Speed;
    public float Range;
    public float Size;
    public float Count;

    public string[] AssetIds;
    public float[] Params;
    public string[] StatusEffectIds;

    public SpellInfo(SpellMasterData data) { ... }
}
```

**Replaces:** `AbilityInfo`. The old `AbilityInfo` mixed ownership (`Owner` field) into what should be stateless data — that is moved to `UnitSpellInstance`.

---

## UnitSpellInstance

**Namespace:** `magus.battle`  
**File:** `Scripts/Battle/Spells/UnitSpellInstance.cs` *(replaces SpellInstance)*

A spell equipped to a specific unit. Tracks per-unit runtime state: cooldown, current level, owner. This is what lives in the player's three prepared spell slots and in enemy loadouts.

```csharp
public class UnitSpellInstance
{
    public SpellInfo Info { get; }
    public IBattleEntity Owner { get; }

    public float CooldownRemaining { get; private set; }
    public bool IsReady => CooldownRemaining <= 0f;

    public UnitSpellInstance(SpellInfo info, IBattleEntity owner) { ... }

    public void Tick(float deltaTime) { ... }       // count down cooldown

    // Returns false if on cooldown or caster cannot cast
    public bool TryCast(SpellCastContext context)
    {
        if (!IsReady) return false;
        var executor = SpellExecutorFactory.Create(Info);
        executor.Execute(context);
        CooldownRemaining = Info.Cooldown;
        return true;
    }
}
```

**Replaces:** `SpellInstance` abstract class. The old design put `Cast()` directly on the instance, blurring the line between ownership and execution.

---

## SpellCastContext

**Namespace:** `magus.battle`  
**File:** `Scripts/Battle/Spells/SpellCastContext.cs` *(replaces AbilityExecutionContext)*

All parameters relevant to one specific cast. Passed into `ISpellExecutor.Execute()`.

```csharp
public class SpellCastContext
{
    public SpellInfo Info;

    public IBattleEntity Caster;
    public IBattleEntity Target;        // null if untargeted

    public Vector3 CastPosition;
    public Vector3 TargetPosition;

    // Extended at cast time — e.g. status effects on target, counter flag
    public bool IsCounter;
}
```

**Note:** `IsCounter` is here so executors can modify their behavior when the cast is a counter (e.g. reflect instead of just extinguish).

---

## ISpellExecutor

**Namespace:** `magus.battle`  
**File:** `Scripts/Battle/Spells/ISpellExecutor.cs` *(replaces IAbilityExecutor)*

Implemented by every concrete spell type. A new instance is created per cast by the factory.

```csharp
public interface ISpellExecutor
{
    void Execute(SpellCastContext context);
}
```

One executor class per `SpellType`. Executors are stateless — all cast-specific data comes from `SpellCastContext`.

---

## SpellExecutorFactory

**Namespace:** `magus.battle`  
**File:** `Scripts/Battle/Spells/SpellExecutorFactory.cs` *(replaces AbilityExecutorFactory and SpellFactory)*

Creates the correct executor based on `SpellType`.

```csharp
public static class SpellExecutorFactory
{
    private static readonly Dictionary<SpellType, Func<ISpellExecutor>> _map = new()
    {
        { SpellType.Fireball,  () => new FireballSpellExecutor()  },
        { SpellType.WaterJet,  () => new WaterJetSpellExecutor()  },
        // ... one entry per SpellType
    };

    public static ISpellExecutor Create(SpellInfo info)
    {
        if (_map.TryGetValue(info.SpellType, out var factory))
            return factory();

        Debug.LogError($"No executor registered for SpellType {info.SpellType}");
        return null;
    }
}
```

**Replaces:** Both `AbilityExecutorFactory` (switch on AbilityType) and `SpellFactory` (dictionary on SpellType). The two factories are unified here under spell type.

---

## FireballSpellExecutor

**Namespace:** `magus.battle`  
**File:** `Scripts/Battle/Spells/Executors/FireballSpellExecutor.cs` *(replaces FireballSpellInstance and ProjectileAbilityExecutor for fireball)*

Example executor. Pops a fireball projectile from the pool and sets it up.

```csharp
public class FireballSpellExecutor : ISpellExecutor
{
    public async void Execute(SpellCastContext context)
    {
        var proj = await BattleController.Instance.ProjectileManager
            .CreateProjectile(context.Info.AssetIds[0], context.Caster);

        proj.transform.SetParent(null);
        proj.transform.position = context.CastPosition;

        var dir = (context.TargetPosition - context.CastPosition);
        dir.y = 0f;
        proj.Setup(dir.normalized * context.Info.Speed);

        // If this cast is a counter, apply counter outcome
        if (context.IsCounter)
            proj.SetCounterFlag();
    }
}
```

The pattern for all future projectile-based spell executors. Non-projectile executors (barriers, buffs, DOT zones) follow the same interface but interact with different systems.

---

## SpellType Enum

**File:** `Scripts/Battle/Spells/SpellType.cs` *(exists, extend as spells are added)*

One entry per concrete spell. Drives the factory.

```csharp
public enum SpellType
{
    // Fire
    Fireball,
    FireWall,
    Ignite,

    // Water
    WaterJet,
    IceShield,
    Cleanse,

    // Wind
    // Earth
    // Light
    // Dark
    // ...
}
```

---

## What to Delete

Once this system is implemented:

| Old class | Replaced by |
|-----------|-------------|
| `AbilityMasterData` | `SpellMasterData` (add missing fields) |
| `AbilityInfo` | `SpellInfo` |
| `AbilityExecutionContext` | `SpellCastContext` |
| `IAbilityExecutor` | `ISpellExecutor` |
| `AbilityExecutorFactory` | `SpellExecutorFactory` |
| `ProjectileAbilityExecutor` | `FireballSpellExecutor` (and per-spell equivalents) |
| `SpellInstance` (abstract) | `UnitSpellInstance` |
| `FireballSpellInstance` | `FireballSpellExecutor` |
| `SpellFactory` | `SpellExecutorFactory` |

---

## Data Flow Example — Player casts Fireball

```
1. Player presses cast button
2. PlayerController calls UnitSpellInstance.TryCast(context)
3. UnitSpellInstance checks cooldown and mana
4. UnitSpellInstance calls SpellExecutorFactory.Create(Info) → FireballSpellExecutor
5. FireballSpellExecutor.Execute(context)
6. Projectile popped from pool, positioned, velocity set
7. ProjectileBase handles movement, collision, damage
8. On hit: DamageReceiver applies damage, status effects applied
9. UnitSpellInstance starts cooldown timer
```
