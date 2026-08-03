using System;
using System.Collections.Generic;
using UnityEngine;
using tora.singleton;
using tora.eventbus;
using tora.save;

namespace magus.story
{
    public class StoryManager : SingletonComponent<StoryManager>, ISaveParticipant
    {
        [SerializeField]
        private StoryGraph _graph;

        private readonly StoryBlackboard _blackboard = new StoryBlackboard();
        private readonly HashSet<string> _completedNodeIds = new HashSet<string>();
        private readonly HashSet<string> _frontier = new HashSet<string>();

        public string CurrentChapter { get; private set; }

        public string SaveKey => "story";

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        public void Initialize()
        {
            SaveSystem.Register(this);
            EventBus.Subscribe<StoryEvent>(RaiseEvent);
            EventBus.Subscribe<DialogueChoiceMadeEvent>(OnDialogueChoiceMade);
        }

        public void Shutdown()
        {
            EventBus.Unsubscribe<DialogueChoiceMadeEvent>(OnDialogueChoiceMade);
            EventBus.Unsubscribe<StoryEvent>(RaiseEvent);
            SaveSystem.Unregister(this);
        }

        /// <summary>Dialogue never writes StoryBlackboard directly - it publishes what
        /// happened, and StoryManager (which owns the blackboard) does the actual write.
        /// See Docs/Design/DialogueSystem.md#dialogue-data.</summary>
        private void OnDialogueChoiceMade(DialogueChoiceMadeEvent e)
        {
            _blackboard.SetVariable(e.VariableKey, StoryVariable.FromString(e.Value));
            Evaluate();
        }

        public void RaiseEvent(StoryEvent e)
        {
            Evaluate();
        }

        public void Save()
        {
            SaveSystem.Save();
        }

        public void Load()
        {
            SaveSystem.Load();
            RebuildFrontier();
            Evaluate();
        }

        public void ResetProgress()
        {
            _completedNodeIds.Clear();
            _blackboard.RestoreSnapshot(null);
            CurrentChapter = null;
            RebuildFrontier();
            Evaluate();
        }

        public void AdvanceChapter(string chapterId)
        {
            CurrentChapter = chapterId;
            EventBus.Publish(new ChapterAdvancedEvent { ChapterId = chapterId });
        }

        /// <summary>Re-checks every node currently eligible for evaluation (the frontier) and
        /// completes any whose conditions now pass, expanding the frontier to their children,
        /// until a full pass produces no further completions.</summary>
        public void Evaluate()
        {
            if (_graph == null)
            {
                return;
            }

            var changed = true;
            var safety = 0;

            while (changed && safety++ < 1000)
            {
                changed = false;

                var candidates = new List<StoryNode>();
                foreach (var id in _frontier)
                {
                    var node = _graph.GetNode(id);
                    if (node != null)
                    {
                        candidates.Add(node);
                    }
                }
                candidates.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                foreach (var node in candidates)
                {
                    if (_completedNodeIds.Contains(node.Id) || !node.EvaluateConditions(_blackboard))
                    {
                        continue;
                    }

                    CompleteNode(node);
                    changed = true;
                }
            }
        }

        private void CompleteNode(StoryNode node)
        {
            node.ExecuteActions(_blackboard);
            _completedNodeIds.Add(node.Id);
            _frontier.Remove(node.Id);

            foreach (var childId in node.ChildIds)
            {
                if (!_completedNodeIds.Contains(childId))
                {
                    _frontier.Add(childId);
                }
            }
        }

        /// <summary>Recomputes the frontier from scratch: root nodes (no incoming edges) plus
        /// the not-yet-completed children of any already-completed node. Called after Load/
        /// ResetProgress since the in-memory frontier isn't itself part of save data.</summary>
        private void RebuildFrontier()
        {
            _frontier.Clear();

            if (_graph == null)
            {
                return;
            }

            var hasParent = new HashSet<string>();
            foreach (var node in _graph.Nodes)
            {
                foreach (var childId in node.ChildIds)
                {
                    hasParent.Add(childId);
                }
            }

            foreach (var node in _graph.Nodes)
            {
                if (_completedNodeIds.Contains(node.Id))
                {
                    continue;
                }

                if (!hasParent.Contains(node.Id) || HasCompletedParent(node.Id))
                {
                    _frontier.Add(node.Id);
                }
            }
        }

        private bool HasCompletedParent(string nodeId)
        {
            foreach (var node in _graph.Nodes)
            {
                if (_completedNodeIds.Contains(node.Id) && node.ChildIds.Contains(nodeId))
                {
                    return true;
                }
            }
            return false;
        }

        public string CaptureState()
        {
            var data = new StoryManagerSaveData
            {
                CompletedNodeIds = new List<string>(_completedNodeIds),
                Blackboard = _blackboard.CaptureSnapshot(),
                CurrentChapter = CurrentChapter
            };
            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string json)
        {
            _completedNodeIds.Clear();

            if (string.IsNullOrEmpty(json))
            {
                _blackboard.RestoreSnapshot(null);
                CurrentChapter = null;
                return;
            }

            var data = JsonUtility.FromJson<StoryManagerSaveData>(json);

            foreach (var id in data.CompletedNodeIds)
            {
                _completedNodeIds.Add(id);
            }
            _blackboard.RestoreSnapshot(data.Blackboard);
            CurrentChapter = data.CurrentChapter;
        }

        [Serializable]
        private class StoryManagerSaveData
        {
            public List<string> CompletedNodeIds = new List<string>();
            public StoryBlackboardSnapshot Blackboard = new StoryBlackboardSnapshot();
            public string CurrentChapter;
        }
    }
}
