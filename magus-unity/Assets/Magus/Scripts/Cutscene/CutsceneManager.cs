using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Cysharp.Threading.Tasks;
using tora.singleton;
using tora.eventbus;
using magus.story;
using magus.chara;
using magus.dialogue;

namespace magus.cutscene
{
    /// <summary>Thin trigger around Unity's own Timeline package (already installed in this
    /// project) - not a custom sequencer. A cutscene is an Addressable prefab that's fully
    /// self-contained: PlayableDirector + its TimelineAsset + whatever actors/camera the
    /// timeline tracks are bound to inside the prefab itself. Author animation/camera moves/
    /// fades as Timeline tracks in Unity's own Timeline editor, not as new IStoryAction types.
    ///
    /// Deliberately not an ISaveParticipant: resuming a half-played cutscene after a reload
    /// isn't an established need (same reasoning DialogueManager's mid-conversation resume
    /// was left unresolved) - add it if that ever becomes a real requirement.</summary>
    public class CutsceneManager : SingletonComponent<CutsceneManager>
    {
        /// <summary>Actor name BattleController registers the live player under - see
        /// RegisterActor and BattleController.LoadPlayerAsync.</summary>
        public const string PlayerActorName = "Player";

        [SerializeField]
        private Transform _cutsceneRoot;

        private readonly Dictionary<string, GameObject> _actors = new Dictionary<string, GameObject>();

        private PlayableDirector _activeDirector;
        private GameObject _activeInstance;
        private string _activeCutsceneAddress;
        private bool _timelineStopped;
        private bool _waitingForDialogue;
        private bool _timelinePausedForDialogue;

        public bool IsPlaying => _activeDirector != null && _activeDirector.state == PlayState.Playing;

        public event Action Ended;

        private void Awake()
        {
            EventBus.Subscribe<TimelineRequestedEvent>(OnTimelineRequested);
            EventBus.Subscribe<ScreenFadeRequestedEvent>(OnScreenFadeRequested);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TimelineRequestedEvent>(OnTimelineRequested);
            EventBus.Unsubscribe<ScreenFadeRequestedEvent>(OnScreenFadeRequested);
        }

        private void OnTimelineRequested(TimelineRequestedEvent e)
        {
            Play(e.CutsceneAddress);
        }

        /// <summary>ScreenFadeAction's handler - lives here rather than on ScreenFader itself
        /// because ScreenFader is created lazily (see SingletonComponent), so it may not exist
        /// yet to subscribe on its own.</summary>
        private void OnScreenFadeRequested(ScreenFadeRequestedEvent e)
        {
            FadeAsync(e.TargetAlpha, e.Duration).Forget();
        }

        private async UniTask FadeAsync(float targetAlpha, float duration)
        {
            await ScreenFader.Instance.FadeAsync(targetAlpha, duration);
            EventBus.Publish(new ScreenFadeFinishedEvent());
        }

        /// <summary>Registers a live, already-spawned object (the player, an NPC) under a
        /// name a cutscene's Timeline tracks can reference. Rename a track to match this
        /// name in Unity's Timeline window and it gets bound automatically before Play() -
        /// see BindActors. Typically called once, right after the object is spawned (e.g.
        /// BattleController.LoadPlayerAsync); re-registering the same name overwrites it.</summary>
        public void RegisterActor(string name, GameObject actor)
        {
            _actors[name] = actor;
        }

        public void UnregisterActor(string name)
        {
            _actors.Remove(name);
        }

        public void Play(string cutsceneAddress)
        {
            PlayAsync(cutsceneAddress).Forget();
        }

        private async UniTask PlayAsync(string cutsceneAddress)
        {
            Stop();

            var prefab = await Addressables.LoadAssetAsync<GameObject>(cutsceneAddress);
            if (prefab == null)
            {
                Debug.LogError($"[CutsceneManager] Failed to load cutscene at '{cutsceneAddress}'.");
                return;
            }

            _activeCutsceneAddress = cutsceneAddress;
            _timelineStopped = false;
            _waitingForDialogue = false;
            _timelinePausedForDialogue = false;

            _activeInstance = _cutsceneRoot != null ? Instantiate(prefab, _cutsceneRoot) : Instantiate(prefab);
            _activeDirector = _activeInstance.GetComponent<PlayableDirector>();

            if (_activeDirector == null)
            {
                Debug.LogError($"[CutsceneManager] Cutscene prefab '{cutsceneAddress}' has no PlayableDirector.");
                Destroy(_activeInstance);
                _activeInstance = null;
                _activeCutsceneAddress = null;
                return;
            }

            // A prefab left on Play On Awake has already built its graph and started
            // playing during Instantiate - before any actor is bound. Reset it so Play()
            // below rebuilds the graph with the bindings, from time 0.
            if (_activeDirector.state == PlayState.Playing)
            {
                _activeDirector.Stop();
            }

            BindActors(_activeDirector);
            SetPlayerInputEnabled(false);

            _activeDirector.stopped += OnDirectorStopped;
            _activeDirector.Play();
        }

