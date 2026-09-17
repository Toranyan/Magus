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

        public bool IsPlaying => _activeDirector != null && _activeDirector.state == PlayState.Playing;

        public event Action Ended;

        private void Awake()
        {
            EventBus.Subscribe<TimelineRequestedEvent>(OnTimelineRequested);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TimelineRequestedEvent>(OnTimelineRequested);
        }

        private void OnTimelineRequested(TimelineRequestedEvent e)
        {
            Play(e.CutsceneAddress);
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

            _activeInstance = _cutsceneRoot != null ? Instantiate(prefab, _cutsceneRoot) : Instantiate(prefab);
            _activeDirector = _activeInstance.GetComponent<PlayableDirector>();

            if (_activeDirector == null)
            {
                Debug.LogError($"[CutsceneManager] Cutscene prefab '{cutsceneAddress}' has no PlayableDirector.");
                Destroy(_activeInstance);
                _activeInstance = null;
                return;
            }

            BindActors(_activeDirector);
            SetPlayerInputEnabled(false);

            _activeDirector.stopped += OnDirectorStopped;
            _activeDirector.Play();
        }

        /// <summary>Binds any track whose name matches a registered actor to that actor's
        /// live GameObject (or the specific component the track actually needs, e.g. an
        /// Animation Track wants an Animator, an Activation Track wants the GameObject
        /// itself) - the runtime equivalent of dragging an object onto a track's binding
        /// slot in the Inspector, which isn't possible here since the actor doesn't exist
        /// yet when the cutscene prefab is authored. Tracks with no matching registered
        /// actor are left alone, keeping whatever binding the prefab itself authored (e.g.
        /// an object that's part of the cutscene prefab, not a live registered actor).
        /// Only resolves root-level tracks - nested tracks inside a Group Track aren't
        /// covered yet.</summary>
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

                var component = actor.GetComponent(binding.outputTargetType);
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
            Stop();
            Ended?.Invoke();
        }

        public void Stop()
        {
            SetPlayerInputEnabled(true);

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
        }

        private void SetPlayerInputEnabled(bool enabled)
        {
            if (_actors.TryGetValue(PlayerActorName, out var player) && player != null)
            {
                player.GetComponent<PlayerController>()?.SetInputEnabled(enabled);
            }
        }
    }
}
