# Coding Conventions

Conventions for Unity projects built on ToraLib. Written from the Magus codebase, but project-agnostic: replace `<project>` with the project's root namespace (Magus uses `magus`).

## Project Layout

```
Assets/
  ToraLib/                  shared, project-agnostic library (git submodule)
    Scripts/<System>/       namespace tora.<system>
  <Project>/                everything specific to this game
    Scripts/                all C# code
    Addressables/           assets loaded at runtime by address
    Data/                   assets referenced directly (ScriptableObjects, signals)
    Prefabs/                prefabs referenced directly
    Scenes/
    Materials/ Models/ Shaders/ Fonts/ Terrain/ UI/
    Settings/ URPAssets/
    Localization/
    BuildScripts/           shell/batch build entry points
```

### ToraLib or project?

Before creating a system, decide where it belongs:

- **ToraLib** (`ToraLib/Scripts/<System>/`, `tora.<system>`) for generic infrastructure that knows nothing about the game, such as `EventBus`, `SaveSystem`, `SingletonComponent<T>`, FSM base classes, the graph framework and the asset-bundle utilities.
- **Project** (`<Project>/Scripts/<System>/`, `<project>.<system>`) for anything with game rules or content in it.

Dependencies only go one way: project code references ToraLib, never the other way around. When a generic framework needs game-specific types (for example story nodes on top of the graph framework), the framework stays in ToraLib and the concrete types live in the project.

## Namespaces

- **Lowercase, dot-separated:** `<project>.<system>`, for example `magus.battle`, `magus.story`, `magus.ui`, `tora.eventbus`.
- **One namespace per top-level system folder** under `Scripts/`:

  | Folder | Namespace |
  |---|---|
  | `Scripts/Battle/` | `<project>.battle` |
  | `Scripts/Story/` | `<project>.story` |
  | `Scripts/Master/` | `<project>.master` |
  | `Scripts/UI/` | `<project>.ui` |

- **Subfolders do not add a namespace segment.** They organize files, not APIs. `Battle/Modifiers/`, `Battle/Loot/`, `Story/Actions/`, `Story/Events/` and `UI/Battle/` all stay in their parent's namespace.
- **Editor code is the exception:** `<System>/Editor/` uses `<project>.<system>.editor`. That keeps editor-only types out of runtime IntelliSense and makes the runtime/editor boundary visible in `using` lists.
- **Every file declares a namespace.** No types in the global namespace.
- **The namespace follows the folder, not the consumer.** A UI component that lives under `Battle/` is still `<project>.battle`. If it really belongs to the UI, move it to `UI/`.
- The bare `<project>` namespace is only for a few truly cross-cutting types at the root of `Scripts/`.

### Cross-system references

If a publisher would need a `using` for a namespace it has no other reason to depend on (for example combat reporting a boss kill to the story system), publish an event through `EventBus` instead of adding the reference.

## Where Scripts Go

| What | Where |
|---|---|
| A system's runtime code | `Scripts/<System>/` |
| Sub-areas of a large system | `Scripts/<System>/<Area>/`, same namespace |
| Editor windows, drawers, inspectors | `Scripts/<System>/Editor/`, `<project>.<system>.editor` |
| Event message types | `Scripts/<System>/Events/`, next to the system that **publishes** them |
| Master data definitions (`BaseMasterData` subclasses) | `Scripts/Master/`, `<project>.master` |
| UI views and presenters | `Scripts/UI/<Feature>/`, `<project>.ui` |
| Game/UI state-machine states | `Scripts/<System>/States/` |
| Project-wide editor tooling (build menu, etc.) | `Scripts/Editor/` |
| Debug-only helpers | `Scripts/Debug/` |

One public type per file, with the file named after the type.

## Where Assets Go

- **`Addressables/`** for anything loaded by address at runtime, grouped by kind: `Addressables/Cutscenes/`, `Dialogue/`, `MasterData/`, `Prefabs/<Category>/` (e.g. `Maps`, `Units`) and `UI/`. If code loads it by string key, it lives here.
- **`Data/`** for ScriptableObjects and other data assets referenced directly from scenes or prefabs (`Data/Loot/`, `Data/Signals/`, `Data/Story/`).
- **`Prefabs/<Category>/`** for prefabs referenced directly, not loaded by address.

