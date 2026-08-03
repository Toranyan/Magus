# EventBus

## Purpose

A generic, typed publish/subscribe channel decoupling systems that raise events from systems that react to them. Built primarily to unblock [StorySystem](StorySystem.md) and [DialogueSystem](DialogueSystem.md) — gameplay needs to notify StoryManager without referencing it, and StoryManager needs to notify presentation systems (Dialogue, Timeline, Popups) without referencing them.

This is shared infrastructure, not narrative-specific. Any system may publish or subscribe to any message type.

## Responsibilities

- Register/unregister subscribers per message type
- Dispatch a published message to every current subscriber of its type, synchronously, in subscription order
- Do nothing else — no message queueing, no persistence, no cross-scene lifetime management beyond what subscribers do themselves

## Does NOT

- Replace tightly-coupled, single-system events (see [Relationship to Existing Events](#relationship-to-existing-events))
- Guarantee delivery order across different message types
- Retain or replay messages published before a subscriber registers
- Serialize or save anything

## Location

`ToraLib/Scripts/EventBus/EventBus.cs`, namespace `tora.eventbus`. Lives in ToraLib because it is generic infrastructure, not Magus-specific — consistent with `SingletonComponent<T>` and the FSM base classes already there.

## API

```csharp
public static class EventBus
{
    public static void Subscribe<T>(Action<T> handler);
    public static void Unsubscribe<T>(Action<T> handler);
    public static void Publish<T>(T message);
    public static void Clear<T>();   // editor/tests/domain-reload safety
}
```

- `T` is any type (struct or class) used purely as a message. No marker interface required.
- Static and global by design — matches how the rest of the project already communicates across systems (see `DamageReceiver.GlobalDamageReceived`). A non-static, instantiable bus is not needed unless a future case requires multiple isolated buses (e.g. per-scene); not planned for v1.
- `Subscribe`/`Unsubscribe` must be paired by every subscriber (typically in `OnEnable`/`OnDisable` or `Initialize`/`Shutdown`) to avoid leaking handlers across scene loads, since the bus itself is static and outlives any single scene.
- `Publish<T>` dispatches synchronously on the calling thread/frame. No deferred or next-frame delivery in v1.

## Message Types

Message types are plain C# classes or structs, one per event, defined next to the system that publishes them. No shared base type is required by the bus itself, but by convention:

- Gameplay-facing messages (published by combat/world systems, consumed by StoryManager) live in `Magus/Scripts/Story/Events/` once StorySystem exists, e.g. `BossKilledEvent`, `ItemAcquiredEvent`.
- Narrative-facing messages (published by StoryManager, consumed by Dialogue/Timeline/Popups) live alongside StoryManager, e.g. `ConversationRequestedEvent`, `TimelineRequestedEvent`, `EndingUnlockedEvent`, `ChapterAdvancedEvent`.

"NarrativeEventBus" in the StorySystem/DialogueSystem architecture diagrams is not a second bus — it refers to this same `EventBus`, scoped by convention to narrative message types. There is one bus for the whole project.

## Relationship to Existing Events

The project already uses hand-declared `event Action<T>` fields on individual classes (e.g. `DamageReceiver.DamageReceived` / `DamageReceiver.GlobalDamageReceived`, `PlayerProgression.ExperienceChanged`). These remain the right tool for tightly-coupled, single-system signals where the publisher and subscriber already know about each other's concrete type.

`EventBus` is for the opposite case: a publisher that should not need a compile-time reference to its subscriber, typically because the subscriber lives in a different, higher-level system (e.g. combat should not need to reference `StoryManager` to report a boss kill). Guideline: if the publisher would otherwise need to `using` a namespace it has no other reason to depend on, route it through `EventBus` instead.

Existing systems are not required to migrate their current events to `EventBus` as part of this work — only new cross-system, narrative-relevant signals need to.

## Dependents

- [StorySystem](StorySystem.md) — subscribes to gameplay messages, publishes narrative messages
- [DialogueSystem](DialogueSystem.md) — subscribes to narrative messages published by StorySystem

Must exist before either of those systems is implemented.

## Future Extensions

- Deferred/queued dispatch (publish now, deliver on next frame or a specific point in the frame)
- Editor debug window listing active subscriptions and recent published messages
- Per-scene or scoped bus instances, if a case emerges where global visibility is undesirable
