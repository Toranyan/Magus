using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using tora.graph.editor;

namespace magus.story.editor
{
    public class StoryGraphEditorWindow : GraphEditorWindow<StoryGraph, StoryNode>
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is StoryGraph graph)
            {
                var window = GetWindow<StoryGraphEditorWindow>("Story Graph");
                window.Load(graph);
                return true;
            }
            return false;
        }

        protected override string GetNodeTitle(StoryNode node)
        {
            return string.IsNullOrEmpty(node.Name) ? node.Id : node.Name;
        }

        protected override Dictionary<string, Type> GetSerializeReferenceListFields()
        {
            return new Dictionary<string, Type>
            {
                [nameof(StoryNode.Conditions)] = typeof(IStoryCondition),
                [nameof(StoryNode.Actions)] = typeof(IStoryAction)
            };
        }
    }
}
