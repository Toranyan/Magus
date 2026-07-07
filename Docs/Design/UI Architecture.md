# UI Architecture

This document defines the target UI architecture and replaces the current ad-hoc set of stubs under `Assets/Magus/UI` and `Scripts/UI`. It consolidates two half-started foundations (a ToraLib MVP-style view framework and an abandoned UI Toolkit experiment) into one system, and specifies every HUD/menu component needed for the vertical slice.

---

## Current State

The UI layer is the least developed part of the codebase. Almost everything under `Scripts/UI` is an unimplemented Unity template stub (`Start()`/`Update()` empty, or `throw new NotImplementedException()`), and none of it is wired into the battle scene.

| File | State |
|---|---|
| `Scripts/UI/UIManager.cs` | Empty. Two serialized fields (`_mainCanvas`, `_root`), no methods. |
| `Assets/Magus/UI/UIController.cs` | Empty. Duplicate singleton of UIManager — only holds a reference to `UITitle`. |
| `Scripts/UI/UIPopupManager.cs` | Unity template stub. Zero content. |
| `Scripts/UI/UIBattleView.cs` | Unity template stub. Zero content. |
| `Scripts/UI/States/UIBattleStartState.cs` | `OnExit`/`OnUpdate` throw `NotImplementedException`. |
| `Scripts/UI/CastMenu/CastMenuPresenter.cs` | `Init()` calls `_view.Init()` before `_view` is ever assigned — the line that would assign it (`UIManager.Instance.CreateView<UICastMenuView>()`) is commented out because that factory method doesn't exist. This is a guaranteed `NullReferenceException` if ever called. |
| `Scripts/UI/CastMenu/UICastMenuView.cs` | Implemented but never instantiated by anything. |
| `Scripts/UI/UITitle.cs` | **Working.** Wires Play/Options buttons, calls `SceneController.ChangeScene` and `UIAnimatedView.Open()`. The only complete piece of UI in the project. |
| `ToraLib/Scripts/UI/UIStateManager.cs` | `Push`/`Pop` are empty bodies. Zero usages anywhere in the codebase. |
| `Assets/Magus/UI/Test/test.uxml` | An isolated UI Toolkit (UXML) experiment with a `LocalizedString` binding, sitting next to a codebase that otherwise uses `UnityEngine.UI` (uGUI) exclusively. Not referenced by any script. |

Grepping the codebase confirms `UIManager`, `UIController`, `CastMenuPresenter`, and `UIStateManager` have **zero external callers** — they are dead scaffolding, not integrated systems.

`SystemsChecklist.md` already flags the gameplay-facing consequence of this: health bar, mana bar, prepared-spell display, status display, counter prompt, and battle result screen are all unchecked. This document is the actionable spec for closing those items.

---

## Target Layering

```
Unit / PlayerController / BattleFSM   — data & events (gameplay layer, already exists)
    ↓
Presenter                             — subscribes to gameplay events, holds no MonoBehaviour state
    ↓
View (UIViewBase / UIAnimatedView)    — pure display, no gameplay references, exposes Open/Close/Show/Hide
    ↓
UIManager                             — owns canvases, creates/retrieves views, top-level open/close routing
```

A Presenter is the only thing allowed to know about both a View and gameplay data. Views never reach into `Unit`, `PlayerController`, or `BattleController` directly — this is already the intent behind `CastMenuPresenter` / `UICastMenuView`, it just isn't finished or reused anywhere else.

---

## Foundation Layer (`ToraLib/Scripts/UI`)

Keep this layer — it's a reasonable small MVP base. Note this is in the `ToraLib` git submodule (`Toranyan/ToraLib`), not the main repo — changes here need their own commit inside that submodule.

**`IUIView` / `UIViewBase`** — **Reworked.** Both now declare `Opened`/`Closed` events and `UIViewBase` calls four overridable lifecycle hooks:

