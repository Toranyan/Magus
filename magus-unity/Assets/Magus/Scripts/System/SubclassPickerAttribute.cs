using UnityEngine;

namespace magus
{
    /// <summary>Put next to [SerializeReference] on an interface/abstract-typed field or list
    /// (e.g. List&lt;IStoryCondition&gt;) to get a type dropdown in the Inspector - Unity's
    /// default Inspector can't create instances for those. Drawn by SubclassPickerDrawer.</summary>
    public class SubclassPickerAttribute : PropertyAttribute
    {
    }
}
