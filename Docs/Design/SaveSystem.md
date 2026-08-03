# Save System

## Purpose

General-purpose persistence for player progress. Built to unblock [StorySystem](StorySystem.md) (completed nodes, variables, flags, current chapter, seen conversations), but scoped as shared infrastructure so other systems (`PlayerProgression`'s experience/currency totals, unlocks, settings) register with it later instead of each inventing their own file I/O.

No save/load of any kind currently exists in the project — this is net-new.

## Responsibilities

- Own the save file's lifecycle: locate it, read it, write it, know whether one exists
- Let independent systems contribute and reclaim their own data without knowing about each other
- Version the file format so future field additions don't break old saves

## Does NOT

- Know what any participant's data means — it stores and returns opaque blobs
- Auto-save on a timer or on scene transitions (callers decide when to save)
- Support cloud sync or multiple save slots in v1

## Location

`ToraLib/Scripts/Save/SaveSystem.cs`, namespace `tora.save`. Generic infrastructure, alongside `EventBus` and `SingletonComponent<T>`.

## Design

Unity's built-in `JsonUtility` is used (no new package dependency — `com.unity.modules.jsonserialize` is already present; Newtonsoft is not installed). `JsonUtility` cannot serialize a heterogeneous `Dictionary` or polymorphic types directly, so the file is a flat envelope of independently-serialized blocks, one per participant:

```csharp
[Serializable]
public class SaveFile
{
    public int Version;
    public List<SaveBlock> Blocks;
}

[Serializable]
public class SaveBlock
{
    public string Key;   // participant's SaveKey
    public string Json;  // JsonUtility.ToJson(participant's own [Serializable] data type)
}
```

## Participant API

```csharp
public interface ISaveParticipant
{
    string SaveKey { get; }              // stable, unique across all participants, e.g. "story"
    string CaptureState();               // returns JsonUtility.ToJson(ownData)
    void RestoreState(string json);      // JsonUtility.FromJson<OwnDataType>(json); no-op if json is null (fresh save)
}
```

- A participant defines its own `[Serializable]` data class privately — `SaveSystem` never needs to know its shape.
- `StoryManager` is the first `ISaveParticipant`, holding completed node Ids, Blackboard variables, story flags, and current chapter (see [StorySystem's Save Data](StorySystem.md#save-data)).
- `BattleController` (`magus.battle`) is the second, holding the last `BattleInitOptions` (map/player Addressables paths) it was given. Deliberately separate from `StoryManager`'s data: restoring story progress does not re-run already-completed nodes' actions, so "what map am I currently in" can't be reconstructed by replaying the graph — it has to be its own persisted fact. `Continue`/`Load Game` call `StoryManager.Load()` and then explicitly `BattleController.Instance.TryResumeSavedBattle()`, rather than the map/player restore happening automatically as a side effect of loading story data.

## SaveSystem API

```csharp
public static class SaveSystem
{
    public static void Register(ISaveParticipant participant);
    public static void Unregister(ISaveParticipant participant);

    public static void Save();     // calls CaptureState() on every registered participant, writes file
    public static void Load();     // reads file, calls RestoreState() on every registered participant with a matching block
    public static bool HasSave();
    public static void DeleteSave();
}
```

- Participants register themselves (typically on `Awake`/`Initialize`) the same way they would subscribe to `EventBus`.
- `Load()` calls `RestoreState(null)` (or skips the call, participant's choice) for any registered participant with no matching block in the file — this is the normal case for a save file written before that participant existed.
- File location: `Application.persistentDataPath/save.json`. Single slot for v1.

## Versioning

`SaveFile.Version` is bumped whenever a participant's data shape changes in a way that isn't forward-compatible with `JsonUtility`'s default tolerant parsing (field add/remove is fine; field rename or type change is not). `SaveSystem` itself does not attempt migration — a participant whose `RestoreState` needs to handle an old version reads `SaveFile.Version` (passed alongside the blob) and branches internally. No migration framework in v1; add one only once a real breaking change happens.

## Dependents

- [StorySystem](StorySystem.md) — first and, for v1, only participant

## Future Extensions

- Multiple save slots
- Async/background write to avoid a frame hitch on large saves
- Encryption/obfuscation of the save file
- Cloud save sync
- Migration framework, once a participant actually needs one
