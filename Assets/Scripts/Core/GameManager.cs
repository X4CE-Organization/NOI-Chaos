using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NOIChaos
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameSession Session { get; private set; }
        public ThirdPersonPlayer Player { get; private set; }
        public ThirdPersonCamera PlayerCamera { get; private set; }
        public GameUI UI { get; private set; }
        public ProceduralAudio AudioDirector { get; private set; }
        public ExamHallWorld World { get; private set; }
        public bool IsPlaying => Session != null && Session.Phase == GamePhase.Playing;

        private bool transitionLocked;
        private bool smokeTest;
        private bool smokeTestFinished;
        private float smokeTestStartedAt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Session = gameObject.AddComponent<GameSession>();
            AudioDirector = gameObject.AddComponent<ProceduralAudio>();
            UI = gameObject.AddComponent<GameUI>();
        }

        private IEnumerator Start()
        {
            yield return null;

            Player = ThirdPersonPlayer.Create(this, new Vector3(0f, 0.05f, -3.1f));
            PlayerCamera = ThirdPersonCamera.Create(this, Player);
            World = ExamHallBuilder.Build(this);
            UI.Initialize(this);
            Player.SetControl(false);
            UI.ShowTitle();
            AudioDirector.StartAmbient();

            if (HasCommandLineArgument("-noiSmokeTest"))
            {
                smokeTest = true;
                smokeTestStartedAt = Time.realtimeSinceStartup;
                StartCoroutine(RunSmokeTest());
            }
        }

        private void Update()
        {
            if (Session == null)
            {
                return;
            }

            if (smokeTest && !smokeTestFinished && Time.realtimeSinceStartup - smokeTestStartedAt > 15f)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - runtime watchdog expired.");
                Application.Quit(1);
                return;
            }

            if (Session.Phase == GamePhase.Playing)
            {
                if (Session.RemainingSeconds <= 0f)
                {
                    EndRun(false, "结局：交卷铃响了", "你还在研究第二题，交卷铃已经替你完成了时间复杂度证明。\n\n下次记得：歪招也需要计划。");
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    PauseGame();
                }
                else if (Input.GetKeyDown(KeyCode.Tab))
                {
                    UI.ToggleNotebook();
                }
            }
            else if (Session.Phase == GamePhase.Paused && Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame();
            }
            else if (Session.Phase == GamePhase.Ended && Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        public void StartGame()
        {
            if (Session.Phase != GamePhase.Title || transitionLocked)
            {
                return;
            }

            transitionLocked = true;
            StartCoroutine(StartGameRoutine());
        }

        private IEnumerator StartGameRoutine()
        {
            UI.Flash();
            AudioDirector.PlayConfirm();
            yield return new WaitForSeconds(0.35f);
            Session.ResetRun();
            UI.ShowHUD();
            Player.SetControl(true);
            PlayerCamera.SetControl(true);
            SetCursorLocked(true);
            UI.Toast("08:59:59  //  第一题 Accepted", new Color(0.45f, 1f, 0.72f));
            ToastLater("但你盯着第二题，决定今天不能只拿一个 Accepted。", 2.6f);
            transitionLocked = false;
        }

        public void PauseGame()
        {
            Session.Pause();
            Player.SetControl(false);
            PlayerCamera.SetControl(false);
            SetCursorLocked(false);
            Time.timeScale = 0f;
            UI.ShowPause(true);
        }

        public void ResumeGame()
        {
            Session.Resume();
            Time.timeScale = 1f;
            UI.ShowPause(false);
            UI.ToggleNotebook(false);
            Player.SetControl(true);
            PlayerCamera.SetControl(true);
            SetCursorLocked(true);
        }

        public void RestartGame()
        {
            if (transitionLocked)
            {
                return;
            }

            transitionLocked = true;
            StartCoroutine(RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            UI.Flash();
            AudioDirector.PlayConfirm();
            yield return new WaitForSeconds(0.2f);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Caught(string reason)
        {
            if (!IsPlaying)
            {
                return;
            }

            EndRun(false, "结局：零分预警", reason + "\n\n监考老师收起你的草稿纸。你忽然意识到，离金牌最近的一次，是刚才老老实实打开题面的时候。");
        }

        public void SubmitInsight()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (Session.ClueCount < Session.MaxClues)
            {
                int missing = Session.MaxClues - Session.ClueCount;
                Toast($"终端拒绝了你的玄学：还缺 {missing} 条能自洽的灵感。");
                AudioDirector.PlayError();
                Session.AddSuspicion(4f);
                return;
            }

            EndRun(true, "结局：金牌，居然有理有据", "离线分块、倍增合并，再让每个起点共享一次观察。\n\n评测终端亮起金色的 Accepted。你靠一个足够离谱、但完全正确的想法拿到了金牌。\n\nX4CE 年度最不按套路选手，诞生。");
        }

        public void TryMedal()
        {
            if (!IsPlaying)
            {
                return;
            }

            Session.MarkMedalTouched();
            AudioDirector.PlayError();
            Session.AddSuspicion(28f);
            Toast("展示柜：请勿触碰。它也不是真金牌。");
            ReportNoise(Player.transform.position, 5f, true);
        }

        public void RevealSideEnding()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (!Session.SideRoomFound)
            {
                Session.MarkSideRoomFound();
                Toast("海报后面不是密道，而是一本落满灰的《选手手册》。");
                return;
            }

            Toast("海报后面传来纸张的摩擦声。手册掉在脚边。");
        }

        public void CompleteHiddenEnding()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (!Session.SideRoomFound)
            {
                Toast("这也太跳关了。先看看那张海报。");
                return;
            }

            EndRun(true, "隐藏结局：规则之内，脑洞之外", "你最终没有偷走奖牌，也没有黑进评测机。\n\n你只是把题目重新读了一遍，又认真翻了规则。真正的漏洞从来不在评测系统里，而在自己最初以为“绝对做不出来”的那一步。\n\n金牌到手。监考老师甚至鼓了掌。");
        }

        public void AddSuspicion(float amount)
        {
            Session.AddSuspicion(amount);
            if (Session.Suspicion >= 100f && IsPlaying)
            {
                Caught("你成为了全场最显眼的目标。监考老师已经站在你身后。");
            }
        }

        public void ReduceSuspicion(float amount)
        {
            Session.ReduceSuspicion(amount);
        }

        public void ReportNoise(Vector3 position, float radius, bool dramatic = false)
        {
            if (World == null)
            {
                return;
            }

            World.AlertInvigilators(position, radius, dramatic);
        }

        public void DistractAll(Vector3 position, float duration)
        {
            if (World == null)
            {
                return;
            }

            World.DistractInvigilators(position, duration);
        }

        public void EndRun(bool victory, string title, string body)
        {
            if (Session.Phase != GamePhase.Playing)
            {
                return;
            }

            Session.End(victory, title, body);
            Player.SetControl(false);
            PlayerCamera.SetControl(false);
            UI.ToggleNotebook(false);
            UI.ShowEnd();
            SetCursorLocked(false);
            if (victory)
            {
                AudioDirector.PlayVictory();
            }
            else
            {
                AudioDirector.PlayCaught();
            }
        }

        public void Toast(string message)
        {
            UI.Toast(message);
        }

        public void Toast(string message, Color color)
        {
            UI.Toast(message, color);
        }

        private void ToastLater(string message, float delay)
        {
            StartCoroutine(ToastLaterRoutine(message, delay));
        }

        private IEnumerator ToastLaterRoutine(string message, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (Session.Phase == GamePhase.Playing)
            {
                UI.Toast(message);
            }
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private IEnumerator RunSmokeTest()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            if (World == null || Player == null || UI == null || World.Invigilators.Count < 2)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - world, player, UI or invigilators missing.");
                Application.Quit(1);
                yield break;
            }

            StartGame();
            yield return new WaitForSecondsRealtime(1.1f);
            if (Session.Phase != GamePhase.Playing)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - game did not enter Playing state.");
                Application.Quit(1);
                yield break;
            }

            Session.AddClue(0, "SMOKE A");
            Session.AddClue(1, "SMOKE B");
            Session.AddClue(2, "SMOKE C");
            SubmitInsight();
            yield return null;

            smokeTestFinished = true;
            if (Session.Phase == GamePhase.Ended && Session.Victory)
            {
                Debug.Log("[NOI SmokeTest] PASS - boot, scene generation, UI, patrol setup and victory flow succeeded.");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError("[NOI SmokeTest] FAIL - victory flow did not complete.");
                Application.Quit(1);
            }
        }

        private static bool HasCommandLineArgument(string expected)
        {
            string[] arguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], expected, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