```csharp
public virtual void Open()
{
    OnPreOpen();
    Show();
    IsOpen = true;
    OnPostOpen();
    RaiseOpened();     // fires the public Opened event
}

public virtual void Close()
{
    OnPreClose();
    Hide();
    IsOpen = false;
    OnPostClose();
    RaiseClosed();     // fires the public Closed event
}

protected virtual void OnPreOpen() { }
protected virtual void OnPostOpen() { }
protected virtual void OnPreClose() { }
protected virtual void OnPostClose() { }
```

`Open()`/`Close()` are now `virtual` so animated/derived views can override the whole sequence (e.g. wait for an animation) while still calling the hooks and raising `Opened`/`Closed` at the right point — `RaiseOpened()`/`RaiseClosed()` are `protected` so a subclass can invoke the base class's event from its own override (C# doesn't allow raising another class's auto-event directly). A Presenter subscribes to `Opened`/`Closed` to know when a view has *actually* finished transitioning, not just when `Open()`/`Close()` was called.

**`UIAnimatedView`** — **Reworked.** It used to declare `Open(Action callback)` / `Close(Action callback)`, which *overloaded* (not overrode) the base class's parameterless `Open()`/`Close()` — meaning `UITitle.OnClickOptionsButton()`'s call to `_optionsWindow.Open()` (the parameterless one) silently skipped the animation entirely and just called the base `Show()`. Now `Open()`/`Close()` are proper `override`s: they call `OnPreOpen()`/`OnPreClose()`, run the animation with `UniTask` (replacing `System.Threading.Tasks.Task`, for consistency with `BattleController`, `DamageIndicator3d`, `FireballSpellExecutor`), then call `OnPostOpen()`/`OnPostClose()` and raise the event once the animation actually finishes.

**`UIStateManager` / `IUIState`** — **Implemented.** It's a back-navigable stack: it wraps a `tora.fsm.StateMachine` for `OnEnter`/`OnExit` dispatch and adds a `Stack<IUIState>` for history, since a plain `StateMachine.SetState()` has no memory of what was active before the last transition.

```csharp
public void Push(IUIState state)
{
    if (CurrentState != null)
        _history.Push(CurrentState);

    state.Init(_fsm);
    _fsm.SetState(state);
}

public void Pop()
{
    if (_history.Count == 0) { Debug.LogWarning(...); return; }
    _fsm.SetState(_history.Pop());
}
```

**`UIStateManager` is now an internal implementation detail — callers talk to `UIManager`, not to it directly.** Originally `UITitle` constructed a hand-written `OptionsUIState` and pushed it straight onto a `UIStateManager` field, while `CastMenuPresenter` (a different call path) opened/closed its view directly through `UIManager`. Two managers, two different ways to "show a screen." Consolidated as follows.

**`ViewState`** (`ToraLib/Scripts/UI/ViewState.cs`, namespace `tora.ui`) — a generic `IUIState` that opens a view on enter, closes it on exit, and turns the view's own `Closed` event into a callback (so the view's own close button pops the stack too, with the same unsubscribe-before-close ordering `OptionsUIState` used to prevent a double-pop). This replaces `OptionsUIState` entirely — it's the same logic, just generic over any `IUIView` instead of hand-written per screen:

```csharp
public class ViewState : State, IUIState
{
    private readonly IUIView _view;
    private readonly Action _onClosedByUser;

    public ViewState(IUIView view, Action onClosedByUser) { ... }

    public override void OnEnter(IState prevState)
    {
        _view.Closed += HandleViewClosedByUser;
        _view.Open();
    }

    public override void OnExit(IState nextState)
    {
        _view.Closed -= HandleViewClosedByUser;
        if (_view.IsOpen) _view.Close();
    }

    private void HandleViewClosedByUser()
    {
        _view.Closed -= HandleViewClosedByUser;
        _onClosedByUser?.Invoke();
    }
}
```

`ViewState` lives in the ToraLib submodule, not `magus.ui` — it only depends on `IUIView`/`State`/`IUIState`, all ToraLib-generic types, no game-specific coupling.

**`UIManager`** now holds the `UIStateManager` reference and exposes it as three methods, alongside the existing `GetOrCreateView`/`Open`/`Close`:

