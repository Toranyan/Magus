using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace magus.addressables
{
    /// <summary>
    /// Editor utility to mark assets under `Assets/Magus/Addressables` as addressable.
    /// Each asset will get an address equal to its path relative to that folder (without extension).
    /// Example: `Assets/Magus/Addressables/Effects/Fire.prefab` -> `Effects/Fire`
    /// Also sets a label on the entry derived from the address (slashes replaced with underscores).
    /// </summary>
    public static class AddressableAutoSetting
    {
        private const string SourceFolder = "Assets/Magus/Addressables/";

        // Reentrancy guard so we don't react to changes caused by ApplyAutoAddressing itself
        private static bool _isAutoApplying = false;

        [MenuItem("Magus/Addressables/Apply Auto Addressing")]
        public static void ApplyAutoAddressing()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found. Make sure Addressables is installed and configured.");
                return;
            }

            // Choose the group to put entries in. Use the default group to avoid creating many groups.
            var group = settings.DefaultGroup;
            if (group == null)
            {
                Debug.LogError("Default Addressables group not found.");
                return;
            }

            // Find all assets under the folder
            var guids = AssetDatabase.FindAssets("", new[] { SourceFolder });
            int processed = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                var path = AssetDatabase.GUIDToAssetPath(guid);

                // Skip folders
                if (AssetDatabase.IsValidFolder(path))
                    continue;

                // Skip some file types that shouldn't be addressable
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".cs" || ext == ".meta" || ext == ".asmdef" || ext == ".unity" || ext == ".dll")
                    continue;

                // Compute relative address
                if (!path.StartsWith(SourceFolder))
                    continue;

                var relative = path.Substring(SourceFolder.Length);
                // Remove extension
                var address = Path.ChangeExtension(relative, null).Replace("\\", "/");

                // Create or find entry
                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    entry = settings.CreateOrMoveEntry(guid, group, false, false);
                }
                if (entry != null)
                {
                    if (entry.address != address)
                    {
                        entry.address = address;
                        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                    }

                    // set a label derived from the address directory (omit the filename)
                    string dirPart;
                    int lastSlash = address.LastIndexOf('/');
                    if (lastSlash >=0)
                        dirPart = address.Substring(0, lastSlash);
                    else
                        dirPart = string.Empty;

                    var label = string.IsNullOrEmpty(dirPart) ? "root" : dirPart;
                    // ensure label exists on the entry
                    // force:true registers the label in AddressableAssetSettings' global label table -
                    // without it, SetLabel only takes effect for labels that already happen to be registered,
                    // and unregistered labels get silently dropped from the built content catalog.
                    entry.SetLabel(label, true, force: true);

                    processed++;
                }
            }

            // Save settings
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true);
            AssetDatabase.SaveAssets();

            Debug.Log($"AddressableAutoSetting: processed {processed} assets under '{SourceFolder}'");
        }

        // AssetPostprocessor implementation to auto-run the tool when assets change under the SourceFolder
        private class AutoAddressPostprocessor : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                // quick check: if we're already applying, bail out
                if (_isAutoApplying)
                    return;

                // Check lists for any path under SourceFolder
                bool shouldRun = false;

                foreach (var p in importedAssets)
                {
                    if (IsUnderSourceFolder(p)) { shouldRun = true; break; }
                }
                if (!shouldRun)
                {
                    foreach (var p in deletedAssets)
                    {
                        if (IsUnderSourceFolder(p)) { shouldRun = true; break; }
                    }
                }
                if (!shouldRun)
                {
                    foreach (var p in movedAssets)
                    {
                        if (IsUnderSourceFolder(p)) { shouldRun = true; break; }
                    }
                }
                if (!shouldRun)
                {
                    foreach (var p in movedFromAssetPaths)
                    {
                        if (IsUnderSourceFolder(p)) { shouldRun = true; break; }
                    }
                }

                if (!shouldRun)
                    return;

                // Schedule to run on the next editor loop to avoid interfering with the import pipeline
                EditorApplication.delayCall += () =>
                {
                    if (_isAutoApplying)
                        return;

                    try
                    {
                        _isAutoApplying = true;
                        ApplyAutoAddressing();
                    }
                    finally
                    {
                        _isAutoApplying = false;
                    }
                };
            }

            private static bool IsUnderSourceFolder(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                return path.StartsWith(SourceFolder, System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
