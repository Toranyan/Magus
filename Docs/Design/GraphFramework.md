# Graph Framework

## Purpose

Node/graph runtime and editor that [StorySystem](StorySystem.md)'s StoryGraph is built on. Originally meant to be shared with [DialogueSystem](DialogueSystem.md)'s DialogueGraph too — that didn't happen. DialogueSystem turned out not to need branching within a single conversation (only *between* conversations, which is a StoryNode's job), so its data collapsed to a plain sequential list instead of a graph; see [DialogueSystem's Dialogue Data](DialogueSystem.md#dialogue-data). `StoryGraph` is this framework's only consumer today.

## Responsibilities

- Define the common shape of a graph asset and a node within it
- Provide a GraphView-based editor window that any graph type built on this framework can open
- Persist nodes as part of the graph's ScriptableObject asset

## Does NOT

- Know anything about story specifically — `StoryNode` subclasses the framework's base types
- Evaluate or execute graphs at runtime — that belongs to `StoryManager`
- Ship the full editor feature set (minimap, comments, search, runtime highlighting) in v1 — see [MVP Scope](#mvp-scope). Validation shipped as part of MVP; see [Validation](#validation)

## Location

`ToraLib/Scripts/Graph/`, namespace `tora.graph`. Editor window under `ToraLib/Scripts/Graph/Editor/`, following the same split as `AssetBundle/Editor`. Generic infrastructure — Magus-specific node types (`StoryNode` and its Condition/Action types) live in `Magus/Scripts/Story/` and reference this framework, not the other way around.

## Core Types

```csharp
[Serializable]
public abstract class GraphNode
{
    public string Id;         // stable, human-authored or auto-generated string, unique within the graph — matches the project's existing string-Id convention (see Master Data), not a System.Guid
    public Vector2 Position;  // editor layout position
    public List<string> ChildIds; // outgoing links, by Id, within the same graph
}

public abstract class GraphAsset<TNode> : ScriptableObject where TNode : GraphNode
{
    public List<TNode> Nodes;
}
```

- `StoryGraph : GraphAsset<StoryNode>`, `StoryNode : GraphNode`.
- Nodes are stored inline in the graph's `Nodes` list, not as separate sub-assets — simpler save/load, and the framework doesn't need `AssetDatabase.AddObjectToAsset` bookkeeping.

## Polymorphic Fields (Conditions / Actions)

`StoryNode` needs a list of `IStoryCondition` and a list of `IStoryAction`. Unity does not serialize interface-typed fields or polymorphic lists by default. This framework's convention: use `[SerializeReference]`.

```csharp
[SerializeReference]
public List<IStoryCondition> Conditions;

[SerializeReference]
public List<IStoryAction> Actions;
```

Every concrete implementation (`StoryFlagCondition`, `SetFlagAction`, etc.) must be a plain `[Serializable]` C# class (not a ScriptableObject) implementing the interface. `[SerializeReference]` is supported by Unity's built-in Inspector out of the box (shows a type picker per list element) — no custom PropertyDrawer is required for v1; a nicer searchable type-picker is a future editor polish item, not a blocker.

## Polymorphic Node Types

Implemented, but **currently unused** — built for DialogueGraph, which was then reverted in favor of a plain list (see [Purpose](#purpose)). Left in place rather than reverted a second time: `StoryGraph` already paid the one-time cost of migrating to this format (see below), and reverting again would force yet another round of recreating its nodes for a capability that's simply idle, not actively harmful. If DialogueGraph-style branching-within-one-asset ever becomes a real need again for some future graph type, this is already here.

`GraphAsset<TNode>.Nodes` is `[SerializeReference] List<TNode>`, so `TNode` could be abstract with concrete subclasses as elements — the same treatment as [Polymorphic Fields](#polymorphic-fields-conditions--actions) above applied to the node list itself rather than a field within a node. `TNode` has no `new()` constraint.

`GraphEditorWindow.GetCreatableNodeTypes()` computes what "Create Node" can produce: `TNode` itself if it's concrete, plus every non-abstract `TypeCache`-discovered type deriving from it. For `StoryGraph`, `StoryNode` is concrete with no subclasses, so this always resolves to exactly one creatable type and the menu stays a flat "Create Node" action — the multi-kind submenu path (`Create Node ▸ Text/Choice/…`) has no current exerciser. Node creation goes through `Activator.CreateInstance(nodeType)` instead of `new TNode()`.

`StoryNode` is functionally unaffected by any of this (still exactly one concrete type in play, and `[SerializeReference]` works fine with a single runtime type) — but adopting it **changed StoryGraph's on-disk serialization format** for its `Nodes` list. Unity does not auto-migrate a plain-list field to a managed-reference one, so `StoryGraph` `.asset` files authored before this change needed their nodes re-created once already.

## Editor Window

A generic `GraphEditorWindow<TGraph, TNode>` (`ToraLib/Scripts/Graph/Editor/GraphEditorWindow.cs`) using Unity's `GraphView` API (`UnityEditor.Experimental.GraphView`), subclassed per graph type — currently only `StoryGraphEditorWindow : GraphEditorWindow<StoryGraph, StoryNode>` — and opened via double-clicking the asset (`[OnOpenAsset]`).

Two implementation notes learned building `StoryGraphEditorWindow`, worth knowing if this framework ever gets a second graph type:
- `GraphView` itself is abstract — instantiate a trivial concrete subclass (`ConcreteGraphView : GraphView`), not `GraphView` directly.
- Override `GraphView.GetCompatiblePorts` to allow connecting any Output port to any Input port on a different node. The default implementation relies on registered `NodeAdapter` type conversions that aren't set up here, and silently rejects every drop (the drag preview line renders, but nothing connects) without this override.

### MVP Scope — shipped

- Create/delete nodes (right-click canvas → Create Node)
- Drag connections between nodes (writes to `ChildIds`)
- Default Inspector panel for the selected node's fields, built from native UI Toolkit `PropertyField`s bound to the `SerializedObject` — plain IMGUI property iteration was tried first and doesn't reliably surface the `[SerializeReference]` type picker in a narrow panel, so don't go back to it
- Custom type-picker UI for `[SerializeReference]` list fields (`GraphEditorWindow.BuildSerializeReferenceListField`) — foldout + per-element Remove + a type dropdown/Add row, driven by `TypeCache.GetTypesDerivedFrom`. Built because Unity's own built-in Add-button type picker for SerializeReference lists isn't reliably discoverable in practice. Fields needing this are opted in via `GetSerializeReferenceListFields()` (field name → base type), overridden per graph type — see `StoryGraphEditorWindow`'s `Conditions`/`Actions` entries
- Save (writes back to the ScriptableObject asset, `AssetDatabase.SaveAssets()`)
- Validate (toolbar button, runs [GraphValidator](#validation), logs to Console)
- Pan/zoom — free with `GraphView`, no extra work

### Deferred (see StorySystem's Editor section for the full wishlist)

- Node search
- Comments
- Minimap
- Runtime highlighting (active node during play)
- Copy/paste
- Unity's built-in `Undo` integration beyond whatever `GraphView` gives for free by default

## Validation

Implemented: `GraphValidator<TNode>` (`ToraLib/Scripts/Graph/Editor/GraphValidator.cs`), a single shared, static validator parameterized by graph — kept generic (not folded directly into `StoryGraphEditorWindow`) in case a second graph type ever needs it again. Covers:
- Duplicate Ids
- Missing links (a `ChildId` with no matching node)
- Cycles
- Orphans (no incoming or outgoing links)

Not yet covered: missing asset references — `StoryNode`'s `StartBattleAction`/`StartDialogueAction` hold Addressables path strings that could be validated (see [StorySystem's Validation](StorySystem.md#validation)), but nothing checks them yet.

## Dependents

- [StorySystem](StorySystem.md) — `StoryGraph`/`StoryNode`. Implemented and in active use. The only dependent — see [Purpose](#purpose) for why DialogueSystem isn't.

Must exist, at MVP scope, before StoryGraph's authoring workflow can be used. StoryManager's runtime evaluation logic can be developed and unit-tested against hand-constructed `StoryGraph` assets (built via code or the Inspector directly) before the editor window exists, if sequencing requires it.

## Future Extensions

- Full editor feature set listed under Deferred above
- Sub-graphs / nested graphs
- A second graph type to actually exercise [Polymorphic Node Types](#polymorphic-node-types) and the multi-kind "Create Node" submenu, if one ever comes up