```csharp
public class UIManager : SingletonComponent<UIManager>
{
    [SerializeField] private UIStateManager _stateManager;
    ...

    public void PushView(IUIView view) => _stateManager.Push(new ViewState(view, PopView));
    public void PopView() => _stateManager.Pop();
    public void PushState(IUIState state) => _stateManager.Push(state);   // escape hatch, see below
}
```

Result — `UITitle.OnClickOptionsButton()` is just:

```csharp
private void OnClickOptionsButton() {
    UIManager.Instance.PushView(_optionsWindow);
}
```

No `OptionsUIState` class, no `UIStateManager` field on `UITitle` at all. `TitleUIState` still exists as a real class (not `ViewState`) because it isn't a view — it's an empty placeholder that exists purely so `PopView()` from Options has a root to land on. Seeding it is a scene-bootstrap concern, not `UITitle`'s job, so it lives in `MainBootstrapper` (`Scripts/MainBootstrapper.cs`, namespace `magus`), which also only talks to `UIManager`:

```csharp
public class MainBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        UIManager.Instance.PushState(new TitleUIState());
    }
}
```

Net effect: `UIStateManager` now has exactly one caller in the whole project — `UIManager`. `UITitle` and `MainBootstrapper` no longer reference it, which also means there's only **one** field left that needs manual Editor wiring for the nav stack (`UIManager._stateManager`), not two.

**Open question, still unresolved:** should `CastMenuPresenter` also route through `PushView`/`PopView` (making the cast menu part of back-navigation), or stay on direct `Open()`/`Close()` since it's a battle-HUD toggle rather than a navigable screen? Left as direct `Open()`/`Close()` for now — not changed in this pass.

---

## UIManager (consolidated)

**Namespace:** `magus.ui`
**File:** `Scripts/UI/UIManager.cs` *(rewritten, replaces UIManager + UIController)* — **Done**

One singleton, responsible for owning the canvas/root, creating and tracking view instances, routing direct open/close calls, and — as of the `UIStateManager` consolidation above — the navigation stack too. `Assets/Magus/UI/UIController.cs` has been deleted — it duplicated `UIManager`'s role and lived outside `Scripts/UI` for no reason (every other system's scripts live under `Scripts/<System>`; that was the one folder-placement inconsistency in the project). Its only field, `_uiTitle`, was merged onto `UIManager` as-is.

The implemented version keeps the existing `_mainCanvas` / `_root` fields (already wired in `BattleScene.unity`) rather than introducing the HUD/Popup canvas split below immediately — that split is scene-authoring work, not just code, and is deferred until the HUD views that need it actually exist (see Build Order).

```csharp
public class UIManager : SingletonComponent<UIManager>
{
    [SerializeField] private Canvas _mainCanvas;
    [SerializeField] private Transform _root;
    [SerializeField] private UITitle _uiTitle;
    [SerializeField] private UIStateManager _stateManager;

    private readonly Dictionary<Type, UIViewBase> _views = new();

    public T GetOrCreateView<T>(T prefab) where T : UIViewBase
    {
        if (_views.TryGetValue(typeof(T), out var existing))
            return (T)existing;

        var view = Instantiate(prefab, _root);
        _views[typeof(T)] = view;
        return view;
    }

    public T GetView<T>() where T : UIViewBase { ... }
    public T Open<T>(T prefab) where T : UIViewBase { ... }   // GetOrCreateView + view.Open(), no history
    public void Close<T>() where T : UIViewBase { ... }       // no history

    public void PushView(IUIView view) { ... }   // opens + remembers history
    public void PopView() { ... }
    public void PushState(IUIState state) { ... }

    // type-driven, Addressables-backed - no prefab reference required at the call site
    public async UniTask<T> GetOrLoadViewAsync<T>() where T : UIViewBase { ... }
    public void OpenView<T>() where T : UIViewBase { ... }     // fire-and-forget, no history
    public void PushView<T>() where T : UIViewBase { ... }     // fire-and-forget, with history
}
```