        /// <summary>DialogueCutsceneSignal calls this instead of DialogueManager.Play()
        /// directly, so a dialogue started mid-cutscene is tracked here too - the cutscene
        /// isn't considered finished (CutsceneFinishedEvent doesn't fire) until this
        /// dialogue ends as well, even if the Timeline itself has already stopped.
        ///
        /// pauseTimeline freezes the Timeline at the signal until the dialogue ends, then
        /// resumes it - so one Timeline can hold several animate -> talk -> animate beats,
        /// with every animated actor holding its current pose while the dialogue is up.
        /// Without it, the Timeline keeps playing underneath the dialogue.</summary>
        public void PlayDialogueDuringCutscene(string dialogueAddress, bool pauseTimeline = false)
        {
            if (!_waitingForDialogue)
            {
                _waitingForDialogue = true;
                DialogueManager.Instance.Ended += OnCutsceneDialogueEnded;
            }

            if (pauseTimeline)
            {
                SetTimelinePaused(true);
            }

            DialogueManager.Instance.Play(dialogueAddress);
        }

        private void OnCutsceneDialogueEnded()
        {
            DialogueManager.Instance.Ended -= OnCutsceneDialogueEnded;
            _waitingForDialogue = false;
            SetTimelinePaused(false);
            TryFinishCutscene();
        }

        /// <summary>Pauses via the root playable's speed rather than PlayableDirector.Pause():
        /// at speed 0 the graph keeps evaluating the same frame every update, so Animation
        /// Tracks keep holding their pose (Pause() stops evaluation, which can let an
        /// Animator fall back to its own controller mid-cutscene).</summary>
        private void SetTimelinePaused(bool paused)
        {
            if (_timelinePausedForDialogue == paused)
            {
                return;
            }
            _timelinePausedForDialogue = paused;

            if (_activeDirector == null || !_activeDirector.playableGraph.IsValid())
            {
                return;
            }

            _activeDirector.playableGraph.GetRootPlayable(0).SetSpeed(paused ? 0d : 1d);
        }

        /// <summary>Binds any track whose name matches a registered actor to that actor's
        /// live GameObject (or the specific component the track actually needs, e.g. an
        /// Animation Track wants an Animator, an Activation Track wants the GameObject
        /// itself) - the runtime equivalent of dragging an object onto a track's binding
        /// slot in the Inspector, which isn't possible here since the actor doesn't exist
        /// yet when the cutscene prefab is authored. Tracks with no matching registered
        /// actor are left alone, keeping whatever binding the prefab itself authored (e.g.
        /// an object that's part of the cutscene prefab, not a live registered actor).
        /// GetOutputTracks() also walks into Group Tracks, so grouped tracks bind too.</summary>
        private void BindActors(PlayableDirector director)
        {
            if (director.playableAsset is not TimelineAsset timeline)
            {
                return;
            }

            foreach (var track in timeline.GetOutputTracks())
            {
                if (!_actors.TryGetValue(track.name, out var actor) || actor == null)
                {
                    continue;
                }

                var target = ResolveBindingTarget(track, actor);
                if (target != null)
                {
                    director.SetGenericBinding(track, target);
                }
                else
                {
                    Debug.LogWarning($"[CutsceneManager] Actor '{track.name}' has no component matching what its track needs.");
                }
            }
        }

        private static UnityEngine.Object ResolveBindingTarget(TrackAsset track, GameObject actor)
        {
            foreach (var binding in track.outputs)
            {
                if (binding.outputTargetType == typeof(GameObject))
                {
                    return actor;
                }

                // InChildren: a character's Animator usually sits on its model child, not
                // the root that gets registered (e.g. pc_test_01 -> PlayerCharacter).
                var component = actor.GetComponentInChildren(binding.outputTargetType, true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private void OnDirectorStopped(PlayableDirector director)
        {
            director.stopped -= OnDirectorStopped;
            _timelineStopped = true;
            TryFinishCutscene();
        }

        /// <summary>Only actually finishes once the Timeline has stopped AND (if a
        /// DialogueCutsceneSignal fired during it) that dialogue has also ended - order
        /// doesn't matter, whichever finishes last calls this and it proceeds.</summary>
        private void TryFinishCutscene()
        {
            if (!_timelineStopped || _waitingForDialogue)
            {
                return;
            }

            Stop();
        }

        /// <summary>Stops immediately, whether the cutscene finished naturally or is being
        /// interrupted (including by a new Play() call). Always publishes
        /// CutsceneFinishedEvent if one was active, so a StoryNode's StartTimelineAction
        /// waiting on it never gets stuck, even on a manual/external Stop().</summary>
        public void Stop()
        {
            var finishedAddress = _activeCutsceneAddress;

            SetPlayerInputEnabled(true);

            if (_waitingForDialogue)
            {
                DialogueManager.Instance.Ended -= OnCutsceneDialogueEnded;
                _waitingForDialogue = false;
            }

            if (_activeDirector != null)
            {
                _activeDirector.stopped -= OnDirectorStopped;
                _activeDirector.Stop();
            }

            if (_activeInstance != null)
            {
                Destroy(_activeInstance);
            }

            _activeDirector = null;
            _activeInstance = null;
            _activeCutsceneAddress = null;
            _timelineStopped = false;
            _timelinePausedForDialogue = false;

            if (finishedAddress != null)
            {
                EventBus.Publish(new CutsceneFinishedEvent { CutsceneAddress = finishedAddress });
                Ended?.Invoke();
            }
        }

        private void SetPlayerInputEnabled(bool enabled)
        {
            if (_actors.TryGetValue(PlayerActorName, out var player) && player != null)
            {
                var controller = player.GetComponent<PlayerController>();
                if (controller == null)
                {
                    return;
                }

                if (enabled)
                {
                    controller.RemoveInputLock(this);
                }
                else
                {
                    controller.AddInputLock(this);
                }
            }
        }
    }
}
