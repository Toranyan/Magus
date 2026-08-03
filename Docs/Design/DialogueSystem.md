# DialogueSystem

> Version 1.0

## Dependencies
Build order — all implemented:

1. [EventBus](EventBus.md), [SaveSystem](SaveSystem.md)
2. [StorySystem](StorySystem.md) — two-way link, both directions implemented:
   - Story → Dialogue: a `StoryNode`'s `StartDialogueAction` publishes `ConversationRequestedEvent`; `DialogueManager` plays it.
   - Dialogue → Story: a Choice entry's picked option publishes `DialogueChoiceMadeEvent`; `StoryManager` records it into `StoryBlackboard`. See [Choice](#choice) and [StorySystem's Blackboard](StorySystem.md#blackboard).
3. DialogueSystem (this doc) — runtime + v1 UI implemented; see per-section notes below for what's still manual/pending.

Notably **not** a dependency: [GraphFramework](GraphFramework.md). An earlier draft of this doc built `DialogueGraph`/`DialogueNode` on it, but that was reverted — see [Dialogue Data](#dialogue-data) for why.

## Purpose
The Dialogue System determines **how conversations are presented**. It never advances story state directly — a conversation can *cause* a change (see [Choice](#choice)) only by publishing an event and letting `StoryManager` act on it, never by writing `StoryBlackboard` itself.

## Responsibilities
- Play a `DialogueAsset` start to finish
- Show UI (Name, Portrait, Text, Choices)
- Record Choice responses for StorySystem to read later
- Save/Load dialogue state

## Does NOT
- Write `StoryBlackboard` — only `StoryManager` does, in reaction to `DialogueChoiceMadeEvent`
- Branch within one conversation — every `DialogueAsset` plays start to finish in order; branching between *different* conversations is a `StoryNode`'s job (its `Conditions` decide which `StartDialogueAction` runs)
- Decide *whether* a conversation is available — same reasoning: that's `StoryNode.Conditions`, evaluated before `StartDialogueAction` ever requests one

## Architecture
```text
StoryManager --(EventBus: ConversationRequestedEvent)--> DialogueManager
                                                                |
                                                          DialogueRunner
                                                                |
                                                          DialogueAsset
                                                                |
                                                          UIDialogueView

DialogueManager --(EventBus: DialogueChoiceMadeEvent)--> StoryManager
```

Both edges are implemented. `StartDialogueAction` (`StoryNode`) → `ConversationRequestedEvent` → `DialogueManager.Play()`; a Choice entry's option → `DialogueRunner.ResponseRecorded` → `DialogueManager` → `DialogueChoiceMadeEvent` → `StoryManager` writes `StoryBlackboard`. See [StorySystem's Narrative Events](StorySystem.md#narrative-events) and [Blackboard](StorySystem.md#blackboard).

## Dialogue Data

**Not a graph.** The original draft of this doc put `DialogueGraph`/`DialogueNode` on [GraphFramework](GraphFramework.md), the same base `StoryGraph` uses. That was reverted once the actual authoring intent became clear: conversations never branch *within* themselves — divergence only ever happens *between* separate `DialogueAsset`s, driven by `StoryNode.Conditions` picking which `StartDialogueAction` to run. A graph (nodes, `ChildId` edges, a custom GraphView editor with a node-type picker) was solving a problem that doesn't exist here. `StoryGraph` still uses GraphFramework — this reversal is Dialogue-specific.

`DialogueAsset : ScriptableObject` holds a plain `List<DialogueEntry>` (`Magus/Scripts/Dialogue/DialogueAsset.cs`), authored via Unity's default reorderable-list Inspector — no custom editor needed at all. `DialogueRunner` plays it as `index++`, not graph traversal.

`DialogueEntry` (`Magus/Scripts/Dialogue/DialogueEntry.cs`) is **one concrete class with a `Kind` enum** (`Text`/`Choice`), not polymorphic subclasses — a deliberate reversal of [StorySystem's Conditions/Actions pattern](StorySystem.md#actions). The polymorphic `[SerializeReference]` approach only pays for itself when a custom editor is already drawing the list (as `GraphEditorWindow` does for `StoryNode`'s Conditions/Actions, via `BuildSerializeReferenceListField`); a plain `List<DialogueEntry>` on a ScriptableObject gets full add/remove/reorder from Unity's default Inspector for free, with zero editor code, only if every element is the same concrete type. A few fields going unused per `Kind` (Choice's `Options` on a Text entry) is the trade, and it's a small one for two kinds.

### Text
`CharacterId` (`CharacterMasterData` Id), `Expression` (string key, ignored until Character Assets grow past one portrait — see [Character Assets](#character-assets)), `Text` (`LocalizedString`, see [Localization](#localization)).

### Choice
`Options: List<DialogueChoiceOption>` (`LocalizedString Text`, `string ResponseValue`) and a `VariableKey`. Picking an option **never branches within the asset** — `DialogueRunner` always continues to the next entry regardless of which option was picked. If `VariableKey` is set, the chosen option's `ResponseValue` is published as a `DialogueChoiceMadeEvent` and `StoryManager` writes `StoryBlackboard[VariableKey] = ResponseValue` (a string variable) — so a *later* `StoryNode`'s `VariableCondition` can read what the player picked and route to a different conversation/outcome. This is how branching-on-player-choice happens without branching the conversation data itself.

### Deferred to a later pass
Presentation-only entry kinds from the original graph-based draft — nothing about the reversal to a linear list rules these out, they just haven't been built: **Camera**, **Music**, **Background**, **Wait**. Also deferred: reading `StoryBlackboard` from *within* a conversation (a "Condition"-equivalent entry) — no entry kind needs it yet, and it wasn't part of the user's request that prompted this rework, so it stays a documented possibility rather than something built speculatively.

## DialogueManager
`DialogueManager : SingletonComponent<DialogueManager>`, matching `StoryManager`/`BattleController`. Also an `ISaveParticipant` (`SaveKey = "dialogue"`), the third registered with [SaveSystem](SaveSystem.md) alongside `StoryManager` and `BattleController`.

```csharp
Play(string assetAddress);  // Addressables path, not a pre-loaded DialogueAsset - see below
Stop();
Pause();
Resume();
Skip();
bool IsPlaying { get; }
```

`Play` takes an Addressables path rather than a pre-loaded asset, matching `BattleController.Init(BattleInitOptions)`'s convention — and giving `CaptureState` a stable string to save (see [Save Data](#save-data)).

`DialogueManager` resolves each entry's presentable content and drives the view: for a Text entry, looks up `CharacterMasterData` by `CharacterId`, loads its portrait `Sprite` via Addressables, and resolves `Text` via `LocalizedString.GetLocalizedStringAsync()`; for a Choice entry, resolves each option's text the same way, and subscribes to `DialogueRunner.ResponseRecorded` to publish `DialogueChoiceMadeEvent`. This resolution/integration logic lives on `DialogueManager`, not `DialogueRunner` (which only knows the asset's data, nothing about master data/localization/UI/EventBus) or the view (which stays a dumb setter, see [UI](#ui)).

## DialogueRunner
Plain index-based traversal through `DialogueAsset.Entries` — `Start`/`Advance`/`Stop`, plus `ChooseOption(index)` for a Choice entry (records via the `ResponseRecorded` C# event if `VariableKey` is set, then advances same as any other entry). No graph, no branching, no EventBus dependency — publishing `DialogueChoiceMadeEvent` is `DialogueManager`'s job, keeping `DialogueRunner` a dependency-free traversal engine.

## Character Assets
Master-data pattern, same as Spell/Ability/Unit (`BaseMasterData` + `MasterDataUnity<T>`, Addressables-loaded — see `Magus/Scripts/Master/`).

v1: `DisplayName`, one `Portrait`. Deferred until a node/UI actually needs them: multiple `Expressions`, `ThemeColor`, `Voice`.

## Localization
Text fields (`DialogueEntry.Text`, `DialogueChoiceOption.Text`) are `UnityEngine.Localization.LocalizedString`, not raw strings — the project already has Unity Localization configured with EN/JA tables (`Magus/Localization/Tables/`), so this is a direct fit, not a new system. `DialogueManager` resolves them via `LocalizedString.GetLocalizedStringAsync()` when presenting an entry.

## UI
v1: Name, Portrait, Text, Choices — implemented as `UIDialogueView` (`Magus/Scripts/UI/Dialogue/`), a dumb view following `UIBattleView`'s pattern: `DialogueManager` calls its setters (`SetSpeaker`, `SetBodyText`, `ShowChoices`, …) and subscribes to its `AdvanceRequested`/`ChoiceSelected` events, same division of responsibility as `UIBattleView`/`CastMenuPresenter` elsewhere in the project. Deferred: History, Auto, Skip. Unaffected by the graph → linear-list reversal — the view never knew about nodes/entries, only strings and sprites.

**Needs manual Editor setup, not done as part of this pass**: a `UIDialogueView.prefab` under `Assets/Magus/Addressables/UI/Views/` (matching `UIManager`'s addressing convention — address = `UI/Views/UIDialogueView`), with the view script's `_nameText`/`_portraitImage`/`_bodyText`/`_advanceButton`/`_choicesContainer`/`_choiceButtonPrefab` fields wired to real UI elements, and marked Addressable. No dialogue can actually be seen on screen until that prefab exists.

## Save Data
`DialogueManager` registers with [SaveSystem](SaveSystem.md#participant-api) as an `ISaveParticipant`:
- Current asset address (the Addressables path passed to `Play`)
- Current entry index
- Auto/Skip state (once those exist — not in v1's UI)

"Text progress" (mid-typewriter character position) from earlier drafts is dropped — not worth persisting through a save/load boundary; resuming a conversation re-shows the current entry's text from the start.

`RestoreState` currently only logs the restored asset/index — unlike `BattleController.TryResumeSavedBattle()`, nothing calls it to actually resume a conversation. There's no defined trigger point/UX for "resume mid-conversation on Continue" yet (does it even make sense to drop the player back into dialogue after a reload, vs. just letting the StoryGraph re-request it?) — see [Future Extensions](#future-extensions).

## Validation
None. `GraphValidator` doesn't apply — there's no graph to validate anymore. A `DialogueAsset` is just a list; the only way to "break" it is an empty `Options` list on a Choice entry or a bad `CharacterId`, neither currently checked. Add validation here only if bad content actually starts shipping, not preemptively.

## Design Principles
- Presentation only
- Data-driven — a plain sequential list, not a graph (see [Dialogue Data](#dialogue-data))
- One concrete `DialogueEntry` type with a `Kind` enum, not polymorphic subclasses — the opposite call from StorySystem's Conditions/Actions, and deliberately so; see [Dialogue Data](#dialogue-data) for why the calculus differs
- Id-based (string Ids, matching Master Data and StorySystem — not GUIDs)
- Manager is a `SingletonComponent<T>`, matching every other manager in the project
- Localizable
- Themeable — not defined further yet; likely just Character `ThemeColor` driving UI accents once Character Assets grow that far (see [Future Extensions](#future-extensions))

## Future Extensions
- `UIDialogueView.prefab` — needs to be authored in the Editor before any of this is visible on screen; see [UI](#ui)
- Deciding whether/how to resume a mid-conversation save at all, then wiring `DialogueManager.RestoreState` to act on it; see [Save Data](#save-data)
- Camera, Music, Background, Wait entry kinds; reading `StoryBlackboard` from within a conversation (see [Deferred](#deferred-to-a-later-pass))
- Character Expressions, ThemeColor, Voice
- History, Auto, Skip UI
- `ConversationSeenCondition` for StorySystem, once `DialogueManager` can report which conversations have been played (see [StorySystem's Future Extensions](StorySystem.md#future-extensions))
