using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NOIChaos
{
    public sealed class GameUI : MonoBehaviour
    {
        private GameManager game;
        private Font font;
        private Canvas canvas;
        private GameObject titleRoot;
        private GameObject hudRoot;
        private GameObject notebookRoot;
        private GameObject pauseRoot;
        private GameObject endRoot;
        private GameObject toastRoot;
        private GameObject flashRoot;
        private Text objectiveText;
        private Text clueCounterText;
        private Text clueListText;
        private Text suspicionText;
        private Text promptText;
        private Text notebookText;
        private Text timerText;
        private Text controlHintText;
        private Text toastText;
        private Text endTitleText;
        private Text endBodyText;
        private Image suspicionFill;
        private CanvasGroup toastCanvasGroup;
        private CanvasGroup flashCanvasGroup;
        private Coroutine toastRoutine;
        private bool initialized;

        public bool IsNotebookOpen => notebookRoot != null && notebookRoot.activeSelf;
        public Canvas Canvas => canvas;

        public void Initialize(GameManager manager)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            game = manager;
            font = FontLibrary.GetChineseFont();
            BuildEventSystem();
            BuildCanvas();
            BuildTitle();
            BuildHUD();
            BuildNotebook();
            BuildPause();
            BuildEnd();
            BuildToast();
            BuildFlash();
            game.Session.Changed += RefreshState;
            RefreshState();
        }

        public void ShowTitle()
        {
            titleRoot.SetActive(true);
            hudRoot.SetActive(false);
            notebookRoot.SetActive(false);
            pauseRoot.SetActive(false);
            endRoot.SetActive(false);
        }

        public void ShowHUD()
        {
            titleRoot.SetActive(false);
            hudRoot.SetActive(true);
            pauseRoot.SetActive(false);
            endRoot.SetActive(false);
            notebookRoot.SetActive(false);
            RefreshState();
        }

        public void ShowPause(bool show)
        {
            pauseRoot.SetActive(show);
            if (show)
            {
                notebookRoot.SetActive(false);
            }
        }

        public void ToggleNotebook()
        {
            ToggleNotebook(!IsNotebookOpen);
        }

        public void ToggleNotebook(bool show)
        {
            if (game.Session.Phase == GamePhase.Ended || game.Session.Phase == GamePhase.Title)
            {
                return;
            }

            notebookRoot.SetActive(show);
            if (show)
            {
                RefreshNotebook();
            }
        }

        public void ShowEnd()
        {
            notebookRoot.SetActive(false);
            pauseRoot.SetActive(false);
            hudRoot.SetActive(true);
            endRoot.SetActive(true);
            endTitleText.text = game.Session.EndingTitle;
            endBodyText.text = game.Session.EndingBody;
            endTitleText.color = game.Session.Victory ? new Color(1f, 0.72f, 0.18f) : new Color(1f, 0.32f, 0.25f);
        }

        public void SetPrompt(string prompt)
        {
            if (promptText == null)
            {
                return;
            }
            promptText.text = prompt;
            promptText.CrossFadeAlpha(string.IsNullOrEmpty(prompt) ? 0f : 1f, 0.12f, true);
        }

        public void Toast(string message)
        {
            Toast(message, new Color(0.85f, 0.93f, 0.96f));
        }

        public void Toast(string message, Color color)
        {
            if (!initialized)
            {
                return;
            }

            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }

            toastText.text = message;
            toastText.color = color;
            toastCanvasGroup.alpha = 1f;
            toastRoot.SetActive(true);
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        public void Flash()
        {
            if (flashRoot == null)
            {
                return;
            }
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        public void ElectricFlash()
        {
            if (flashRoot == null)
            {
                return;
            }

            StopAllCoroutines();
            flashRoot.GetComponent<Image>().color = new Color(0.24f, 0.92f, 1f, 1f);
            StartCoroutine(ElectricFlashRoutine());
        }

        private void RefreshState()
        {
            if (!initialized || game == null || game.Session == null)
            {
                return;
            }

            GameSession session = game.Session;
            objectiveText.text = session.Objective;
            clueCounterText.text = $"灵感 {session.ClueCount}/{session.MaxClues} · 抄录 {session.CopiedAnswerCount}/{session.MaxCopiedAnswers}";

            float suspicion = session.Suspicion / 100f;
            suspicionFill.rectTransform.anchorMax = new Vector2(suspicion, 1f);
            suspicionFill.color = Color.Lerp(
                new Color(0.16f, 0.78f, 0.75f),
                new Color(1f, 0.22f, 0.16f),
                Mathf.Clamp01(suspicion * 1.25f));
            RefreshDetectionStatus(session);
            if (controlHintText != null)
            {
                controlHintText.text = session.IsSeated
                    ? "坐姿：鼠标左键 电脑   ·   F 偷看代码   ·   G 纸团   ·   E 站起   ·   Tab 笔记"
                    : "WASD 移动 · Shift 奔跑 · C 蹲伏 · 左键/E 交互 · F 抄代码 · G 纸团 · Q 拳击 · Tab 笔记";
            }

            if (clueListText != null)
            {
                if (session.ClueNames.Count == 0 && session.CopiedAnswerCount == 0)
                {
                    clueListText.text = "尚未收集\n\n观察、询问、翻找。\n靠近别人的屏幕按 F 抄代码。";
                }
                else
                {
                    StringBuilder clueBuilder = new StringBuilder();
                    clueBuilder.AppendLine($"灵感 {session.ClueCount}/{session.MaxClues}");
                    for (int i = 0; i < session.ClueNames.Count; i++)
                    {
                        clueBuilder.Append("◆ ");
                        clueBuilder.AppendLine(session.ClueNames[i]);
                    }
                    clueBuilder.AppendLine();
                    clueBuilder.AppendLine($"抄录答案 {session.CopiedAnswerCount}/{session.MaxCopiedAnswers}");
                    clueListText.text = clueBuilder.ToString();
                }
            }

            RefreshNotebook();
        }

        private void Update()
        {
            if (timerText == null || game == null || game.Session == null)
            {
                return;
            }

            float remaining = game.Session.RemainingSeconds;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"T-MINUS  {minutes:00}:{seconds:00}";
            timerText.color = remaining < 60f ? new Color(1f, 0.31f, 0.25f) : new Color(0.9f, 0.94f, 0.94f);
            RefreshDetectionStatus(game.Session);
        }

        private void RefreshDetectionStatus(GameSession session)
        {
            if (suspicionText == null)
            {
                return;
            }

            int suspicionPercent = Mathf.RoundToInt(session.Suspicion);
            if (session.IsSeated && game.Player != null && game.Player.IsConcealedAtSeat)
            {
                suspicionText.text = "座位掩护 · 安全";
                suspicionText.color = new Color(0.32f, 0.94f, 0.76f);
            }
            else if (session.IsSeated)
            {
                suspicionText.text = $"暴露中 · {suspicionPercent:00}%";
                suspicionText.color = new Color(1f, 0.66f, 0.22f);
            }
            else
            {
                suspicionText.text = $"怀疑度  {suspicionPercent:00}%";
                suspicionText.color = session.Suspicion > 72f
                    ? new Color(1f, 0.45f, 0.34f)
                    : new Color(0.77f, 0.84f, 0.87f);
            }
        }

        private void RefreshNotebook()
        {
            if (notebookText == null || game == null || game.Session == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("EXAM NOTES  //  SEAT 047");
            builder.AppendLine();
            builder.AppendLine("<color=#61E5DB>当前目标</color>");
            builder.AppendLine(game.Session.Objective);
            builder.AppendLine();
            builder.AppendLine("<color=#61E5DB>灵感碎片</color>");
            if (game.Session.ClueNames.Count == 0)
            {
                builder.AppendLine("· 尚未发现。先观察白板、废纸篓和邻座。");
            }
            else
            {
                for (int i = 0; i < game.Session.ClueNames.Count; i++)
                {
                    builder.AppendLine($"· {game.Session.ClueNames[i]}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("<color=#61E5DB>抄来的答案</color>");
            if (game.Session.CopiedAnswers.Count == 0)
            {
                builder.AppendLine("· 靠近其他人的屏幕，按 F 偷看。蹲下会更安全。");
            }
            else
            {
                for (int i = 0; i < game.Session.CopiedAnswers.Count; i++)
                {
                    builder.AppendLine($"· {game.Session.CopiedAnswers[i]}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("<color=#61E5DB>行动记录</color>");
            if (game.Session.ExamRun != null)
            {
                for (int i = 0; i < game.Session.ExamRun.Problems.Count; i++)
                {
                    ProblemRunState state = game.Session.ExamRun.GetProblemState(i);
                    builder.AppendLine($"· T{i + 1} {state.ProblemId}：{state.BestScore}/100");
                }
                builder.AppendLine($"· 总分：{game.Session.ExamScore}/300");
            }
            builder.AppendLine(game.Session.SideRoomFound ? "· 发现被遗忘的选手手册" : "· 海报背后似乎有空隙");
            builder.AppendLine($"· 扔出纸团：{game.Session.PaperBallsThrown}");
            builder.AppendLine();
            builder.AppendLine("<color=#F2AA3C>操作</color>  WASD 移动  ·  鼠标观察  ·  Shift 奔跑");
            builder.AppendLine(game.Session.IsSeated
                ? "坐姿：F 侧头偷看  ·  G 扔纸团  ·  E/左键 站起  ·  移动动作禁用"
                : "C 蹲伏  ·  F 抄代码  ·  G 扔纸团  ·  Q 拳击  ·  E/左键 交互  ·  Tab 关闭");
            notebookText.text = builder.ToString();
        }

        private void BuildEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("NOI Chaos UI");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        private void BuildTitle()
        {
            titleRoot = CreateFullPanel("Title screen", canvas.transform, Color.white);
            ApplyTitleBackdrop(titleRoot);
            CreatePanel(
                "Left editorial panel",
                titleRoot.transform,
                new Color(0.018f, 0.033f, 0.048f, 0.42f),
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(1160f, 0f));
            CreatePanel(
                "Gold signature",
                titleRoot.transform,
                new Color(1f, 0.63f, 0.14f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(92f, 0f),
                new Vector2(7f, 610f));
            CreatePanel(
                "Top hairline",
                titleRoot.transform,
                new Color(0.23f, 0.84f, 0.79f, 0.5f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -38f),
                new Vector2(-240f, 1f));

            Text brand = CreateText("Brand", titleRoot.transform, "NOI 2026  /  OPEN EXAM  /  BUILD 0.3", 18, new Color(0.39f, 0.86f, 0.82f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(brand.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132f, -78f), new Vector2(760f, 32f));
            Text edition = CreateText("Edition", titleRoot.transform, "NOI 2026  ·  CAMPUS INCIDENT FILE", 17, new Color(0.6f, 0.65f, 0.67f), TextAnchor.MiddleRight);
            Place(edition.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-118f, -78f), new Vector2(620f, 32f));

            Text kicker = CreateText("Kicker", titleRoot.transform, "考场潜行喜剧", 24, new Color(0.72f, 0.79f, 0.8f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(kicker.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(132f, 220f), new Vector2(430f, 36f));

            Text title = CreateText("Title", titleRoot.transform, "NOI 乱斗", 108, new Color(1f, 0.69f, 0.19f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(126f, 108f), new Vector2(990f, 170f));
            Outline titleOutline = title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.12f, 0.05f, 0.01f, 0.8f);
            titleOutline.effectDistance = new Vector2(3f, -4f);

            CreatePanel(
                "Title underline",
                titleRoot.transform,
                new Color(0.23f, 0.84f, 0.79f, 0.72f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(132f, 8f),
                new Vector2(655f, 2f));

            Text subtitle = CreateText(
                "Subtitle",
                titleRoot.transform,
                "三道题，一场考试。A+B 只是开始。\n能拿多少分，取决于你抄到了什么。",
                28,
                new Color(0.85f, 0.89f, 0.89f),
                TextAnchor.UpperLeft);
            subtitle.lineSpacing = 1.25f;
            Place(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(132f, -82f), new Vector2(760f, 112f));

            Button startButton = CreateButton("Start button", titleRoot.transform, "进入考场  /  ENTER", new Vector2(360f, 72f), new Color(0.95f, 0.58f, 0.12f), new Color(0.035f, 0.05f, 0.065f));
            Place(startButton.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(132f, -205f), new Vector2(360f, 72f));
            startButton.onClick.AddListener(game.StartGame);

            Text startHint = CreateText("Start hint", titleRoot.transform, "WASD 移动 · 左键/E 交互 · F 抄代码 · G 纸团 · Q 拳击", 18, new Color(0.49f, 0.57f, 0.59f), TextAnchor.MiddleLeft);
            Place(startHint.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(132f, -270f), new Vector2(680f, 32f));

            Text disclaimer = CreateText(
                "Disclaimer",
                titleRoot.transform,
                "虚构考场 · 荒诞潜行 · 不鼓励现实作弊\n大声打斗会引来保安，电击不是装饰",
                17,
                new Color(0.42f, 0.49f, 0.51f),
                TextAnchor.UpperLeft);
            Place(disclaimer.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(132f, -334f), new Vector2(760f, 62f));

            BuildContestFilePanel(titleRoot.transform);

            Text footer = CreateText(
                "Footer",
                titleRoot.transform,
                "Copyright © 2026 X4CE   ·   MIT License   ·   Unity 2022.3",
                16,
                new Color(0.37f, 0.43f, 0.45f),
                TextAnchor.MiddleLeft);
            Place(footer.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(132f, 55f), new Vector2(800f, 30f));
        }

        private void ApplyTitleBackdrop(GameObject root)
        {
            const int textureWidth = 320;
            const int textureHeight = 180;
            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            texture.name = "NOI title backdrop";

            Color baseColor = new Color(0.006f, 0.014f, 0.024f, 1f);
            Color tealColor = new Color(0.018f, 0.095f, 0.1f, 1f);
            for (int y = 0; y < textureHeight; y++)
            {
                float v = y / (float)(textureHeight - 1);
                for (int x = 0; x < textureWidth; x++)
                {
                    float u = x / (float)(textureWidth - 1);
                    float horizontal = Mathf.SmoothStep(0f, 1f, u) * 0.34f;
                    float glowX = (u - 0.78f) * 2.2f;
                    float glowY = (v - 0.46f) * 1.55f;
                    float glow = Mathf.Exp(-(glowX * glowX + glowY * glowY) * 2.8f) * 0.45f;
                    float scan = Mathf.Sin((u * 160f + v * 42f) * Mathf.PI) * 0.003f;
                    Color color = Color.Lerp(baseColor, tealColor, horizontal + glow);
                    color.r += scan;
                    color.g += scan;
                    color.b += scan;
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            Image background = root.GetComponent<Image>();
            background.sprite = Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0.5f));
            background.type = Image.Type.Simple;
            background.color = Color.white;

            for (int i = 1; i < 12; i++)
            {
                Image vertical = CreateImage("Backdrop vertical", root.transform, new Color(0.2f, 0.78f, 0.75f, 0.035f));
                Place(
                    vertical.rectTransform,
                    new Vector2(i / 12f, 0f),
                    new Vector2(i / 12f, 1f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(1f, 0f));
                vertical.raycastTarget = false;
            }
            for (int i = 1; i < 8; i++)
            {
                Image horizontalLine = CreateImage("Backdrop horizontal", root.transform, new Color(0.2f, 0.78f, 0.75f, 0.025f));
                Place(
                    horizontalLine.rectTransform,
                    new Vector2(0f, i / 8f),
                    new Vector2(1f, i / 8f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(0f, 1f));
                horizontalLine.raycastTarget = false;
            }
        }

        private void BuildContestFilePanel(Transform parent)
        {
            GameObject filePanel = CreatePanel(
                "Contest file",
                parent,
                new Color(0.018f, 0.038f, 0.054f, 0.94f),
                new Vector2(0.77f, 0.5f),
                new Vector2(0.77f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(468f, 610f));
            CreatePanel(
                "File accent",
                filePanel.transform,
                new Color(0.95f, 0.59f, 0.14f, 1f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(0f, 4f));

            Text header = CreateText("File header", filePanel.transform, "CONTEST FILE  /  2026", 18, new Color(0.4f, 0.85f, 0.81f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -34f), new Vector2(300f, 28f));
            Text clearance = CreateText("Clearance", filePanel.transform, "PUBLIC", 15, new Color(1f, 0.67f, 0.18f), TextAnchor.MiddleRight, FontStyle.Bold);
            Place(clearance.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -34f), new Vector2(100f, 28f));
            CreateLine(filePanel.transform, new Vector2(32f, -66f), 404f, new Color(0.18f, 0.3f, 0.36f, 0.8f));

            Text candidateLabel = CreateText("Candidate label", filePanel.transform, "CANDIDATE", 15, new Color(0.42f, 0.5f, 0.53f), TextAnchor.MiddleLeft);
            Place(candidateLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -92f), new Vector2(180f, 24f));
            Text candidate = CreateText("Candidate", filePanel.transform, "考生 047 / 你", 31, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(candidate.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -122f), new Vector2(240f, 42f));
            Text rank = CreateText("Rank", filePanel.transform, "047", 74, new Color(1f, 0.68f, 0.18f), TextAnchor.MiddleRight, FontStyle.Bold);
            Place(rank.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -112f), new Vector2(170f, 88f));

            CreateLine(filePanel.transform, new Vector2(32f, -185f), 404f, new Color(0.12f, 0.21f, 0.26f, 0.85f));
            CreateFileRow(filePanel.transform, "TASK 01", "A+B Ready", new Color(0.32f, 0.91f, 0.68f), -218f);
            CreateFileRow(filePanel.transform, "TASK 02-03", "Black × 2", new Color(1f, 0.67f, 0.18f), -267f);
            CreateFileRow(filePanel.transform, "GOLD LINE", "200–260", new Color(1f, 0.39f, 0.28f), -316f);
            CreateLine(filePanel.transform, new Vector2(32f, -365f), 404f, new Color(0.12f, 0.21f, 0.26f, 0.85f));

            Text summaryLabel = CreateText("Summary label", filePanel.transform, "CURRENT MISSION", 15, new Color(0.42f, 0.5f, 0.53f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(summaryLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -392f), new Vector2(240f, 24f));
            Text summary = CreateText(
                "Summary",
                filePanel.transform,
                "做 A+B、抄邻座代码、制造混乱。\n在铃响之前争取金牌线。",
                21,
                new Color(0.78f, 0.84f, 0.85f),
                TextAnchor.UpperLeft);
            summary.lineSpacing = 1.3f;
            Place(summary.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -428f), new Vector2(404f, 82f));

            Text footer = CreateText("File footer", filePanel.transform, "NOI CAMPUS DIVISION  ·  RUNTIME GENERATED", 13, new Color(0.32f, 0.42f, 0.46f), TextAnchor.MiddleLeft);
            Place(footer.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 26f), new Vector2(404f, 22f));
        }

        private void CreateFileRow(Transform parent, string label, string value, Color valueColor, float top)
        {
            Text key = CreateText("File row " + label, parent, label, 15, new Color(0.43f, 0.52f, 0.55f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(key.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, top), new Vector2(180f, 26f));
            Text result = CreateText("File value " + label, parent, value, 18, valueColor, TextAnchor.MiddleRight, FontStyle.Bold);
            Place(result.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, top), new Vector2(220f, 26f));
        }

        private void CreateLine(Transform parent, Vector2 position, float width, Color color)
        {
            Image line = CreateImage("Divider", parent, color);
            Place(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(width, 1f));
            line.raycastTarget = false;
        }

        private void BuildHUD()
        {
            hudRoot = CreateFullPanel("HUD", canvas.transform, Color.clear);
            hudRoot.GetComponent<Image>().raycastTarget = false;

            GameObject objectivePanel = CreatePanel("Objective panel", hudRoot.transform, new Color(0.025f, 0.05f, 0.072f, 0.88f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -42f), new Vector2(680f, 152f));
            CreatePanel("Objective accent", objectivePanel.transform, new Color(0.16f, 0.87f, 0.8f, 1f), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5f, 0f));
            Text label = CreateText("Label", objectivePanel.transform, "CURRENT OBJECTIVE  //  当前目标", 17, new Color(0.36f, 0.79f, 0.78f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -22f), new Vector2(610f, 28f));
            objectiveText = CreateText("Objective", objectivePanel.transform, string.Empty, 25, Color.white, TextAnchor.UpperLeft);
            Place(objectiveText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -58f), new Vector2(620f, 68f));

            GameObject statusPanel = CreatePanel("Status panel", hudRoot.transform, new Color(0.025f, 0.05f, 0.072f, 0.88f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-46f, -42f), new Vector2(430f, 152f));
            suspicionText = CreateText("Suspicion label", statusPanel.transform, "怀疑度  00%", 20, new Color(0.77f, 0.84f, 0.87f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(suspicionText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -22f), new Vector2(190f, 28f));
            clueCounterText = CreateText("Clue count", statusPanel.transform, "灵感 0/3 · 抄录 0/6", 16, new Color(1f, 0.69f, 0.2f), TextAnchor.MiddleRight, FontStyle.Bold);
            Place(clueCounterText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -22f), new Vector2(210f, 28f));

            Image barBackground = CreateImage("Suspicion track", statusPanel.transform, new Color(0.08f, 0.12f, 0.15f, 1f));
            Place(barBackground.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -70f), new Vector2(386f, 15f));
            suspicionFill = CreateImage("Suspicion fill", barBackground.transform, new Color(0.16f, 0.78f, 0.75f, 1f));
            RectTransform fillTransform = suspicionFill.rectTransform;
            fillTransform.anchorMin = Vector2.zero;
            fillTransform.anchorMax = Vector2.one;
            fillTransform.offsetMin = Vector2.zero;
            fillTransform.offsetMax = Vector2.zero;
            fillTransform.pivot = new Vector2(0f, 0.5f);
            Text barHelp = CreateText("Suspicion help", statusPanel.transform, "奔跑和可疑互动会吸引老师注意", 15, new Color(0.42f, 0.5f, 0.54f), TextAnchor.MiddleLeft);
            Place(barHelp.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -102f), new Vector2(386f, 25f));

            GameObject timerPanel = CreatePanel("Timer panel", hudRoot.transform, new Color(0.025f, 0.05f, 0.072f, 0.88f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(250f, 58f));
            timerText = CreateText("Timer", timerPanel.transform, "T-MINUS  09:00", 22, new Color(0.9f, 0.94f, 0.94f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(timerText.rectTransform, 8f, 8f, 5f, 5f);

            GameObject cluePanel = CreatePanel("Clue panel", hudRoot.transform, new Color(0.025f, 0.05f, 0.072f, 0.76f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(46f, -112f), new Vector2(340f, 220f));
            Text clueHeading = CreateText("Clue heading", cluePanel.transform, "INSPIRATION  //  灵感", 17, new Color(0.36f, 0.79f, 0.78f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(clueHeading.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -20f), new Vector2(292f, 28f));
            clueListText = CreateText("Clue list", cluePanel.transform, "尚未收集\n\n观察、询问、翻找。\n靠近别人的屏幕按 F 抄代码。", 18, new Color(0.7f, 0.78f, 0.8f), TextAnchor.UpperLeft);
            Place(clueListText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -58f), new Vector2(292f, 140f));

            promptText = CreateText("Interaction prompt", hudRoot.transform, string.Empty, 22, new Color(1f, 0.76f, 0.25f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(promptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 142f), new Vector2(1180f, 52f));
            Outline promptOutline = promptText.gameObject.AddComponent<Outline>();
            promptOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            promptOutline.effectDistance = new Vector2(2f, -2f);
            promptText.CrossFadeAlpha(0f, 0f, true);

            controlHintText = CreateText("Control hint", hudRoot.transform, "WASD 移动 · Shift 奔跑 · C 蹲伏 · 左键/E 交互 · F 抄代码 · G 纸团 · Q 拳击 · Tab 笔记", 16, new Color(0.45f, 0.54f, 0.58f), TextAnchor.MiddleLeft);
            Place(controlHintText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 40f), new Vector2(1000f, 32f));

            Text watermark = CreateText("Watermark", hudRoot.transform, "EXAM HALL 03  /  2026", 17, new Color(0.95f, 0.61f, 0.16f, 0.7f), TextAnchor.MiddleRight);
            Place(watermark.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 40f), new Vector2(300f, 32f));

            BuildVignette(hudRoot.transform);
        }

        private void BuildNotebook()
        {
            notebookRoot = CreateFullPanel("Notebook overlay", canvas.transform, new Color(0f, 0f, 0f, 0.56f));
            GameObject panel = CreatePanel("Notebook", notebookRoot.transform, new Color(0.025f, 0.055f, 0.075f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1020f, 700f));
            CreatePanel("Notebook edge", panel.transform, new Color(0.97f, 0.63f, 0.16f, 1f), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(7f, 0f));
            Text heading = CreateText("Heading", panel.transform, "EXAM NOTES  //  SEAT 047", 30, new Color(0.43f, 0.91f, 0.86f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(heading.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -36f), new Vector2(760f, 44f));
            Text close = CreateText("Close hint", panel.transform, "TAB  关闭", 17, new Color(0.48f, 0.57f, 0.6f), TextAnchor.MiddleRight);
            Place(close.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -36f), new Vector2(180f, 32f));
            notebookText = CreateText("Notebook body", panel.transform, string.Empty, 19, new Color(0.85f, 0.89f, 0.9f), TextAnchor.UpperLeft);
            notebookText.lineSpacing = 0.98f;
            Place(notebookText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -108f), new Vector2(930f, 545f));
            notebookRoot.SetActive(false);
        }

        private void BuildPause()
        {
            pauseRoot = CreateFullPanel("Pause overlay", canvas.transform, new Color(0.005f, 0.012f, 0.02f, 0.82f));
            Text pauseTitle = CreateText("Pause title", pauseRoot.transform, "PAUSED", 76, new Color(1f, 0.69f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(pauseTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(700f, 100f));
            Text pauseChinese = CreateText("Pause Chinese", pauseRoot.transform, "考试暂停，脑子可以继续转", 23, new Color(0.67f, 0.76f, 0.79f), TextAnchor.MiddleCenter);
            Place(pauseChinese.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 82f), new Vector2(600f, 40f));

            Button resume = CreateButton("Resume", pauseRoot.transform, "继续考试  /  RESUME", new Vector2(320f, 64f), new Color(0.96f, 0.61f, 0.16f), new Color(0.03f, 0.045f, 0.06f));
            Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(320f, 64f));
            resume.onClick.AddListener(game.ResumeGame);

            Button quit = CreateButton("Quit", pauseRoot.transform, "退出游戏", new Vector2(320f, 52f), new Color(0.1f, 0.14f, 0.17f), new Color(0.76f, 0.82f, 0.84f));
            Place(quit.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(320f, 52f));
            quit.onClick.AddListener(game.QuitGame);
            pauseRoot.SetActive(false);
        }

        private void BuildEnd()
        {
            endRoot = CreateFullPanel("Ending overlay", canvas.transform, new Color(0.006f, 0.012f, 0.02f, 0.93f));
            GameObject line = CreatePanel("Ending top line", endRoot.transform, new Color(1f, 0.64f, 0.15f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 270f), new Vector2(116f, 4f));
            line.GetComponent<Image>().raycastTarget = false;

            Text kicker = CreateText("Ending kicker", endRoot.transform, "RESULT  /  NOI 2026", 19, new Color(0.41f, 0.82f, 0.8f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(kicker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(700f, 32f));
            endTitleText = CreateText("Ending title", endRoot.transform, "结局", 60, new Color(1f, 0.7f, 0.18f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(endTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(1400f, 90f));
            endBodyText = CreateText("Ending body", endRoot.transform, string.Empty, 25, new Color(0.82f, 0.88f, 0.89f), TextAnchor.UpperCenter);
            Place(endBodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(1050f, 220f));

            Button restart = CreateButton("Restart", endRoot.transform, "再来一次  /  R", new Vector2(300f, 64f), new Color(0.96f, 0.61f, 0.16f), new Color(0.03f, 0.045f, 0.06f));
            Place(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-170f, -240f), new Vector2(300f, 64f));
            restart.onClick.AddListener(game.RestartGame);

            Button quit = CreateButton("Quit", endRoot.transform, "退出", new Vector2(220f, 64f), new Color(0.1f, 0.14f, 0.17f), new Color(0.78f, 0.84f, 0.86f));
            Place(quit.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180f, -240f), new Vector2(220f, 64f));
            quit.onClick.AddListener(game.QuitGame);
            endRoot.SetActive(false);
        }

        private void BuildToast()
        {
            toastRoot = CreatePanel("Toast", canvas.transform, new Color(0.02f, 0.045f, 0.06f, 0.94f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(1040f, 86f));
            RectTransform rect = toastRoot.GetComponent<RectTransform>();
            toastCanvasGroup = toastRoot.AddComponent<CanvasGroup>();
            toastText = CreateText("Toast text", toastRoot.transform, string.Empty, 22, Color.white, TextAnchor.MiddleCenter);
            Stretch(toastText.rectTransform, 28f, 28f, 12f, 12f);
            toastRoot.GetComponent<Image>().raycastTarget = false;
            toastText.raycastTarget = false;
            toastRoot.SetActive(false);
        }

        private void BuildFlash()
        {
            flashRoot = CreateFullPanel("Transition flash", canvas.transform, new Color(0.12f, 0.94f, 0.87f, 0f));
            flashRoot.GetComponent<Image>().raycastTarget = false;
            flashCanvasGroup = flashRoot.AddComponent<CanvasGroup>();
            flashCanvasGroup.alpha = 0f;
            flashCanvasGroup.blocksRaycasts = false;
        }

        private void BuildVignette(Transform parent)
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.name = "Runtime vignette";
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float nx = (x / (float)(texture.width - 1) - 0.5f) * 2f;
                    float ny = (y / (float)(texture.height - 1) - 0.5f) * 2f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.SmoothStep(0f, 0.52f, Mathf.InverseLerp(0.42f, 1.12f, distance));
                    texture.SetPixel(x, y, new Color(0f, 0.01f, 0.02f, alpha));
                }
            }
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f));
            Image vignette = CreateImage("Vignette", parent, Color.white);
            vignette.sprite = sprite;
            vignette.type = Image.Type.Simple;
            vignette.raycastTarget = false;
            RectTransform rect = vignette.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private IEnumerator ToastRoutine()
        {
            yield return new WaitForSeconds(3.2f);
            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = 1f - elapsed / 0.35f;
                yield return null;
            }
            toastRoot.SetActive(false);
            toastRoutine = null;
        }

        private IEnumerator FlashRoutine()
        {
            flashCanvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(0.06f);
            float elapsed = 0f;
            while (elapsed < 0.45f)
            {
                elapsed += Time.unscaledDeltaTime;
                flashCanvasGroup.alpha = Mathf.Clamp01(1f - elapsed / 0.45f);
                yield return null;
            }
            flashCanvasGroup.alpha = 0f;
        }

        private IEnumerator ElectricFlashRoutine()
        {
            for (int i = 0; i < 5; i++)
            {
                flashCanvasGroup.alpha = i % 2 == 0 ? 0.92f : 0.18f;
                yield return new WaitForSecondsRealtime(0.055f);
            }

            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.unscaledDeltaTime;
                flashCanvasGroup.alpha = Mathf.Clamp01(0.8f - elapsed / 0.35f * 0.8f);
                yield return null;
            }
            flashCanvasGroup.alpha = 0f;
        }

        private Text CreateText(string objectName, Transform parent, string content, int size, Color color, TextAnchor alignment, FontStyle style = FontStyle.Normal)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.lineSpacing = 1.08f;
            return text;
        }

        private Button CreateButton(string objectName, Transform parent, string label, Vector2 size, Color background, Color foreground)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = background;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.17f);
            colors.selectedColor = background;
            colors.disabledColor = new Color(0.25f, 0.27f, 0.29f, 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Text text = CreateText("Label", buttonObject.transform, label, 21, foreground, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(text.rectTransform, 12f, 12f, 4f, 4f);
            return button;
        }

        private GameObject CreateFullPanel(string objectName, Transform parent, Color color)
        {
            GameObject panel = new GameObject(objectName);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private GameObject CreatePanel(
            string objectName,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject panel = new GameObject(objectName);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            Place(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, size);
            return panel;
        }

        private Image CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(objectName);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
