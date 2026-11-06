using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NOIChaos.Editor
{
    public static class RuntimeSmokeTest
    {
        private static readonly List<string> Failures = new List<string>();
        private static double startedAt;
        private static bool startedGame;
        private static bool endedGame;

        public static void Run()
        {
            Failures.Clear();
            startedGame = false;
            endedGame = false;
            startedAt = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += CaptureLog;
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            double elapsed = EditorApplication.timeSinceStartup - startedAt;
            GameManager manager = Object.FindObjectOfType<GameManager>();

            if (!startedGame && manager != null && manager.Session != null && elapsed > 0.5)
            {
                startedGame = true;
                manager.StartGame();
            }

            if (startedGame && !endedGame && manager != null && manager.Session.Phase == GamePhase.Playing && elapsed > 2.2)
            {
                manager.Session.AddClue(0, "SMOKE A");
                manager.Session.AddClue(1, "SMOKE B");
                manager.Session.AddClue(2, "SMOKE C");
                manager.SubmitInsight();
                endedGame = manager.Session.Phase == GamePhase.Ended;
            }

            if (elapsed < 4.5)
            {
                return;
            }

            EditorApplication.update -= Tick;
            Application.logMessageReceived -= CaptureLog;
            bool passed = startedGame && endedGame && Failures.Count == 0;
            if (passed)
            {
                Debug.Log("[NOI SmokeTest] PASS - scene boot, player/world/UI creation and win flow completed.");
            }
            else
            {
                Debug.LogError("[NOI SmokeTest] FAIL\n" + string.Join("\n", Failures));
            }
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                Failures.Add(condition + "\n" + stackTrace);
            }
        }
    }
}