`Open<T>`/`Close<T>` (no history) and `PushView`/`PopView` (with history) are kept as separate, deliberately-named pairs rather than one API — they mean different things (a toggled HUD popup vs. a back-navigable screen), and collapsing them would hide that distinction rather than simplify it. What both share is that neither needs `UIStateManager` directly: `CastMenuPresenter.Init()` (`UIManager.Instance.GetOrCreateView(_viewPrefab)`) — still has zero callers, something needs to construct a `CastMenuPresenter` and drive `Open()`/`Close()` from player input before the cast menu is actually reachable in-game.

### Type-driven views (`OpenView<T>()` / `PushView<T>()`)

`GetOrCreateView<T>(T prefab)` and `PushView(IUIView view)` both require the caller to already hold a reference — either a scene-embedded instance (`_optionsWindow`) or a prefab wired via the Inspector (`CastMenuPresenter`'s constructor). Sometimes there's no reference to hand over at all — the caller just wants "the Title screen" by type. `OpenView<T>()`/`PushView<T>()` cover that case:

```csharp
public async UniTask<T> GetOrLoadViewAsync<T>() where T : UIViewBase
{
    if (_views.TryGetValue(typeof(T), out var existing))
        return (T)existing;

    if (!_pendingLoads.TryGetValue(typeof(T), out var pending))
    {
        pending = LoadViewAsync<T>().Preserve();   // .Preserve() lets multiple concurrent
        _pendingLoads[typeof(T)] = pending;        // callers await the same in-flight load
    }

    return (T)await pending;
}

private async UniTask<UIViewBase> LoadViewAsync<T>() where T : UIViewBase
{
    var prefab = await Addressables.LoadAssetAsync<GameObject>(ViewAddressPrefix + typeof(T).Name);
    var view = Instantiate(prefab, _root).GetComponent<T>();
    _views[typeof(T)] = view;
    return view;
}

public void PushView<T>() where T : UIViewBase => PushViewAsync<T>().Forget();
private async UniTaskVoid PushViewAsync<T>() where T : UIViewBase
{
    PushView(await GetOrLoadViewAsync<T>());
}
```

**Convention: address = path relative to `Assets/Magus/Addressables/`, no extension** — this is `AddressableAutoSetting`'s existing rule (`Assets/Magus/Addressables/Effects/Fire.prefab` → `"Effects/Fire"`), not a new one invented for views. View prefabs live under `Assets/Magus/Addressables/UI/Views/`, so `ViewAddressPrefix = "UI/Views/"` and a view type's address is `"UI/Views/" + typeof(T).Name` (e.g. `UITitle.prefab` at `Addressables/UI/Views/UITitle.prefab` → `"UI/Views/UITitle"`). No attribute or lookup table needed as long as every view prefab's filename matches its component's class name, which is already true for every view in the project.

Follows the codebase's existing async convention exactly (`ObjectPoolManager.GetPrefab` already does `await Addressables.LoadAssetAsync<GameObject>(id)`; `MeleeAttackComponent`/`ProjectileAttackComponent` already expose a synchronous-looking method that calls a private `XxxAsync()` and `.Forget()`s it) — nothing new introduced, just applied to views.

**`UITitle.prefab` migrated** — moved from `Assets/Magus/UI/Title/UITitle.prefab` to `Assets/Magus/Addressables/UI/Views/UITitle.prefab` (file move, GUID preserved, so the existing prefab-instance link in `Title.unity` isn't affected; originally landed at `Addressables/UI/UITitle.prefab` and was moved one level deeper into a `Views/` subfolder). `AddressableAutoSetting`'s `AssetPostprocessor` should pick up the move automatically and assign it address `"UI/Views/UITitle"` the next time the Editor's asset database refreshes; if it doesn't fire on its own, run **Magus > Addressables > Apply Auto Addressing** manually. Nothing calls `PushView<UITitle>()`/`OpenView<UITitle>()` yet — this just makes it possible.

**Not yet migrated:** `OptionsWindow` and `CastMenu` are still referenced as direct instances (`_optionsWindow`, `CastMenuPresenter`'s constructor param) rather than through the type-driven path. `_optionsWindow` in particular is a pre-placed scene instance, not an Addressable prefab — moving it over would mean changing OptionsWindow from "always in the scene, hidden" to "instantiated on demand," a real behavior change, not just a refactor, and hasn't been decided. (Note: `Assets/Magus/Addressables/UI/CastMenu.prefab` already exists as a separate copy with its own GUID, distinct from `Assets/Magus/UI/CastMenu/CastMenu.prefab` — not touched here, flagging in case it wasn't intentional.)

---

## Canvas Plan

Target: two `Screen Space - Overlay` canvases in the battle scene, sort order separates them. **Not yet split** — see note above.

| Canvas | Sort Order | Contents |
|---|---|---|
| HUD | 0 | Player health bar, mana bar, prepared spell slots, active status icons |
| Popup | 10 | Cast menu, options dialog, battle result screen |

**Enemy health bars and floating damage numbers are not on these canvases.** See below.

---

## Floating Combat Text — pick one implementation

Two parallel implementations of the same feature exist today and only one should survive:

| | `DamageIndicator` + `DamageIndicatorManager` | `DamageIndicator3d` |
|---|---|---|
| Approach | uGUI `TextMeshProUGUI` on the HUD canvas; manager does `WorldToScreenPoint` → `ScreenPointToLocalPointInRectangle` every hit | World-space `TextMeshPro`, billboarded to camera each frame |
| Cost | Screen/canvas-space math per spawn, canvas rebuild per instance | Just a `transform.Lerp`, no canvas rebuild |
| Scales to many enemies | Worse (canvas layout cost) | Better |

**Recommendation: keep `DamageIndicator3d`, delete `DamageIndicator` and the screen-projection code in `DamageIndicatorManager`.** Repoint the pool in `DamageIndicatorManager` at `DamageIndicator3d` and drop the `_canvas` / `RectTransformUtility` path entirely. This also sets the pattern for enemy health bars (below), which need the same world-space, camera-facing approach and shouldn't use a second, different technique.

---

## HUD Components Needed

### PlayerHealthBarView

**Namespace:** `magus.ui` · **File:** `Scripts/UI/HUD/PlayerHealthBarView.cs` *(new)*

Binds to `Unit.CurrentHp` / `Unit.MaxHp`. **Blocked on a data gap:** `Unit` fires `ManaChanged` on mana changes but has no equivalent `HpChanged` event — `ReceiveDamage()` and `Heal()` mutate `CurrentHp` silently. Add:

```csharp
// Unit.cs
public event Action<float> HpChanged;

// in ReceiveDamage, after CurrentHp is updated:
HpChanged?.Invoke(CurrentHp);
// in Heal, after CurrentHp is updated:
HpChanged?.Invoke(CurrentHp);
```

Without this, the view has to poll every frame instead of reacting to the event — inconsistent with how mana is already done.

### PlayerManaBarView

**Namespace:** `magus.ui` · **File:** `Scripts/UI/HUD/PlayerManaBarView.cs` *(new)*

Binds to `Unit.ManaChanged` — event already exists, no data-layer changes needed.

### PreparedSpellSlotsView

**Namespace:** `magus.ui` · **File:** `Scripts/UI/HUD/PreparedSpellSlotsView.cs` *(new)*

Shows the 3 equipped spells (icon, cooldown sweep, mana cost). **Blocked on a data gap:** `PlayerController._preparedSpells` is a private array with no accessor and no change event — `SetSpell()` mutates it silently. Add:

```csharp
// PlayerController.cs
public event Action<int, UnitSpellInstance> SpellSlotChanged;
public UnitSpellInstance GetSpell(int index) => _preparedSpells[index];

// in SetSpell, after assignment:
SpellSlotChanged?.Invoke(index, spell);
```

Cooldown display reads `UnitSpellInstance.CooldownRemaining` / `IsReady` each frame — those are already public, no change needed there.

### EnemyHealthBarView (world-space)

**Namespace:** `magus.ui` · **File:** `Scripts/UI/HUD/EnemyHealthBarView.cs` *(new)*

Same billboard-to-camera pattern as `DamageIndicator3d`, attached above each enemy, bound to that enemy's `Unit.HpChanged` (once added). Not on the HUD canvas — either a `World Space` canvas per enemy or a non-canvas sprite/shader-based bar. Given the project has no more than a handful of enemies on screen at once for the vertical slice, a small `World Space` canvas per enemy prefab is acceptable; revisit only if profiling shows a problem.

### StatusEffectView

**Blocked on the Status Effect system itself**, which `SystemsChecklist.md` lists as entirely missing (no base class, no handler component). Do not build this view until `StatusEffectBase`/`StatusHandler` exist — building the UI first means guessing the data shape.

### CounterPromptView

**Blocked on the Counter mechanic**, also listed as entirely missing in `SystemsChecklist.md` (no counterable-window flag on incoming attacks, no resolution logic). Same reasoning — the UI needs a concrete signal (e.g. `Unit.CounterWindowOpened(ElementType incomingElement)`) to bind to, which doesn't exist yet.

### BattleResultView

**Blocked on `BattleFSM`**, which only has the `Init` state stubbed (`Battle` and `Result` states are unimplemented, per `SystemsChecklist.md`). `UIBattleStartState.OnExit`/`OnUpdate` currently `throw NotImplementedException` — implement this alongside the `BattleFSM.Result` state, not before it, since the transition trigger doesn't exist yet.

---

## Namespace and Folder Cleanup

Two inconsistencies to fix while touching this area:

1. `Scripts/Battle/BattleFSM.cs` is in namespace `App.Battle` — every other file in the project uses `magus.*` (`magus.battle`, `magus.chara`, `magus.master`, `magus.ui`). Rename to `magus.battle`.
2. `Assets/Magus/UI/UIController.cs` lives outside `Scripts/UI` — move/merge into `Scripts/UI/UIManager.cs` per the consolidation above. Non-script UI assets (prefabs, sprites, animations) stay under `Assets/Magus/UI/*`; that split itself is fine and matches the rest of the project.
3. `DamageIndicator.cs` has a stray unused `using Unity.VisualScripting.Antlr3.Runtime;` — remove it (dead import, unrelated to the file's content). Moot if `DamageIndicator` itself is deleted per the recommendation above.

---

## UI Toolkit vs uGUI — Decision

**Standardize on uGUI (`UnityEngine.UI` + TextMeshPro).** Every working piece of UI (`UITitle`, `OptionsWindow`, `CastMenu`) is built on uGUI; `test.uxml` was the only UI Toolkit artifact in the project and had no runtime binding code anywhere. **Done** — `Assets/Magus/UI/Test/` deleted.

---

## What to Delete / Rework

| Item | Action | Status |
|---|---|---|
| `Assets/Magus/UI/UIController.cs` | Delete — merged into `UIManager` | **Done** |
| `Scripts/UI/UIPopupManager.cs` | Delete — unused template stub, no design calls for a generic popup manager beyond the Popup canvas above | Open |
| `Scripts/UI/UIBattleView.cs` | Delete — unused template stub, superseded by the specific HUD views above | Open |
| `ToraLib/Scripts/UI/UIStateManager.cs` | Implement (menu nav stack) or delete — currently dead | **Done** — `Push`/`Pop` implemented; now only called from `UIManager` |
| `ToraLib/Scripts/UI/UIDialogBase.cs` | Fix — `m_buttonOK`/`m_buttonClose` are serialized but `Start()` never wires their `onClick`, only `m_buttonResponse[]` | **Done** |
| `ToraLib/Scripts/UI/ViewState.cs` | New — generic `IUIState` that opens/closes a view, used by `UIManager.PushView` | **Done** |
| `Scripts/UI/States/OptionsUIState.cs` | Deleted — superseded by generic `ViewState` | **Done** |
| `Scripts/Battle/DamageIndicator/DamageIndicator.cs` | Delete — superseded by `DamageIndicator3d` | **Done** (prefab `UI/Damage/DamageIndicator.prefab` deleted too) |
| `Scripts/Battle/DamageIndicator/DamageIndicatorManager.cs` | Rework — pool `DamageIndicator3d` instead, remove screen-projection code | **Done** |
| `ToraLib/Scripts/UI/UIAnimatedView.cs` | Rework — override base `Open`/`Close` instead of overloading, switch `Task` → `UniTask` | **Done** — also fixed a latent bug where `UITitle`'s call to `_optionsWindow.Open()` resolved to the non-animated base overload and never played the animation |
| `Scripts/Battle/BattleFSM.cs` | Rename namespace `App.Battle` → `magus.battle` | Open |
| `Assets/Magus/UI/Test/test.uxml` | Delete or relocate out of the live asset tree | **Done** — deleted |
| `Scripts/UI/CastMenu/CastMenuPresenter.cs` | Fix — assign `_view` via `UIManager.GetOrCreateView` before calling `_view.Init()` | **Done** — prefab now passed in via constructor; still has zero callers, needs a MonoBehaviour to construct and drive it |
| `Scripts/Battle/Unit.cs` | Add `HpChanged` event | Open |
| `Scripts/Chara/PlayerController.cs` | Add `SpellSlotChanged` event + `GetSpell(int)` accessor | Open |
| `ToraLib/Scripts/UI/UIViewBase.cs` / `IUIView.cs` | Add `OnPreOpen`/`OnPostOpen`/`OnPreClose`/`OnPostClose` overridable hooks + `Opened`/`Closed` events | **Done** |
| `Scripts/UI/UIManager.cs` | Implement view registry + `GetOrCreateView<T>`/`GetView<T>`/`Open<T>`/`Close<T>` | **Done** |

---

## Data Flow Example — Player takes damage, HUD updates

```
1. Enemy projectile hits DamageReceiver
2. DamageReceiver.Damage(info) calls Unit.ReceiveDamage(info)
3. Unit.ReceiveDamage updates CurrentHp, fires HpChanged(CurrentHp)   [new event]
4. PlayerHealthBarView, subscribed in its Presenter's Init, receives HpChanged
5. PlayerHealthBarView updates fill amount: CurrentHp / MaxHp
6. DamageReceiver also fires DamageReceived + GlobalDamageReceived
7. DamageIndicatorManager.OnGlobalDamageReceived spawns a DamageIndicator3d
   at the hit position, billboarded, fades and rises over its lifetime
```

---

## Build Order

Follow dependency order, not visual priority — several HUD pieces are blocked on gameplay systems that don't exist yet:

1. ~~`UIManager` consolidation~~ **Done** (view registry + `GetOrCreateView`/`Open`/`Close`); canvas split (HUD/Popup) still pending, deferred to step 5/6
2. ~~`DamageIndicatorManager` rework onto `DamageIndicator3d`~~ **Done**; `EnemyHealthBarView` (same billboard pattern) still pending
3. ~~`CastMenuPresenter` fix~~ **Done** (constructor takes the view prefab, calls `UIManager.GetOrCreateView`); still needs a caller — nothing constructs a `CastMenuPresenter` or drives it from player input yet
4. ~~`UIStateManager` navigation stack~~ **Done** (Title ↔ Options), consolidated behind `UIManager.PushView`/`PopView`/`PushState`; root state seeded by `MainBootstrapper`. Needs a `UIStateManager` + `MainBootstrapper` added to the Title scene, with the `UIStateManager` assigned to `UIManager._stateManager` in the Editor (the only manual wiring point left for this feature)
5. `Unit.HpChanged` + `PlayerHealthBarView`
6. `PlayerManaBarView` (data already exists)
7. `PlayerController.SpellSlotChanged` + `PreparedSpellSlotsView`
8. `StatusEffectView` — only after the Status Effect system lands
9. `CounterPromptView` — only after the Counter mechanic lands
10. `BattleResultView` — only after `BattleFSM.Result` state lands
