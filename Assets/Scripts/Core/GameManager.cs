using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
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
        public ExamTerminalUI ExamTerminal { get; private set; }
        public ProceduralAudio AudioDirector { get; private set; }
        public ExamHallWorld World { get; private set; }
        public bool IsPlaying => Session != null && Session.Phase == GamePhase.Playing;

        private bool transitionLocked;
        private bool smokeTest;
        private bool smokeTestFinished;
        private float smokeTestStartedAt;
        private bool shockActive;

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
            ExamTerminal = gameObject.AddComponent<ExamTerminalUI>();
        }

        private IEnumerator Start()
        {
            yield return null;

            Player = ThirdPersonPlayer.Create(this, new Vector3(0f, 0.05f, -3.1f));
            PlayerCamera = ThirdPersonCamera.Create(this, Player);
            World = ExamHallBuilder.Build(this);
            UI.Initialize(this);
            ExamTerminal.Initialize(this);
            Player.SetControl(false);
            UI.ShowTitle();
            AudioDirector.StartAmbient();

            if (HasCommandLineArgument("-noiSecuritySmokeTest"))
            {
                smokeTest = true;
                smokeTestStartedAt = Time.realtimeSinceStartup;
                StartCoroutine(RunSecuritySmokeTest());
            }
            else if (HasCommandLineArgument("-noiSmokeTest"))
            {
                smokeTest = true;
                smokeTestStartedAt = Time.realtimeSinceStartup;
                StartCoroutine(RunSmokeTest());
            }
            else if (HasCommandLineArgument("-noiScreenshotTest"))
            {
                StartCoroutine(RunScreenshotTest());
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
                    ExamTerminal.ForceClose();
                    FinalizeExam("交卷铃响了，系统自动提交当前最高成绩。");
                    return;
                }

                if (ExamTerminal.IsOpen)
                {
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
            ConfigureExamSources();
            UI.ShowHUD();
            Player.SetControl(true);
            PlayerCamera.SetControl(true);
            GameObject playerStation = GameObject.Find("Your station");
            Transform playerChair = playerStation != null ? playerStation.transform.Find("Chair") : null;
            Player.SitAt(playerChair);
            SetCursorLocked(true);
            UI.Toast("你已经在自己的 047 号座位坐好", new Color(0.45f, 1f, 0.72f));
            ToastLater("座位掩护生效：普通巡场扫描不会增加怀疑度。", 2.6f);
            ToastLater("E/左键站起 · F 侧头偷看 · G 扔纸团 · 移动和拳击需先站起", 5.2f);
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

            FinalizeExam("你按下了最终交卷按钮。");
        }

        public void OpenExamTerminal()
        {
            if (!IsPlaying)
            {
                return;
            }

            Session.MarkWorkstationChecked();
            ExamTerminal.Open();
        }

        private void ConfigureExamSources()
        {
            AnswerSource[] sources = FindObjectsOfType<AnswerSource>()
                .OrderBy(source => source.transform.position.x)
                .ThenBy(source => source.transform.position.z)
                .ToArray();
            if (sources.Length < 6 || Session.ExamRun == null)
            {
                Debug.LogError("[NOI Exam] Six neighbor code sources are required.");
                return;
            }

            for (int problemIndex = 0; problemIndex < 3; problemIndex++)
            {
                ExamProblemDefinition problem = Session.ExamRun.Problems[problemIndex];
                ExamSolutionVariant correct = problem.variants.FirstOrDefault(variant => variant.kind == "correct");
                ExamSolutionVariant partial = problem.variants.FirstOrDefault(variant => variant.kind == "partial");
                ConfigureSource(sources[problemIndex * 2], problem.id, correct);
                ConfigureSource(sources[problemIndex * 2 + 1], problem.id, partial);
            }
        }

        private static void ConfigureSource(AnswerSource source, string problemId, ExamSolutionVariant variant)
        {
            if (source == null || variant == null)
            {
                return;
            }

            string candidate = source.gameObject.name.Replace("Station ", string.Empty);
            source.ConfigureExam(problemId, variant.id, candidate);
        }

        public void FinalizeExam(string reason)
        {
            if (!IsPlaying || Session.ExamRun == null)
            {
                return;
            }

            ExamRunState run = Session.ExamRun;
            string medal = run.GetMedalName();
            StringBuilder body = new StringBuilder();
            body.AppendLine(reason);
            body.AppendLine();
            for (int i = 0; i < run.Problems.Count; i++)
            {
                ProblemRunState state = run.GetProblemState(i);
                body.AppendLine($"T{i + 1}  {run.Problems[i].id}  {state.BestScore}/100 {(state.Accepted ? "Accepted" : string.Empty)}");
            }
            body.AppendLine();
            body.AppendLine($"总分 {run.TotalScore}/300");
            body.AppendLine($"本局奖牌线：金 {run.GoldLine} / 银 {run.SilverLine} / 铜 {run.BronzeLine}");
            body.AppendLine($"结果：{medal}");
            bool victory = medal != "无牌";
            EndRun(victory, $"结局：{medal}", body.ToString());
        }

        public void LeaveExam()
        {
            if (!IsPlaying)
            {
                return;
            }

            EndRun(false, "结局：自由地弃赛", "你推开考场门，决定把奖牌留给更需要它的人。\n\n走廊里没有监考老师，也没有那些随机黑题。");
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

        public void AlertSecurity(Vector3 position, string reason)
        {
            if (World != null)
            {
                World.AlertSecurity(position);
            }
        }

        public void SecurityShock(string reason)
        {
            if (!IsPlaying || shockActive)
            {
                return;
            }

            shockActive = true;
            StartCoroutine(SecurityShockRoutine(reason));
        }

        private IEnumerator SecurityShockRoutine(string reason)
        {
            Session.AddSuspicion(100f);
            Player.SetControl(false);
            PlayerCamera.SetControl(false);
            AudioDirector.PlayShock();
            UI.ElectricFlash();
            UI.Toast("安保电击：不许动！", new Color(0.35f, 0.95f, 1f));
            yield return new WaitForSeconds(0.65f);
            EndRun(false, "结局：被保安电击", reason + "\n\n你的视野白了一秒。耳鸣中，监考老师平静地收走了答题纸。\n\n声势很大，但显然不是一道可执行算法。");
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
            if (ExamTerminal != null)
            {
                ExamTerminal.ForceClose();
            }
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
            if (World == null || Player == null || UI == null || World.Invigilators.Count < 2 || FindObjectsOfType<AnswerSource>().Length < 6)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - world, player, UI, invigilators or answer sources missing.");
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

            if (Session.ExamRun == null || Session.ExamRun.Problems.Count != 3
                || Session.ExamRun.Problems[0].id != "P1001"
                || Session.ExamRun.Problems.Select(problem => problem.id).Distinct().Count() != 3
                || Session.ExamRun.ProblemStates.Any(state => state.TestIndices.Length != 5))
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - exam problem selection or test selection is invalid.");
                Application.Quit(1);
                yield break;
            }

            try
            {
                ValidateExamModel();
            }
            catch (System.Exception exception)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - exam model validation: " + exception.Message);
                Application.Quit(1);
                yield break;
            }
            if (!LocalCppJudge.IsCompilerAvailable)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - local C++14 compiler was not detected.");
                Application.Quit(1);
                yield break;
            }
            ExamProblemDefinition abProblem = Session.ExamRun.Problems[0];
            ExamSolutionVariant abCorrect = abProblem.variants.First(variant => variant.kind == "correct");
            System.Threading.Tasks.Task<ExamJudgeResult> compileTask = LocalCppJudge.JudgeAsync(
                abProblem,
                Session.ExamRun.GetProblemState(0),
                ProblemCatalog.LoadCode(abCorrect),
                false);
            while (!compileTask.IsCompleted)
            {
                yield return null;
            }
            if (compileTask.IsFaulted || !compileTask.Result.CompileSuccess || compileTask.Result.Score != 100)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - local C++14 compile/judge flow failed.");
                Application.Quit(1);
                yield break;
            }
            const string bitsHeaderSmokeCode =
                "#include <bits/stdc++.h>\n" +
                "int main(){long long a,b;std::cin>>a>>b;std::cout<<a+b<<'\\n';}\n";
            System.Threading.Tasks.Task<ExamJudgeResult> bitsCompileTask = LocalCppJudge.JudgeAsync(
                abProblem,
                Session.ExamRun.GetProblemState(0),
                bitsHeaderSmokeCode,
                true);
            while (!bitsCompileTask.IsCompleted)
            {
                yield return null;
            }
            if (bitsCompileTask.IsFaulted
                || !bitsCompileTask.Result.CompileSuccess
                || bitsCompileTask.Result.Message != "Sample Passed")
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - #include <bits/stdc++.h> runtime compilation failed.");
                Application.Quit(1);
                yield break;
            }
            OpenExamTerminal();
            yield return null;
            if (!ExamTerminal.IsOpen || !ExamTerminal.IsCodeEditorReady)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - exam terminal or code editor did not open.");
                Application.Quit(1);
                yield break;
            }
            ExamTerminal.Close();
            yield return null;

            GameObject playerStation = GameObject.Find("Your station");
            Transform playerChair = playerStation != null ? playerStation.transform.Find("Chair") : null;
            Player.SitAt(playerChair);
            yield return null;
            if (!Player.IsSeated || !Session.IsSeated)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - player could not sit at seat 047.");
                Application.Quit(1);
                yield break;
            }

            Session.AddClue(0, "SMOKE INSPIRATION");
            AnswerSource[] sources = FindObjectsOfType<AnswerSource>();
            if (sources.Length > 0)
            {
                sources[0].Copy(Player);
                if (!Session.HasCopiedVariant(sources[0].ProblemId, sources[0].VariantId))
                {
                    smokeTestFinished = true;
                    Debug.LogError("[NOI SmokeTest] FAIL - neighbor code was not copied.");
                    Application.Quit(1);
                    yield break;
                }
            }
            Player.StandUp();
            yield return null;
            if (Player.IsSeated || Session.IsSeated)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - player could not stand up.");
                Application.Quit(1);
                yield break;
            }

            PunchableTarget[] punchTargets = FindObjectsOfType<PunchableTarget>();
            PunchableTarget candidateTarget = null;
            for (int i = 0; i < punchTargets.Length; i++)
            {
                if (punchTargets[i].DisplayName != null && punchTargets[i].DisplayName.StartsWith("选手"))
                {
                    candidateTarget = punchTargets[i];
                    break;
                }
            }
            if (candidateTarget == null)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - no punchable candidate found.");
                Application.Quit(1);
                yield break;
            }
            candidateTarget.ReceivePunch(Player);
            yield return null;
            if (Session.Suspicion < 20f)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - punching did not raise suspicion.");
                Application.Quit(1);
                yield break;
            }

            PaperBallProjectile.Throw(this, PlayerCamera.GetComponent<Camera>(), Player);
            yield return null;
            if (Session.CopiedAnswerCount < 1 || Session.PaperBallsThrown < 1)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - answer copying or paper-ball action did not register.");
                Application.Quit(1);
                yield break;
            }

            for (int i = 0; i < Session.ExamRun.Problems.Count; i++)
            {
                ExamProblemDefinition problem = Session.ExamRun.Problems[i];
                ExamSolutionVariant correct = problem.variants.First(variant => variant.kind == "correct");
                int score = OfflineJudge.Score(problem, Session.ExamRun.GetProblemState(i), correct.id);
                if (score != 100)
                {
                    smokeTestFinished = true;
                    Debug.LogError($"[NOI SmokeTest] FAIL - correct solution for {problem.id} scored {score}.");
                    Application.Quit(1);
                    yield break;
                }
                Session.RecordExamScore(problem.id, correct.id, score);
            }
            SubmitInsight();
            yield return null;

            smokeTestFinished = true;
            if (Session.Phase == GamePhase.Ended && Session.Victory)
            {
                Debug.Log("[NOI SmokeTest] PASS - exam selection, six code sources, terminal, copied code, punches, paper ball and 300-point gold flow succeeded.");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError("[NOI SmokeTest] FAIL - victory flow did not complete.");
                Application.Quit(1);
            }
        }

        private void ValidateExamModel()
        {
            if (ProblemCatalog.Problems.Count != 8)
            {
                throw new System.InvalidOperationException("Expected eight exam problems.");
            }
            for (int i = 0; i < ProblemCatalog.Problems.Count; i++)
            {
                ExamProblemDefinition definition = ProblemCatalog.Problems[i];
                if (definition.testCount != 8 || definition.variants == null || definition.variants.Length != 3)
                {
                    throw new System.InvalidOperationException("Invalid problem pack: " + definition.id);
                }
                ExamSolutionVariant correct = definition.variants.First(variant => variant.kind == "correct");
                if (!correct.passesSample || ProblemCatalog.LoadCode(correct).Length < 40)
                {
                    throw new System.InvalidOperationException("Missing or invalid correct solution: " + definition.id);
                }
            }

            for (int seed = 1; seed <= 20; seed++)
            {
                ExamRunState run = ExamRunState.Create(ProblemCatalog.Problems, seed);
                if (run.Problems.Count != 3
                    || run.Problems[0].id != "P1001"
                    || run.Problems.Select(problem => problem.id).Distinct().Count() != 3
                    || run.ProblemStates.Any(state => state.TestIndices.Length != 5)
                    || run.GoldLine < 200 || run.GoldLine > 260
                    || run.SilverLine < 130 || run.SilverLine > 180
                    || run.BronzeLine < 70 || run.BronzeLine > 120)
                {
                    throw new System.InvalidOperationException("Exam run invariants failed at seed " + seed);
                }
                if (run.GetMedalName(run.GoldLine) != "金牌"
                    || run.GetMedalName(run.SilverLine) != "银牌"
                    || run.GetMedalName(run.BronzeLine) != "铜牌"
                    || run.GetMedalName(run.BronzeLine - 1) != "无牌")
                {
                    throw new System.InvalidOperationException("Medal threshold boundary failed at seed " + seed);
                }

                for (int i = 0; i < run.Problems.Count; i++)
                {
                    ExamSolutionVariant partial = run.Problems[i].variants.First(variant => variant.kind == "partial");
                    int partialScore = OfflineJudge.Score(run.Problems[i], run.GetProblemState(i), partial.id);
                    if (partialScore < 20 || partialScore > 80 || partialScore % 20 != 0)
                    {
                        throw new System.InvalidOperationException($"Partial score out of range for {run.Problems[i].id}: {partialScore}");
                    }
                }
            }
        }

        private IEnumerator RunSecuritySmokeTest()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            StartGame();
            yield return new WaitForSecondsRealtime(1.1f);
            if (!IsPlaying || World == null)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - security test could not enter play mode.");
                Application.Quit(1);
                yield break;
            }

            bool hasSecurity = false;
            InvigilatorAgent securityGuard = null;
            for (int i = 0; i < World.Invigilators.Count; i++)
            {
                if (World.Invigilators[i].IsSecurityGuard)
                {
                    hasSecurity = true;
                    securityGuard = World.Invigilators[i];
                    break;
                }
            }
            if (!hasSecurity)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - security guard was not created.");
                Application.Quit(1);
                yield break;
            }

            ReportNoise(Player.transform.position, 20f, true);
            yield return new WaitForSecondsRealtime(0.8f);
            if (securityGuard.IsInvestigating)
            {
                smokeTestFinished = true;
                Debug.LogError("[NOI SmokeTest] FAIL - non-punch noise alerted the security guard.");
                Application.Quit(1);
                yield break;
            }

            GameObject playerStation = GameObject.Find("Your station");
            Transform playerChair = playerStation != null ? playerStation.transform.Find("Chair") : null;
            Player.SitAt(playerChair);
            yield return null;
            AlertSecurity(Player.transform.position, "SMOKE SECURITY ALERT");
            float securityDeadline = Time.realtimeSinceStartup + 8f;
            while (Session.Phase == GamePhase.Playing && Time.realtimeSinceStartup < securityDeadline)
            {
                yield return null;
            }
            smokeTestFinished = true;
            if (Session.Phase == GamePhase.Ended && !Session.Victory && Session.EndingTitle.Contains("电击"))
            {
                Debug.Log("[NOI SmokeTest] PASS - security guard heard the alert, reached the player and delivered the electric-shock ending.");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError($"[NOI SmokeTest] FAIL - electric-shock ending did not complete. Phase={Session.Phase}, Victory={Session.Victory}, Title={Session.EndingTitle}");
                Application.Quit(1);
            }
        }

        private IEnumerator RunScreenshotTest()
        {
            Screen.SetResolution(1600, 900, false);
            yield return new WaitForSecondsRealtime(2f);
            yield return CapturePng("/tmp/noi-title.png");
            yield return new WaitForSecondsRealtime(1.2f);

            StartGame();
            yield return new WaitForSecondsRealtime(1.2f);
            GameObject playerStation = GameObject.Find("Your station");
            Transform playerChair = playerStation != null ? playerStation.transform.Find("Chair") : null;
            if (playerChair != null)
            {
                Player.SitAt(playerChair);
            }
            yield return new WaitForSecondsRealtime(1.4f);
            yield return CapturePng("/tmp/noi-game.png");
            OpenExamTerminal();
            yield return new WaitForSecondsRealtime(1.2f);
            yield return CapturePng("/tmp/noi-exam.png");
            ExamTerminal.Close();
            yield return new WaitForSecondsRealtime(1.2f);

            Debug.Log($"[NOI ScreenshotTest] Complete at {Screen.width}x{Screen.height}; graphics={SystemInfo.graphicsDeviceName}.");
            Application.Quit(0);
        }

        private IEnumerator CapturePng(string path)
        {
            yield return new WaitForEndOfFrame();
            const int width = 1600;
            const int height = 900;
            Camera captureCamera = PlayerCamera.GetComponent<Camera>();
            Canvas uiCanvas = UI.Canvas;
            RenderTexture previousTarget = captureCamera.targetTexture;
            RenderMode previousMode = uiCanvas.renderMode;
            Camera previousUiCamera = uiCanvas.worldCamera;
            Canvas examCanvas = ExamTerminal.Canvas;
            RenderMode previousExamMode = examCanvas.renderMode;
            Camera previousExamCamera = examCanvas.worldCamera;

            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            captureCamera.targetTexture = renderTexture;
            uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            uiCanvas.worldCamera = captureCamera;
            uiCanvas.planeDistance = 0.35f;
            examCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            examCanvas.worldCamera = captureCamera;
            examCanvas.planeDistance = 0.3f;
            captureCamera.Render();

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());

            RenderTexture.active = previousActive;
            captureCamera.targetTexture = previousTarget;
            uiCanvas.renderMode = previousMode;
            uiCanvas.worldCamera = previousUiCamera;
            examCanvas.renderMode = previousExamMode;
            examCanvas.worldCamera = previousExamCamera;
            RenderTexture.ReleaseTemporary(renderTexture);
            Destroy(texture);
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
