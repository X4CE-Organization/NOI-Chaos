using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NOIChaos.Editor
{
    public static class ProjectBootstrapper
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("NOI Chaos/Rebuild Main Scene")]
        public static void SetupProject()
        {
            Directory.CreateDirectory("Assets/Scenes");
            ConfigurePlayer();
            CreateMainScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NOI Chaos] Project setup complete. Main scene: " + MainScenePath);
        }

        [MenuItem("NOI Chaos/Build macOS Smoke Test")]
        public static void BuildMacSmokeTest()
        {
            Directory.CreateDirectory("Builds/Smoke");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/Smoke/NOIChaosSmoke.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development | BuildOptions.StrictMode
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new IOException("macOS smoke build failed: " + report.summary.result);
            }
            Debug.Log("[NOI Chaos] macOS smoke build complete: " + options.locationPathName);
        }

        [MenuItem("NOI Chaos/Build macOS App")]
        public static void BuildMacApp()
        {
            Directory.CreateDirectory("Builds/macOS");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/macOS/NOI Chaos.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.StrictMode
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new IOException("macOS build failed: " + report.summary.result);
            }
            Debug.Log("[NOI Chaos] macOS build complete: " + options.locationPathName);
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "X4CE";
            PlayerSettings.productName = "NOI 乱斗";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.x4ce.noichaos");
        }

        private static void CreateMainScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject manager = new GameObject("NOI Chaos // Game Manager");
            manager.AddComponent<GameManager>();

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, MainScenePath);
            if (!saved)
            {
                throw new IOException("Could not save " + MainScenePath);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };
        }
    }
}
