# StorySystem

> Version 1.0

## Dependencies
Build order — steps 1-4 are implemented:

1. [EventBus](EventBus.md) — gameplay → StoryManager, StoryManager → presentation systems
2. [GraphFramework](GraphFramework.md) — shared node/graph base that StoryGraph is built on (MVP editor scope only)
3. [SaveSystem](SaveSystem.md) — StoryManager is its first participant
4. StorySystem (this doc) — implemented; see [Core Components](#core-components) for what's live vs. deferred pending step 5
5. [DialogueSystem](DialogueSystem.md) — implemented (v1 entry set/runtime/UI; not graph-based — see its Dialogue Data section). Both directions of the link between the two systems are done: `StartDialogueAction` → `ConversationRequestedEvent` → `DialogueManager` requests a conversation (see [Actions](#actions)/[Narrative Events](#narrative-events)); `DialogueChoiceMadeEvent` → `StoryManager` records the player's answer into `StoryBlackboard` (see [Blackboard](#blackboard)). Conversation *completion* is reported back too, as a flag — see [Progression](#progression)

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

- Implemented: `StoryFlagCondition`, `VariableCondition`, `ConversationSeenCondition` (a flag `DialogueManager` sets when a conversation plays to its end — see [Progression](#progression))
- Not implemented: `Item` (needs an inventory). No `BossKilled` condition is planned anymore — a kill is just a flag (`StoryEventOnDeath` + `StoryFlagCondition`), see [Progression](#progression)

### Actions
Implement `IStoryAction`. Stored as `[SerializeReference] List<IStoryAction>` on `StoryNode`, same mechanism as Conditions.

- Implemented: `SetFlagAction`, `SetVariableAction`, `LogMessageAction` (debug/POC only — prints a message to the console, not part of the original design), `StartBattleAction` (publishes `BattleStartRequestedEvent` with a map/player Addressables path, plus an optional `SpawnPointId` — see below), `StartDialogueAction` (publishes `ConversationRequestedEvent` with a `DialogueAsset` Addressables path), `StartTimelineAction` (publishes `TimelineRequestedEvent` with a cutscene prefab's Addressables path), `CheckpointAction` (publishes `CheckpointReachedEvent` with a `CheckpointId` — see [Narrative Events](#narrative-events)), `ScreenFadeAction` (publishes `ScreenFadeRequestedEvent` with a `TargetAlpha` (1 = black) and `Duration` — see [Screen Fade](#screen-fade)). `StartDialogueAction`/`StartTimelineAction` supersede the originally-planned `UnlockConversation`-style naming/shape — they directly request playback rather than just marking something available for some other system to trigger later, all mirroring the same pattern
- `StartBattleAction.SpawnPointId` — lets different StoryNodes land the player at different points on the *same* map (e.g. arriving at a village gate the first time vs. waking up in a house later). Resolved by `BattleController.FindSpawnPosition()` against `SpawnPoint` marker components (`magus.battle`, a `string Id` field) placed in the map prefab — falls back to a `SpawnPoint` with Id `"default"`, then any `SpawnPoint` at all, then the map's own root position, logging a warning at each fallback. Neither `map_test_01` nor `map_test_02` has any `SpawnPoint`s placed yet.
- Note: dialogue can also be triggered *without* going through a StoryNode at all — a cutscene's Timeline can fire a Signal that calls `CutsceneManager.PlayDialogueDuringCutscene()` via `DialogueCutsceneSignal` (`Magus/Scripts/Cutscene/`), for a conversation that's part of a cutscene rather than a separate narrative beat. See [Async Actions](#async-actions) and `CutsceneManager`.
- Not implemented — depend on systems/content that don't exist yet: `UnlockEnding` (ending content)

### Async Actions
`StartTimelineAction`, `StartBattleAction` and `ScreenFadeAction` implement `IAsyncStoryAction : IStoryAction` (`Magus/Scripts/Story/IAsyncStoryAction.cs`). Everything above this point in the doc describes the original model: `StoryManager.Evaluate()` runs a node's actions and marks it complete in the same synchronous pass, unlocking children immediately. That's still exactly what happens for plain `IStoryAction`s. `IAsyncStoryAction` changes this for whichever actions need it:

- A node's plain actions still run immediately, in the same `Evaluate()` pass, as before.
- Its async actions then run **sequentially, one at a time** — `StoryManager.RunAsyncActions()` doesn't start the next one until the previous calls its `onComplete` callback. Multiple `StartTimelineAction`s on one node queue cutscenes back to back, not simultaneously.
- The node is **not** marked completed, and its children stay locked (excluded from the frontier), until every async action has completed. A new `_inProgressNodeIds` set tracks this — a node in progress is excluded from both "eligible to re-fire" and "completed" checks, so it doesn't re-trigger its cutscene while waiting.
- `StartTimelineAction.ExecuteAsync` subscribes to `CutsceneFinishedEvent` (below) filtered by `CutsceneAddress`, and calls `onComplete` when it arrives. This is what makes "wait for a cutscene to finish before running a child node" possible: put the `StartTimelineAction` on a parent node, and the child node (with no further gating needed) simply won't unlock until the cutscene's `CutsceneFinishedEvent` fires.
- `StartBattleAction.ExecuteAsync` subscribes to `BattleReadyEvent` (below) the same way. **This one was a real bug fix, not just a nice-to-have**: as a plain `IStoryAction`, `StartBattleAction` only published a request and returned immediately, so its node completed (unlocking children) before `BattleController.Init()`'s async map/player load chain had actually finished. A child node's `StartTimelineAction` could then fire before the player was even spawned yet, so `CutsceneManager.RegisterActor("Player", ...)` hadn't run, so `SetPlayerInputEnabled` silently found nothing to disable — the player could move freely during that first cutscene. Making `StartBattleAction` async closes this race.
- `ScreenFadeAction.ExecuteAsync` waits for `ScreenFadeFinishedEvent` the same way, so e.g. `[ScreenFadeAction (1, 0.5s), StartBattleAction, StartTimelineAction]` on one node goes black, loads the map/player behind the black screen, then starts the cutscene.
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
  - `TimelineRequestedEvent`, published by `StartTimelineAction`. `CutsceneManager` subscribes directly and calls `Play(e.CutsceneAddress)` — same reasoning as `ConversationRequestedEvent`, no `GameState` change involved. The cutscene address points at a self-contained Addressable prefab (`PlayableDirector` + `TimelineAsset` + whatever's bound inside the prefab). Runtime actors not part of the prefab (the live player, eventually NPCs) are bound via `CutsceneManager.RegisterActor(name, gameObject)` — any Timeline track named to match a registered actor gets bound automatically before `Play()`, resolving to whatever component type that track actually needs (`Animator` for an Animation Track, the `GameObject` itself for an Activation Track, etc.) via `PlayableBinding.outputTargetType`. Components are looked up with `GetComponentInChildren`, so a track still binds when the component sits on a child of the registered object — needed for the player, whose `Animator` is on its `PlayerCharacter` model child, not the `pc_test_01` root. `BattleController` registers the player as `"Player"` right after spawning it. `CutsceneManager` also holds a player input lock (`PlayerController.AddInputLock(this)`) from `Play()` until `Stop()`/natural completion, so a Timeline Animation Track can drive the player's Animator without the player's own input fighting it. Locks are per owner (`InputEnabled` is true only with none held), so a dialogue ending inside a cutscene doesn't hand control back early — see [DialogueSystem's Player Input](DialogueSystem.md#player-input).
  - `CutsceneFinishedEvent`, published by `CutsceneManager.Stop()` — covers both natural completion (the `PlayableDirector` stops) and interruption (a manual `Stop()` call, or a new `Play()` cutting off a previous one), so a `StartTimelineAction` waiting on it never gets stuck. If a Timeline Signal called `CutsceneManager.PlayDialogueDuringCutscene()` mid-playback (see [Actions](#actions)), this event is held back until that dialogue's own `DialogueManager.Ended` fires too — whichever of "Timeline stopped" / "dialogue ended" happens last is what actually triggers it. Consumed by `StartTimelineAction` — see [Async Actions](#async-actions).
  - **Dialogue Markers (preferred).** A Signal Receiver's reactions are keyed by Signal Asset, so every Signal Emitter sharing `PlayDialogueSignal` plays the same fixed address — different dialogues would each need their own Signal Asset. `DialogueMarker` (`Magus/Scripts/Cutscene/`) carries its own `DialogueAddress` and `PauseTimeline` (default on), so one Timeline holds any number of conversations. Add it via right-click > Add Dialogue Marker on the Timeline's Markers row (or the Signal Track). It's delivered through `INotificationReceiver` to `DialogueCutsceneSignal.OnNotify`, which must sit on the director's GameObject (Markers row) or the track's bound GameObject. In a cutscene prefab both are the root. It fires once per play and retroactively, so a skipped frame can't drop a conversation. `DialogueMarkerEditor` shows the address as the marker's tooltip and flags a missing one. The Signal Receiver path below still works.
  - **Multi-beat cutscenes.** `DialogueCutsceneSignal` has two Signal Receiver reactions: `PlayDialogue`, where the Timeline keeps playing underneath, and `PlayDialogueAndWait`. `PlayDialogueAndWait` freezes the Timeline at the signal until the dialogue ends, then resumes it. It pauses by setting the root playable's speed to 0 rather than calling `PlayableDirector.Pause()`, so the graph keeps evaluating the same frame and every Animation Track holds its pose. This means one continuous Timeline can carry several pose → talk → animate → talk beats with a shared actor, e.g. the player asleep → dialogue → stands up → dialogue. That replaces the older workaround of queuing one small cutscene per beat, where the player's pose snapped back to its Animator controller between cutscenes. Set Animation Track clips' post-extrapolation to **Hold** so an actor keeps a clip's last frame while waiting on a dialogue that starts after the clip ends.
  - `ScreenFadeRequestedEvent` / `ScreenFadeFinishedEvent` — see [Screen Fade](#screen-fade).
  - `CheckpointReachedEvent`, published by `CheckpointAction`. `CheckpointManager` (`Magus/Scripts/Checkpoint/`) subscribes and calls `SaveSystem.Save()` — see [SaveSystem's Dependents](SaveSystem.md#dependents). This is also, as of now, the *only* thing that ever calls `Save()` anywhere in the project — the game saves at checkpoints, nowhere else.
- Not implemented — no action produces this yet, since the action that would (`UnlockEnding`) depends on ending content that doesn't exist: `EndingUnlocked`

Presentation systems subscribe independently.

### Screen Fade
`ScreenFader` (`Magus/Scripts/Cutscene/`) is a full-screen black overlay on its own Screen Space Overlay canvas (sorting order 1000, above all other UI). It builds its own Canvas/Image in `Awake`, and `SingletonComponent` creates it on first use of `Instance`, so nothing has to be placed in a scene. Two ways to drive it:
- **From the story graph** — `ScreenFadeAction` → `ScreenFadeRequestedEvent`. `CutsceneManager` handles it, not `ScreenFader`, because `ScreenFader` may not exist yet to subscribe. It runs `ScreenFader.FadeAsync` and then publishes `ScreenFadeFinishedEvent`.
- **From inside a cutscene** — a **Screen Fade Track** (`ScreenFadeTrack`/`ScreenFadeClip`/`ScreenFadeMixerBehaviour`), added from the Timeline window's `+` menu. It needs no binding. Each clip lerps `StartAlpha` → `EndAlpha` over its length (default 1 → 0, a fade-in). The mixer reads the director's time, not clip weights: past a clip it holds that clip's `EndAlpha`, and before the first clip it leaves the fader alone. So a cutscene that begins while the screen is black (from a `ScreenFadeAction`) stays black until its first fade clip starts. It does nothing in Timeline editor preview.

The intended order is: the story graph fades to black and loads, then the cutscene fades in. The cutscene poses its actors (e.g. the player lying down) on its first frame, before the screen clears. If the story graph faded in instead, the player would visibly stand idle for a moment before the Timeline took over.

## Progression
How the story moves forward, and who goes first. The answer is neither: the graph and the game take turns.

- **Gameplay reports facts, never moves the graph.** A `StoryEvent { Id }` on EventBus ("entered_village", "slime_boss_killed"). Gameplay doesn't know which node is active and never calls into the graph.
- **The graph decides what facts mean.** A node in the frontier waits on its Conditions. When they pass it runs its Actions (cutscene, dialogue, battle) and unlocks its children, which then wait on the next facts.

```text
graph:  node_3 done --> node_4 waits: [StoryFlagCondition "entered_village"]
                                            ^
game:   player enters StoryTriggerZone --> StoryEvent "entered_village"
                                            |
graph:  node_4 passes --> runs actions --> unlocks node_5, waits: ["slime_boss_killed"] ...
```

**Events are recorded as facts.** `StoryManager.RaiseEvent` sets `StoryEvent.Id` as a StoryBlackboard flag, then evaluates. So the order the player does things in doesn't matter: if the boss is killed before the node that asks for it is reachable, that node's `StoryFlagCondition` already passes the moment it enters the frontier. A plain "something happened" event would have been missed. Flags are also saved, so facts survive Continue.

Reporters (`Magus/Scripts/Story/Triggers/`):
- `StoryTriggerZone` — trigger collider in a map prefab, reports its Id when the player enters (once per load by default).
- `StoryEventOnDeath` — alongside a `Unit`, reports its Id when it dies (same pattern as `LootDropper`).
- `DialogueManager` — reports `StoryFlags.ConversationSeen(address)` (`"seen:<address>"`) when a conversation plays to its end. A conversation cut off by `Stop()` doesn't count. Read it with `ConversationSeenCondition`.

Flag names are free-form strings matched between the reporter's Id and the node's `StoryFlagCondition.Flag`. Only code-generated names (like "seen:") live in `StoryFlags`, so they can't drift. Suggested convention: `<what>_<verb past tense>`, e.g. `village_entered`, `slime_boss_killed`.

### Rebuilding the world after a load
Completed nodes' actions never run again after a load: replaying completed nodes doesn't re-fire them. So whatever a story beat changed in the world (boss gone, gate open, NPC moved in) can't be restored by replaying it. It has to be derived from the facts. `StoryFlagGate` (also in `Triggers/`) turns its Targets on or off depending on one flag. It applies when enabled (the map loads after `StoryManager.Load()`, so restored flags are already there) and, if `ReactLive` is on, the moment the flag changes. It listens to `StoryStateChangedEvent`, which `StoryManager.Evaluate()` publishes every time; every path that changes flags, variables or completed nodes ends in an `Evaluate()`. Turn `ReactLive` off when the target sets the flag itself and must finish first, e.g. a boss whose `StoryEventOnDeath` would otherwise hide it mid death animation. `BattleController` saving the last map is the same idea at map level.

### Save rules
Only story state is saved: completed node Ids plus blackboard flags and variables (see [Save Data](#save-data)). Saving happens only at checkpoints. Two consequences of how nodes run:
- **Put `CheckpointAction` alone on its own node.** Plain actions run in `BeginNode`, before the node is marked complete, so the save records the checkpoint node as not completed. On load it runs again. That's harmless when it only saves, but any cutscene or dialogue sharing the node would replay on every Continue.
- **Checkpoint at calm moments** (after a cutscene, on entering an area), never mid-sequence. An in-progress async node isn't persisted (see [Async Actions](#async-actions)); after a load it simply starts over.

## Save Data
StoryManager registers with [SaveSystem](SaveSystem.md#participant-api) as an `ISaveParticipant`. Its data:
- Completed node Ids
- Blackboard variables (Global scope only in v1)
- Story flags
- Current chapter (a single tracked value, not yet a Blackboard scope — see [Blackboard](#blackboard))

Seen conversations aren't a separate field: they're flags (`"seen:<address>"`), saved with the rest of the blackboard.

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
- A `PopupRequestedEvent`/`StartPopupAction` pair — "Popups" has been in the architecture diagram since the first draft but nothing has ever requested one
- `ItemCondition`, `UnlockEnding` action and `EndingUnlocked` event — gated on the systems/content they depend on (inventory, ending content) existing
- `NpcInteract`-style reporter (talk-to-NPC as an interaction, not just a conversation finishing) — needs an interaction system; until then `ConversationSeenCondition` covers "talked to X"
- A `StoryManager` GameObject/`StoryGraph` reference wired into an actual scene — not done as part of this pass
- Multiple save slots (tracked at the [SaveSystem](SaveSystem.md#future-extensions) level, not here)
