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
           Dialogue  Timeline  Popups  GameManager (game flow, e.g. BattleStartRequestedEvent)
```

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

- Implemented: `SetFlagAction`, `SetVariableAction`, `LogMessageAction` (debug/POC only — prints a message to the console, not part of the original design), `StartBattleAction` (publishes `BattleStartRequestedEvent` with a map/player Addressables path), `StartDialogueAction` (publishes `ConversationRequestedEvent` with a `DialogueAsset` Addressables path — see [Narrative Events](#narrative-events)). `StartDialogueAction` supersedes the originally-planned `UnlockConversation` name/shape — it directly requests playback rather than just marking a conversation available for some other system to trigger later, mirroring `StartBattleAction`
- Not implemented — depend on systems/content that don't exist yet: `UnlockEnding` (ending content)

### Blackboard
v1 ships **Global scope only** — a flat `Dictionary<string, Variable>` of bool/int/float/string values, saved and loaded as part of StoryManager's [Save Data](#save-data).

The hierarchical Global/Campaign/Chapter scoping described in earlier drafts is deferred: "Campaign" and "Chapter" aren't defined anywhere yet, in this doc or in the narrative design docs (`Docs/magus-lore/`), and scoping rules (does Chapter override Campaign? are they cleared on chapter advance?) can't be designed sensibly until that structure exists. Revisit once the narrative content plan defines what a Chapter/Campaign actually is; see [Future Extensions](#future-extensions).

Types: bool, int, float, string.

Two ways a variable gets written: a node's own `SetVariableAction` (see [Actions](#actions)), or externally via `DialogueChoiceMadeEvent` — `StoryManager` subscribes and calls `SetVariable(e.VariableKey, StoryVariable.FromString(e.Value))` in response to a Dialogue Choice entry being answered. This is the only place something outside `StoryManager` causes a Blackboard write, and it's still `StoryManager` doing the actual write, not Dialogue reaching in — see [DialogueSystem's Choice](DialogueSystem.md#choice).

### Narrative Events
Published on [EventBus](EventBus.md) after actions run.

- Implemented: `ChapterAdvancedEvent`, published by `StoryManager.AdvanceChapter(chapterId)`.
  - `BattleStartRequestedEvent`, published by `StartBattleAction`. Not consumed by a presentation system — `GameManager` subscribes, stores the options on `PendingBattleInitOptions`, and calls `ChangeState(GameState.Battle)`. `BattleGameState.OnEnter()` reads those options instead of hardcoding a map/player, so entering Battle is entirely story-graph-driven. `UITitle`'s New Game/Continue buttons only reset/load story progress and never call `ChangeState` themselves — doing both would enter Battle twice.
  - `ConversationRequestedEvent`, published by `StartDialogueAction`. `DialogueManager` subscribes directly and calls `Play(e.GraphAddress)` — no `GameManager`-style intermediary needed here, since playing a conversation doesn't change `GameState`.
- Not implemented — no action produces these yet, since the action that would (`UnlockEnding`) depends on ending content that doesn't exist: `TimelineRequested`, `EndingUnlocked`

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

Missing assets is not yet checked, even though `StartBattleAction`/`StartDialogueAction` now hold Addressables path strings that could be validated (e.g. flag a typo'd `GraphAddress` at author time instead of failing at runtime) — see [Future Extensions](#future-extensions).

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
- Missing-asset validation for `StartBattleAction`/`StartDialogueAction`'s Addressables path fields (see [Validation](#validation))
- `ItemCondition`, `BossKilledCondition`, `ConversationSeenCondition`, `UnlockEnding` action, `TimelineRequested`/`EndingUnlocked` events, and `SeenConversations` save data — all gated on the systems/content they depend on (inventory, a combat kill event on EventBus, ending content, `DialogueManager` reporting completed conversations) existing
- A gameplay-published `StoryEvent` — `StoryManager` already subscribes on EventBus, but nothing in gameplay code publishes one yet
- A `StoryManager` GameObject/`StoryGraph` reference wired into an actual scene — not done as part of this pass
- Multiple save slots (tracked at the [SaveSystem](SaveSystem.md#future-extensions) level, not here)
