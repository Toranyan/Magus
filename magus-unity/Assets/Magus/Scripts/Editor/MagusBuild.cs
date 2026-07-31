using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace magus.build
{
    /// <summary>
    /// Adds build commands to the Unity menu and implements the shared build pipeline.
    /// See Docs/Design/Build System.md for the full specification.
    /// </summary>
    public static class MagusBuild
    {
        private const string ProfilesFolder = "Assets/Magus/Build/Profiles";
        private const string OutputRoot = "Build/Output";

        // ---- Public Build Methods (Menu Wrapper Methods) ----

        [MenuItem("Magus/Build/Android/Development")]
        public static void BuildAndroidDevelopment() => Build("android-development");

        [MenuItem("Magus/Build/Android/Release")]
        public static void BuildAndroidRelease() => Build("android-release");

        [MenuItem("Magus/Build/iOS/Development")]
        public static void BuildIOSDevelopment() => Build("ios-development");

        [MenuItem("Magus/Build/iOS/Release")]
        public static void BuildIOSRelease() => Build("ios-release");

        [MenuItem("Magus/Build/Windows/Development")]
        public static void BuildWindowsDevelopment() => Build("windows-development");

        [MenuItem("Magus/Build/Windows/Release")]
        public static void BuildWindowsRelease() => Build("windows-release");

        // ---- Shared Build Pipeline ----

        private static BuildResult Build(string profileName)
        {
            var stopwatch = Stopwatch.StartNew();

            var profile = FindBuildProfile(profileName);
            if (profile == null)
            {
                Debug.LogError($"[MagusBuild] Build Profile '{profileName}' not found under '{ProfilesFolder}'. " +
                                $"Create it via File > Build Profiles and save it as '{profileName}.asset'.");
                return Fail();
            }

            if (!BuildAddressablesContent())
                return Fail();

            var buildTarget = GetBuildTarget(profile);
            var outputPath = ResolveOutputPath(buildTarget, profileName);
            var outputDir = buildTarget == BuildTarget.iOS ? outputPath : Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            var playerOptions = new BuildPlayerWithProfileOptions
            {
                buildProfile = profile,
                locationPathName = outputPath,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(playerOptions);
            stopwatch.Stop();

            return LogResult(report, buildTarget, profileName, outputPath, stopwatch.Elapsed);
        }

        // BuildProfile does not publicly expose its target platform, so read the serialized
        // field directly - the same mechanism Unity's own custom editors use for this class.
        private static BuildTarget GetBuildTarget(BuildProfile profile)
        {
            using var serializedProfile = new SerializedObject(profile);
            var buildTargetProperty = serializedProfile.FindProperty("m_BuildTarget");
            return (BuildTarget)buildTargetProperty.intValue;
        }

        // Building the Player alone does not bake Addressables content - it only does so
        // automatically when "Build Addressables Content on Player Build" is enabled in the
        // Addressables settings. Building it explicitly here means every build is correct
        // regardless of that (easy to accidentally toggle) Editor setting.
        private static bool BuildAddressablesContent()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return true;

            AddressableAssetSettings.BuildPlayerContent(out var result);

            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"[MagusBuild] Addressables content build failed: {result.Error}");
                return false;
            }

            Debug.Log($"[MagusBuild] Addressables content built: {result.LocationCount} locations in {result.Duration:F2}s.");
            return true;
        }

        private static BuildProfile FindBuildProfile(string profileName)
        {
            var guids = AssetDatabase.FindAssets("t:BuildProfile", new[] { ProfilesFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), profileName, StringComparison.OrdinalIgnoreCase))
                    return AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
            }

            return null;
        }

        // Unity's build API requires a concrete file (or folder, for Xcode-project platforms) path.
        // This is technical plumbing required to call BuildPipeline.BuildPlayer, not build configuration -
        // the profile itself remains the single source of truth for everything else.
        private static string ResolveOutputPath(BuildTarget buildTarget, string profileName)
        {
            var parts = profileName.Split('-');
            var platformFolder = ToDisplayName(parts[0]);
            var configurationFolder = ToDisplayName(parts.Length > 1 ? parts[1] : "development");

            var directory = $"{OutputRoot}/{platformFolder}/{configurationFolder}";

            var extension = buildTarget switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => ".exe",
                BuildTarget.Android => ".apk",
                _ => null,
            };

            // Platforms that build to a folder (e.g. iOS Xcode projects) use the directory itself.
            return extension == null ? directory : $"{directory}/{PlayerSettings.productName}{extension}";
        }

        private static string ToDisplayName(string token)
        {
            if (string.Equals(token, "ios", StringComparison.OrdinalIgnoreCase))
                return "iOS";

            return char.ToUpperInvariant(token[0]) + token.Substring(1).ToLowerInvariant();
        }

        private static BuildResult LogResult(BuildReport report, BuildTarget buildTarget, string profileName, string outputPath, TimeSpan duration)
        {
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                var sizeText = summary.totalSize > 0 ? $"{summary.totalSize / (1024f * 1024f):F2} MB" : "unknown";
                Debug.Log(
                    "[MagusBuild] Build succeeded.\n" +
                    $"Profile: {profileName}\n" +
                    $"Platform: {buildTarget}\n" +
                    $"Output: {outputPath}\n" +
                    $"Duration: {duration:mm\\:ss\\.ff}\n" +
                    $"Size: {sizeText}");
            }
            else
            {
                Debug.LogError(
                    $"[MagusBuild] Build FAILED for profile '{profileName}' (target {buildTarget}). " +
                    $"Result: {summary.result}, Errors: {summary.totalErrors}. See errors above for details.");
                return Fail(summary.result);
            }

            return summary.result;
        }

        private static BuildResult Fail(BuildResult result = BuildResult.Failed)
        {
            // Unity's default batch-mode exit code does not reflect build failures on its own,
            // so force a non-zero exit code when running headless (e.g. from build.sh / build.bat) for CI.
            if (Application.isBatchMode)
                EditorApplication.Exit(1);

            return result;
        }
    }
}