### Asset naming

Lowercase `snake_case`, with type, then id or category, then a zero-padded variant number:

- `map_test_02`
- `npc_test_01`
- `npc_0001_dia_01` (dialogue 01 for NPC 0001)

## Code Style

### Naming

| Element | Style | Example |
|---|---|---|
| Types, methods, properties, events | PascalCase | `DialogueManager`, `TryAdvance`, `IsPlaying`, `EntryChanged` |
| Interfaces | `I` + PascalCase | `ISaveParticipant`, `ISpellExecutor` |
| Private fields | `_camelCase` | `_runner`, `_isPaused` |
| Public serialized data fields (master data, plain data classes) | PascalCase | `BaseDamage`, `IconAssetKey` |
| Static readonly fields | PascalCase | `VariableTokenRegex` |
| Event handlers | `On` + event | `OnEntryChanged`, `OnConversationRequested` |
| Events | Past tense or noun phrase, no `On` prefix | `Ended`, `ChoiceSelected`, `AdvanceRequested` |
| EventBus message types | `<Thing>Event` | `BossKilledEvent`, `ConversationRequestedEvent` |
| Master data types | `<Thing>MasterData` | `SpellMasterData` |

### Formatting

- Allman braces (opening brace on its own line), 4-space indentation.
- Braces around every multi-line body. One-line guard clauses can drop them: `if (_lootTable == null) return;`.
- Explicit `private`.
- `using` order: `System.*`, then `UnityEngine.*`, then third-party (`Cysharp.*`), then `tora.*`, then `<project>.*`.

### Comments

- Use `/// <summary>` on types and non-obvious members, and explain **why**, not what: the constraint, the ordering dependency, the convention being matched.
- Put short trailing `//` comments on data fields when their meaning isn't obvious from the name (`// Addressables key for the spell's UI icon sprite`).

### Patterns

- **Managers** are `SingletonComponent<T>` (from ToraLib), e.g. `GameManager`, `InputManager`, `UIManager`, `StoryManager`.
- **Lifecycle:** subscribe and register in `Awake`, take references to other singletons in `Start` (their `Awake` may not have run yet), and unsubscribe/unregister in `OnDestroy` in reverse order. In `OnDestroy`, use cached references instead of `X.Instance`, which can re-create the singleton during teardown.
- **Views are dumb:** managers call setters on the view and subscribe to its events. Views hold no game logic.
- **Ids are strings**, unique within their table or graph, not `System.Guid`.
- **Content is loaded by Addressables path:** `Play(address)` / `Init(options)` take an address, not a preloaded asset. This also gives save data a stable key.
- **Polymorphic serialized lists** use `[SerializeReference]`, but only when a custom editor already draws the list. Otherwise, prefer one concrete class with a `Kind` enum so Unity's default inspector works.
- **Async** uses UniTask, not coroutines.
- **Saving:** systems with persistent state implement `ISaveParticipant` and register with `SaveSystem`.

## Known Deviations in Magus

These existed when the doc was written and should be fixed when the files are next touched, not copied:

- `Scripts/Battle/Spells/Executors/` uses `magus.battle.spells.executors` (should be `magus.battle`).
- One file in `Scripts/Battle/DamageIndicator/` uses `magus.ui`. Move it to `UI/` or change it to `magus.battle`.
- `Scripts/Editor/` mixes `magus.build` and `magus.editor`. Pick `magus.editor`.
- `Scripts/System/` and `Scripts/Weapons/` use the bare `magus` namespace.
- No namespace: `AI/EnemyBaseAgent.cs`, `Battle/ProjectileAutospawner.cs`, `Battle/Spells/SpellInstance.cs`, `FireballSpellInstance.cs`, `SpellFactory.cs`, `Battle/Spells/Executors/FireballSpellExecutor.cs`, `Chara/CharaAnimationListener.cs`, `UI/UIPopupManager.cs`, `Weapons/BasicProjectile.cs`. Several are legacy spell code slated for replacement (see `Design/SpellArchitecture.md`).
- `Prefabs/New Animation.anim` is a stray asset in a prefab folder.
