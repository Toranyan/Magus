# StorySystem

> Version 1.0

## Dependencies
Build order — steps 1-4 are implemented:

1. [EventBus](EventBus.md) — gameplay → StoryManager, StoryManager → presentation systems
2. [GraphFramework](GraphFramework.md) — shared node/graph base that StoryGraph is built on (MVP editor scope only)
3. [SaveSystem](SaveSystem.md) — StoryManager is its first participant
4. StorySystem (this doc) — implemented; see [Core Components](#core-components) for what's live vs. deferred pending step 5
5. [DialogueSystem](DialogueSystem.md) — implemented (v1 entry set/runtime/UI; not graph-based — see its Dialogue Data section). Both directions of the link between the two systems are done: `StartDialogueAction` → `ConversationRequestedEvent` → `DialogueManager` requests a conversation (see [Actions](#actions)/[Narrative Events](#narrative-events)); `DialogueChoiceMadeEvent` → `StoryManager` records the player's answer into `StoryBlackboard` (see [Blackboard](#blackboard)). `SeenConversations` save data still isn't wired, since nothing reports conversation *completion* back yet

## Purpose
The Story System determines **what narrative content becomes available**. It is data-driven and presentation-agnostic.

## Responsibilities
- Track StoryState
- Evaluate StoryGraph
- Respond to gameplay events
- Execute StoryActions
- Publish NarrativeEvents
- Save/Load progress

## Does NOT
- Show dialogue
- Play timelines
- Control UI

## Architecture
```text
Gameplay -> EventBus -> StoryManager -> StoryGraph
                          ^    |
    DialogueChoiceMadeEvent    v
                        NarrativeEventBus
                  /      |       \      \
           Dialogue  Cutscene  Popups  GameManager (game flow, e.g. BattleStartRequestedEvent)
        (DialogueManager) (CutsceneManager)
```

"Timeline" from earlier drafts is `CutsceneManager` (`Magus/Scripts/Cutscene/`) — a thin trigger around Unity's own Timeline package (already installed), not a custom sequencer. See [Actions](#actions)/[Narrative Events](#narrative-events). "Popups" still has no consumer/action — nothing requests one yet.

## Core Components

### StoryManager
Owns StoryState, evaluates graph, publishes NarrativeEvents. Implemented as `StoryManager : SingletonComponent<StoryManager>` (`ToraLib/Scripts/Singleton/SingletonComponent.cs`), matching every other top-level manager in the project (`GameManager`, `InputManager`, `UIManager`, `BattleController`). See [Design Principles](#design-principles) — this supersedes the "no singletons" phrasing in earlier drafts of this doc.

Public API:
```csharp
Initialize();
Shutdown();
RaiseEvent(StoryEvent e);
Save();
Load();
ResetProgress();
```

### StoryGraph
Directed graph of StoryNodes.

### StoryNode
`StoryNode : GraphNode` (see [GraphFramework](GraphFramework.md)). Fields:
- Id (string) — inherited from `GraphNode`. Matches the project's existing Master Data Id convention (see `Magus/Scripts/Master/`) rather than a `System.Guid` — earlier drafts of this doc specified Guid; superseded.
- Name
- Conditions
- Actions
- ChildIds — inherited from `GraphNode`
- Priority
- Tags

### Conditions
Implement `IStoryCondition`. Stored as `[SerializeReference] List<IStoryCondition>` on `StoryNode` (see [GraphFramework — Polymorphic Fields](GraphFramework.md#polymorphic-fields-conditions--actions)).

- Implemented: `StoryFlagCondition`, `VariableCondition`
- Not implemented — each depends on a system that doesn't exist yet: `Item` (inventory), `BossKilled` (combat doesn't publish a kill event on EventBus yet), `ConversationSeen` (DialogueSystem)

### Actions
Implement `IStoryAction`. Stored as `[SerializeReference] List<IStoryAction>` on `StoryNode`, same mechanism as Conditions.

- Implemented: `SetFlagAction`, `SetVariableAction`, `LogMessageAction` (debug/POC only — prints a message to the console, not part of the original design), `StartBattleAction` (publishes `BattleStartRequestedEvent` with a map/player Addressables path, plus an optional `SpawnPointId` — see below), `StartDialogueAction` (publishes `ConversationRequestedEvent` with a `DialogueAsset` Addressables path), `StartTimelineAction` (publishes `TimelineRequestedEvent` with a cutscene prefab's Addressables path), `CheckpointAction` (publishes `CheckpointReachedEvent` with a `CheckpointId` — see [Narrative Events](#narrative-events)). `StartDialogueAction`/`StartTimelineAction` supersede the originally-planned `UnlockConversation`-style naming/shape — they directly request playback rather than just marking something available for some other system to trigger later, all mirroring the same pattern
- `StartBattleAction.SpawnPointId` — lets different StoryNodes land the player at different points on the *same* map (e.g. arriving at a village gate the first time vs. waking up in a house later). Resolved by `BattleController.FindSpawnPosition()` against `SpawnPoint` marker components (`magus.battle`, a `string Id` field) placed in the map prefab — falls back to a `SpawnPoint` with Id `"default"`, then any `SpawnPoint` at all, then the map's own root position, logging a warning at each fallback. Neither `map_test_01` nor `map_test_02` has any `SpawnPoint`s placed yet.
- Note: dialogue can also be triggered *without* going through a StoryNode at all — a cutscene's Timeline can fire a Signal that calls `CutsceneManager.PlayDialogueDuringCutscene()` via `DialogueCutsceneSignal` (`Magus/Scripts/Cutscene/`), for a conversation that's part of a cutscene rather than a separate narrative beat. See [Async Actions](#async-actions) and `CutsceneManager`.
- Not implemented — depend on systems/content that don't exist yet: `UnlockEnding` (ending content)

### Async Actions
`StartTimelineAction` and `StartBattleAction` implement `IAsyncStoryAction : IStoryAction` (`Magus/Scripts/Story/IAsyncStoryAction.cs`). Everything above this point in the doc describes the original model: `StoryManager.Evaluate()` runs a node's actions and marks it complete in the same synchronous pass, unlocking children immediately. That's still exactly what happens for plain `IStoryAction`s. `IAsyncStoryAction` changes this for whichever actions need it:

- A node's plain actions still run immediately, in the same `Evaluate()` pass, as before.
- Its async actions then run **sequentially, one at a time** — `StoryManager.RunAsyncActions()` doesn't start the next one until the previous calls its `onComplete` callback. Multiple `StartTimelineAction`s on one node queue cutscenes back to back, not simultaneously.
- The node is **not** marked completed, and its children stay locked (excluded from the frontier), until every async action has completed. A new `_inProgressNodeIds` set tracks this — a node in progress is excluded from both "eligible to re-fire" and "completed" checks, so it doesn't re-trigger its cutscene while waiting.
- `StartTimelineAction.ExecuteAsync` subscribes to `CutsceneFinishedEvent` (below) filtered by `CutsceneAddress`, and calls `onComplete` when it arrives. This is what makes "wait for a cutscene to finish before running a child node" possible: put the `StartTimelineAction` on a parent node, and the child node (with no further gating needed) simply won't unlock until the cutscene's `CutsceneFinishedEvent` fires.
- `StartBattleAction.ExecuteAsync` subscribes to `BattleReadyEvent` (below) the same way. **This one was a real bug fix, not just a nice-to-have**: as a plain `IStoryAction`, `StartBattleAction` only published a request and returned immediately, so its node completed (unlocking children) before `BattleController.Init()`'s async map/player load chain had actually finished. A child node's `StartTimelineAction` could then fire before the player was even spawned yet, so `CutsceneManager.RegisterActor("Player", ...)` hadn't run, so `SetPlayerInputEnabled` silently found nothing to disable — the player could move freely during that first cutscene. Making `StartBattleAction` async closes this race.
- Not persisted: a node interrupted mid-async-action by a save/reload isn't remembered as "in progress" — it just re-fires from scratch next time it's evaluated, same as an incomplete node. Acceptable for now, same reasoning as `DialogueManager`'s un-resumed mid-conversation save.

### Blackboard
v1 ships **Global scope only** — a flat `Dictionary<string, Variable>` of bool/int/float/string values, saved and loaded as part of StoryManager's [Save Data](#save-data).

The hierarchical Global/Campaign/Chapter scoping described in earlier drafts is deferred: "Campaign" and "Chapter" aren't defined anywhere yet, in this doc or in the narrative design docs (`Docs/magus-lore/`), and scoping rules (does Chapter override Campaign? are they cleared on chapter advance?) can't be designed sensibly until that structure exists. Revisit once the narrative content plan defines what a Chapter/Campaign actually is; see [Future Extensions](#future-extensions).

Types: bool, int, float, string.

Two ways a variable gets written: a node's own `SetVariableAction` (see [Actions](#actions)), or externally via `DialogueChoiceMadeEvent` — `StoryManager` subscribes and calls `SetVariable(e.VariableKey, StoryVariable.FromString(e.Value))` in response to a Dialogue Choice entry being answered. This is the only place something outside `StoryManager` causes a Blackboard write, and it's still `StoryManager` doing the actual write, not Dialogue reaching in — see [DialogueSystem's Choice](DialogueSystem.md#choice).

### Narrative Events
Published on [EventBus](EventBus.md) after actions run.

- Implemented: `ChapterAdvancedEvent`, published by `StoryManager.AdvanceChapter(chapterId)`.
  - `BattleStartRequestedEvent`, published by `StartBattleAction`. Not consumed by a presentation system — `GameManager` subscribes, stores the options on `PendingBattleInitOptions`, and calls `ChangeState(GameState.Battle)`. `BattleGameState.OnEnter()` reads those options instead of hardcoding a map/player, so entering Battle is entirely story-graph-driven. `UITitle`'s New Game/Continue buttons only reset/load story progress and never call `ChangeState` themselves — doing both would enter Battle twice.
  - `BattleReadyEvent`, published by `BattleController` once its map/player load chain (and `InitRequired`'s master-data/player-setup/camera step) has actually finished — not just requested. Consumed by `StartBattleAction` — see [Async Actions](#async-actions) for why this exists (it's a bug fix, not just symmetry with the other `Start*Action`s).
  - `ConversationRequestedEvent`, published by `StartDialogueAction`. `DialogueManager` subscribes directly and calls `Play(e.GraphAddress)` — no `GameManager`-style intermediary needed here, since playing a conversation doesn't change `GameState`.
  - `TimelineRequestedEvent`, published by `StartTimelineAction`. `CutsceneManager` subscribes directly and calls `Play(e.CutsceneAddress)` — same reasoning as `ConversationRequestedEvent`, no `GameState` change involved. The cutscene address points at a self-contained Addressable prefab (`PlayableDirector` + `TimelineAsset` + whatever's bound inside the prefab). Runtime actors not part of the prefab (the live player, eventually NPCs) are bound via `CutsceneManager.RegisterActor(name, gameObject)` — any Timeline track named to match a registered actor gets bound automatically before `Play()`, resolving to whatever component type that track actually needs (`Animator` for an Animation Track, the `GameObject` itself for an Activation Track, etc.) via `PlayableBinding.outputTargetType`. `BattleController` registers the player as `"Player"` right after spawning it. `CutsceneManager` also toggles `PlayerController.InputEnabled` off for `Play()` and back on for `Stop()`/natural completion, so a Timeline Animation Track can drive the player's Animator without the player's own input fighting it.
  - `CutsceneFinishedEvent`, published by `CutsceneManager.Stop()` — covers both natural completion (the `PlayableDirector` stops) and interruption (a manual `Stop()` call, or a new `Play()` cutting off a previous one), so a `StartTimelineAction` waiting on it never gets stuck. If a Timeline Signal called `CutsceneManager.PlayDialogueDuringCutscene()` mid-playback (see [Actions](#actions)), this event is held back until that dialogue's own `DialogueManager.Ended` fires too — whichever of "Timeline stopped" / "dialogue ended" happens last is what actually triggers it. Consumed by `StartTimelineAction` — see [Async Actions](#async-actions).
  - `CheckpointReachedEvent`, published by `CheckpointAction`. `CheckpointManager` (`Magus/Scripts/Checkpoint/`) subscribes and calls `SaveSystem.Save()` — see [SaveSystem's Dependents](SaveSystem.md#dependents). This is also, as of now, the *only* thing that ever calls `Save()` anywhere in the project — the game saves at checkpoints, nowhere else.
- Not implemented — no action produces this yet, since the action that would (`UnlockEnding`) depends on ending content that doesn't exist: `EndingUnlocked`

Presentation systems subscribe independently.

## Save Data
StoryManager registers with [SaveSystem](SaveSystem.md#participant-api) as an `ISaveParticipant`. Its data:
- Completed node Ids
- Blackboard variables (Global scope only in v1)
- Story flags
- Current chapter (a single tracked value, not yet a Blackboard scope — see [Blackboard](#blackboard))

"Seen conversations" isn't part of the save data yet — there's nothing to populate it until DialogueSystem exists. Add it alongside DialogueSystem rather than as an unused field now.

Never serialize ScriptableObjects.

## Editor
Built on [GraphFramework](GraphFramework.md). v1 ships the framework's MVP scope only:
- create/delete nodes, drag connections, default inspector, save, validate, pan/zoom (free with GraphView)

Deferred to a later pass, once the runtime system is validated against real content — see [GraphFramework's Deferred list](GraphFramework.md#deferred) and this doc's [Future Extensions](#future-extensions):
- search, comments, minimap, runtime highlighting, undo/redo polish

## Validation
Implemented as `GraphValidator<StoryNode>` per [GraphFramework — Validation](GraphFramework.md#validation), run via the **Validate** button in the graph editor toolbar, results logged to the Console:
- Duplicate Ids
- Missing links
- Cycles
- Orphans

Missing assets is not yet checked, even though `StartBattleAction`/`StartDialogueAction`/`StartTimelineAction` now hold Addressables path strings that could be validated (e.g. flag a typo'd address at author time instead of failing at runtime) — see [Future Extensions](#future-extensions). `CheckpointAction`'s `CheckpointId` isn't an asset reference, so it's out of scope for this check either way.

## Design Principles
- Data-driven
- Event-driven
- Id-based (string Ids, matching Master Data — not GUIDs)
- Manager is a `SingletonComponent<T>`, matching every other manager in the project
- Presentation independent
- Extensible via interfaces

## Future Extensions
- Campaign/Chapter-scoped Blackboard, once the narrative content plan defines what a Chapter and a Campaign actually are (see [Blackboard](#blackboard))
- Full graph editor feature set (search, comments, minimap, runtime highlighting, undo/redo polish, copy/paste)
- Missing-asset validation for `StartBattleAction`/`StartDialogueAction`/`StartTimelineAction`'s Addressables path fields (see [Validation](#validation))
- `CheckpointManager.CurrentCheckpointId` isn't persisted (`CheckpointManager` isn't an `ISaveParticipant`) — nothing reads it back yet (e.g. no "respawn at last checkpoint" logic), add persistence once something does
- No fade-to-black/fade-in system exists anywhere in the project, and nothing orchestrates "load → fade in → trigger cutscene" as a sequence — `BattleGameState.OnEnter()` just calls `BattleController.Init()` and stops. Both are still open from the original cutscene-sequence gap analysis, not resolved by `CutsceneManager`/`CheckpointManager`
- Whether player input should be disabled while dialogue is playing, not just during cutscenes (`PlayerController.SetInputEnabled` exists and `CutsceneManager` already uses it — `DialogueManager` doesn't yet)
- Pause/resume on `CutsceneManager`'s `PlayableDirector` when a Signal triggers mid-cutscene dialogue — deliberately not built. Today, a Timeline keeps playing underneath a Signal-triggered dialogue (only `CutsceneFinishedEvent` waits for the dialogue, not the Timeline itself), so a *single* Timeline with more than one animate→dialogue beat would race ahead into the next beat while the first dialogue is still up. The current workaround, confirmed working: author each beat as its own small cutscene and queue multiple `StartTimelineAction`s on one node (see [Async Actions](#async-actions)) rather than packing multiple beats into one Timeline. Revisit pause/resume only if per-beat cutscenes prove unwieldy (camera/actor continuity across separate prefabs needs matching end/start poses by hand) or animating shared/common objects across beats calls for one continuous Timeline instead.
- A `PopupRequestedEvent`/`StartPopupAction` pair — "Popups" has been in the architecture diagram since the first draft but nothing has ever requested one
- `ItemCondition`, `BossKilledCondition`, `ConversationSeenCondition`, `UnlockEnding` action, `EndingUnlocked` event, and `SeenConversations` save data — all gated on the systems/content they depend on (inventory, a combat kill event on EventBus, ending content, `DialogueManager` reporting completed conversations) existing
- A gameplay-published `StoryEvent` — `StoryManager` already subscribes on EventBus, but nothing in gameplay code publishes one yet
- A `StoryManager` GameObject/`StoryGraph` reference wired into an actual scene — not done as part of this pass
- Multiple save slots (tracked at the [SaveSystem](SaveSystem.md#future-extensions) level, not here)
