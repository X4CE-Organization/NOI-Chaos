using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace NOIChaos
{
    public sealed class ExamTerminalUI : MonoBehaviour
    {
        private GameManager game;
        private Canvas canvas;
        private GameObject root;
        private Font uiFont;
        private Font codeFont;
        private Text scoreText;
        private Text problemTitleText;
        private Text problemBodyText;
        private Text codeHeaderText;
        private Text codeText;
        private InputField codeInput;
        private Text verdictText;
        private Text thresholdText;
        private Text earlySubmitLabel;
        private Image earlySubmitImage;
        private ScrollRect problemScroll;
        private ScrollRect codeScroll;
        private RectTransform problemContent;
        private RectTransform codeContent;
        private readonly Button[] problemButtons = new Button[3];
        private readonly List<GameObject> variantButtons = new List<GameObject>();
        private readonly string[] selectedVariantIds = new string[3];
        private readonly string[] editorCode = new string[3];
        private int selectedProblemIndex;
        private bool initialized;
        private bool judgeRunning;
        private int loadedSeed = int.MinValue;
        private bool codeLayoutDirty;
        private float earlySubmitConfirmUntil;

        public bool IsOpen => root != null && root.activeSelf;
        public bool IsCodeEditorReady => codeInput != null;
        public Canvas Canvas => canvas;

        public void Initialize(GameManager manager)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            game = manager;
            uiFont = FontLibrary.GetChineseFont();
            codeFont = FontLibrary.GetMonospaceFont();
            BuildCanvas();
            BuildInterface();
            game.Session.Changed += OnSessionChanged;
            root.SetActive(false);
        }

        public void Open()
        {
            if (!game.IsPlaying || game.Session.ExamRun == null)
            {
                return;
            }

            selectedProblemIndex = 0;
            if (loadedSeed != game.Session.ExamRun.Seed)
            {
                loadedSeed = game.Session.ExamRun.Seed;
                System.Array.Clear(selectedVariantIds, 0, selectedVariantIds.Length);
                System.Array.Clear(editorCode, 0, editorCode.Length);
            }
            root.SetActive(true);
            game.Player.SetControl(false);
            game.PlayerCamera.SetControl(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ResetEarlySubmitButton();
            RefreshAll();
        }

        public void Close()
        {
            CloseInternal(false);
        }

        public void ForceClose()
        {
            CloseInternal(true);
        }

        private void CloseInternal(bool force)
        {
            if (!IsOpen || (judgeRunning && !force))
            {
                return;
            }

            root.SetActive(false);
            if (game.IsPlaying)
            {
                game.Player.SetControl(true);
                game.PlayerCamera.SetControl(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && !judgeRunning)
            {
                Close();
            }
            if (earlySubmitConfirmUntil > 0f && Time.realtimeSinceStartup > earlySubmitConfirmUntil)
            {
                ResetEarlySubmitButton();
            }
        }

        private void OnSessionChanged()
        {
            if (IsOpen)
            {
                RefreshAll();
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("Exam terminal canvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        private void BuildInterface()
        {
            root = CreatePanel("Exam terminal", canvas.transform, new Color(0.012f, 0.023f, 0.034f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text brand = CreateText("Brand", root.transform, "NOI / ONLINE EXAM TERMINAL  //  C++14", 19, new Color(0.38f, 0.88f, 0.82f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(brand.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -34f), new Vector2(780f, 30f));
            scoreText = CreateText("Score", root.transform, string.Empty, 20, new Color(1f, 0.7f, 0.18f), TextAnchor.MiddleRight, FontStyle.Bold);
            Place(scoreText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-46f, -34f), new Vector2(760f, 30f));
            CreateDivider(root.transform, new Vector2(46f, -72f), 1828f, new Color(0.17f, 0.44f, 0.45f, 0.8f));

            GameObject left = CreatePanel("Problem list", root.transform, new Color(0.025f, 0.045f, 0.06f, 1f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(46f, -58f), new Vector2(340f, -138f));
            Text listTitle = CreateText("List title", left.transform, "题目 / PROBLEMS", 17, new Color(0.47f, 0.72f, 0.74f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(listTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -20f), new Vector2(290f, 26f));
            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                Button button = CreateButton("Problem " + (i + 1), left.transform, "T" + (i + 1), new Vector2(292f, 82f), new Color(0.055f, 0.1f, 0.13f), Color.white);
                Place(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -62f - i * 96f), new Vector2(292f, 82f));
                button.onClick.AddListener(() => SelectProblem(captured));
                problemButtons[i] = button;
            }

            Text rules = CreateText(
                "Rules",
                left.transform,
                $"C++14 本地评测\n每题 5 个随机测试点\n每点 20 分，满分 100\n\n每题保留最高提交成绩\n编译器：{(LocalCppJudge.IsCompilerAvailable ? "已就绪" : "未找到")}",
                17,
                new Color(0.53f, 0.61f, 0.63f),
                TextAnchor.UpperLeft);
            Place(rules.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 26f), new Vector2(292f, 210f));
            Button earlySubmit = CreateButton(
                "Early submit",
                left.transform,
                "提前交卷  /  FINAL SUBMIT",
                new Vector2(292f, 58f),
                new Color(0.48f, 0.16f, 0.12f),
                Color.white);
            Place(
                earlySubmit.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(22f, -354f),
                new Vector2(292f, 58f));
            earlySubmit.onClick.AddListener(OnEarlySubmitClicked);
            earlySubmitLabel = earlySubmit.GetComponentInChildren<Text>();
            earlySubmitImage = earlySubmit.GetComponent<Image>();

            GameObject center = CreatePanel("Problem panel", root.transform, new Color(0.02f, 0.039f, 0.052f, 1f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(404f, -58f), new Vector2(710f, -138f));
            problemTitleText = CreateText("Problem title", center.transform, string.Empty, 27, new Color(1f, 0.72f, 0.22f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(problemTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(660f, 38f));
            problemBodyText = CreateScrollView(center.transform, new Vector2(20f, -72f), new Vector2(670f, 830f), uiFont, 19, 28f, out problemContent, out problemScroll);

            GameObject right = CreatePanel("Code panel", root.transform, new Color(0.018f, 0.035f, 0.047f, 1f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-46f, -58f), new Vector2(710f, -138f));
            codeHeaderText = CreateText("Code header", right.transform, "CODE LIBRARY", 18, new Color(0.44f, 0.85f, 0.82f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(codeHeaderText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(660f, 28f));
            thresholdText = CreateText("Thresholds", right.transform, string.Empty, 16, new Color(0.58f, 0.66f, 0.68f), TextAnchor.MiddleRight);
            Place(thresholdText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(550f, 28f));
            codeText = CreateScrollView(right.transform, new Vector2(20f, -240f), new Vector2(670f, 500f), codeFont, 16, 21f, out codeContent, out codeScroll);
            codeText.supportRichText = false;
            codeText.raycastTarget = true;
            codeInput = codeText.gameObject.AddComponent<InputField>();
            codeInput.targetGraphic = codeScroll.viewport.GetComponent<Image>();
            codeInput.textComponent = codeText;
            codeInput.lineType = InputField.LineType.MultiLineNewline;
            codeInput.contentType = InputField.ContentType.Standard;
            codeInput.characterLimit = 1000000;
            codeInput.caretColor = new Color(0.35f, 1f, 0.85f);
            codeInput.selectionColor = new Color(0.15f, 0.55f, 0.55f, 0.45f);
            codeInput.onValueChanged.AddListener(OnCodeChanged);

            Button close = CreateButton("Close", root.transform, "返回考场  /  ESC", new Vector2(230f, 54f), new Color(0.08f, 0.12f, 0.15f), new Color(0.78f, 0.84f, 0.85f));
            Place(close.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-46f, 35f), new Vector2(230f, 54f));
            close.onClick.AddListener(Close);
            Button runSample = CreateButton("Run sample", root.transform, "编译并运行样例", new Vector2(210f, 54f), new Color(0.09f, 0.29f, 0.31f), Color.white);
            Place(runSample.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-312f, 35f), new Vector2(210f, 54f));
            runSample.onClick.AddListener(RunSample);
            Button submit = CreateButton("Submit", root.transform, "编译并提交", new Vector2(190f, 54f), new Color(0.96f, 0.59f, 0.14f), new Color(0.03f, 0.045f, 0.06f));
            Place(submit.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-518f, 35f), new Vector2(190f, 54f));
            submit.onClick.AddListener(SubmitCode);

            verdictText = CreateText("Verdict", right.transform, "可直接编辑代码；提交会调用本机 C++14 编译器。", 16, new Color(0.7f, 0.78f, 0.8f), TextAnchor.UpperLeft);
            Place(verdictText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 94f), new Vector2(670f, 82f));
        }

        private void SelectProblem(int index)
        {
            if (game.Session.ExamRun == null || index < 0 || index >= game.Session.ExamRun.Problems.Count)
            {
                return;
            }
            if (codeInput != null && selectedProblemIndex >= 0 && selectedProblemIndex < editorCode.Length)
            {
                editorCode[selectedProblemIndex] = codeInput.text;
            }
            selectedProblemIndex = index;
            RefreshAll();
        }

        private void SelectVariant(string variantId)
        {
            selectedVariantIds[selectedProblemIndex] = variantId;
            ExamProblemDefinition problem = game.Session.ExamRun.Problems[selectedProblemIndex];
            ExamSolutionVariant variant = ProblemCatalog.FindVariant(problem.id, variantId);
            if (variant != null)
            {
                editorCode[selectedProblemIndex] = ProblemCatalog.LoadCode(variant);
            }
            RefreshCode();
        }

        private void RefreshAll()
        {
            if (!IsOpen || game.Session.ExamRun == null)
            {
                return;
            }

            ExamRunState run = game.Session.ExamRun;
            scoreText.text = $"总分 {run.TotalScore}/300    金 {run.GoldLine}  银 {run.SilverLine}  铜 {run.BronzeLine}";
            thresholdText.text = $"本局奖牌线：金 {run.GoldLine} · 银 {run.SilverLine} · 铜 {run.BronzeLine}";

            for (int i = 0; i < problemButtons.Length; i++)
            {
                ExamProblemDefinition problem = run.Problems[i];
                ProblemRunState state = run.GetProblemState(i);
                Text label = problemButtons[i].GetComponentInChildren<Text>();
                string status = state.Accepted ? "ACCEPTED" : (state.BestScore > 0 ? state.BestScore + "/100" : "待提交");
                label.text = $"T{i + 1}   {problem.id}\n{status}";
                label.fontSize = 18;
                Image image = problemButtons[i].GetComponent<Image>();
                image.color = selectedProblemIndex == i
                    ? new Color(0.09f, 0.31f, 0.32f)
                    : new Color(0.055f, 0.1f, 0.13f);
            }

            RefreshProblem();
            RefreshVariantButtons();
            RefreshCode();
        }

        private void RefreshProblem()
        {
            ExamProblemDefinition problem = game.Session.ExamRun.Problems[selectedProblemIndex];
            problemTitleText.text = $"T{selectedProblemIndex + 1}  {problem.id}  {problem.title}";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(problem.statement);
            builder.AppendLine();
            builder.AppendLine("<color=#62E2D6>公开样例输入</color>");
            builder.AppendLine(problem.sampleInput);
            builder.AppendLine("<color=#62E2D6>公开样例输出</color>");
            builder.AppendLine(problem.sampleOutput);
            builder.AppendLine();
            builder.AppendLine("<color=#F0B34C>洛谷原题</color>");
            builder.AppendLine(problem.sourceUrl);

            problemBodyText.text = builder.ToString();
            float height = Mathf.Max(820f, problemBodyText.preferredHeight + 20f);
            problemContent.sizeDelta = new Vector2(0f, height);
            problemScroll.verticalNormalizedPosition = 1f;
        }

        private void RefreshVariantButtons()
        {
            for (int i = 0; i < variantButtons.Count; i++)
            {
                Destroy(variantButtons[i]);
            }
            variantButtons.Clear();

            ExamProblemDefinition problem = game.Session.ExamRun.Problems[selectedProblemIndex];
            float x = 20f;
            for (int i = 0; i < problem.variants.Length; i++)
            {
                ExamSolutionVariant variant = problem.variants[i];
                bool own = variant.kind == "template";
                bool available = own || game.Session.HasCopiedVariant(problem.id, variant.id);
                if (!available)
                {
                    continue;
                }

                Button button = CreateButton(
                    "Variant " + variant.id,
                    codeHeaderText.transform.parent,
                    variant.owner + " · " + variant.kind,
                    new Vector2(210f, 46f),
                    new Color(0.06f, 0.15f, 0.18f),
                    Color.white);
                Place(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -58f), new Vector2(210f, 46f));
                string capturedVariant = variant.id;
                button.onClick.AddListener(() => SelectVariant(capturedVariant));
                variantButtons.Add(button.gameObject);
                x += 218f;
            }

            if (string.IsNullOrEmpty(selectedVariantIds[selectedProblemIndex])
                || ProblemCatalog.FindVariant(problem.id, selectedVariantIds[selectedProblemIndex]) == null
                || (!game.Session.HasCopiedVariant(problem.id, selectedVariantIds[selectedProblemIndex])
                    && ProblemCatalog.FindVariant(problem.id, selectedVariantIds[selectedProblemIndex]).kind != "template"))
            {
                selectedVariantIds[selectedProblemIndex] = problem.variants[0].id;
            }
            codeHeaderText.text = LocalCppJudge.IsCompilerAvailable
                ? "C++14 CODE EDITOR  ·  本地编译"
                : "CODE LIBRARY  ·  离线兼容模式";
        }

        private void RefreshCode()
        {
            ExamProblemDefinition problem = game.Session.ExamRun.Problems[selectedProblemIndex];
            string variantId = selectedVariantIds[selectedProblemIndex];
            ExamSolutionVariant variant = ProblemCatalog.FindVariant(problem.id, variantId);
            if (variant == null)
            {
                codeText.text = "// 未选择代码版本";
                verdictText.text = "请选择一个已获得的代码版本。";
                return;
            }

            string code = ProblemCatalog.LoadCode(variant);
            if (string.IsNullOrEmpty(editorCode[selectedProblemIndex]))
            {
                editorCode[selectedProblemIndex] = code;
            }
            if (codeInput.text != editorCode[selectedProblemIndex])
            {
                codeInput.SetTextWithoutNotify(editorCode[selectedProblemIndex]);
            }
            UpdateCodeContentHeight(true);

            int best = game.Session.ExamRun.GetProblemState(selectedProblemIndex).BestScore;
            verdictText.text = LocalCppJudge.IsCompilerAvailable
                ? $"{variant.owner} / {variant.kind}\n{variant.description}\n代码可直接编辑；使用 C++14 本地编译。"
                : $"{variant.owner} / {variant.kind}\n{variant.description}\n未找到编译器，只可离线判题库内原始代码。";
            verdictText.color = best >= 100
                ? new Color(0.35f, 0.95f, 0.65f)
                : new Color(0.7f, 0.78f, 0.8f);
        }

        private void RunSample()
        {
            if (!judgeRunning)
            {
                StartCoroutine(JudgeRoutine(false));
            }
        }

        private void SubmitCode()
        {
            if (!judgeRunning)
            {
                StartCoroutine(JudgeRoutine(true));
            }
        }

        private System.Collections.IEnumerator JudgeRoutine(bool submit)
        {
            judgeRunning = true;
            ExamProblemDefinition problem = game.Session.ExamRun.Problems[selectedProblemIndex];
            ProblemRunState state = game.Session.ExamRun.GetProblemState(selectedProblemIndex);
            ExamSolutionVariant selectedVariant = ProblemCatalog.FindVariant(problem.id, selectedVariantIds[selectedProblemIndex]);
            string submittedCode = codeInput.text;
            editorCode[selectedProblemIndex] = submittedCode;

            if (!LocalCppJudge.IsCompilerAvailable)
            {
                bool knownCode = selectedVariant != null
                    && submittedCode.Trim() == ProblemCatalog.LoadCode(selectedVariant).Trim();
                if (!knownCode)
                {
                    verdictText.text = "[COMPILER UNAVAILABLE]\n请安装 clang++ 或 MinGW g++ 后再编译自定义代码。";
                    verdictText.color = new Color(1f, 0.45f, 0.32f);
                    judgeRunning = false;
                    yield break;
                }

                if (submit)
                {
                    int offlineScore = OfflineJudge.Score(problem, state, selectedVariant.id);
                    game.Session.RecordExamScore(problem.id, selectedVariant.id, offlineScore);
                    verdictText.text = $"[OFFLINE FALLBACK] {offlineScore}/100\n当前平台没有编译器，仅验证了题库内原始代码。";
                    verdictText.color = offlineScore >= 100
                        ? new Color(0.35f, 0.95f, 0.65f)
                        : new Color(1f, 0.66f, 0.26f);
                }
                else
                {
                    verdictText.text = selectedVariant.passesSample
                        ? "[OFFLINE SAMPLE PASSED]\n" + problem.sampleOutput
                        : "[OFFLINE SAMPLE FAILED]";
                }
                judgeRunning = false;
                yield break;
            }

            verdictText.text = submit ? "正在编译并运行 5 个隐藏测试点..." : "正在编译并运行公开样例...";
            verdictText.color = new Color(0.7f, 0.78f, 0.8f);
            Task<ExamJudgeResult> task = LocalCppJudge.JudgeAsync(problem, state, submittedCode, !submit);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            if (task.IsFaulted)
            {
                verdictText.text = "[JUDGE ERROR]\n" + task.Exception.GetBaseException().Message;
                verdictText.color = new Color(1f, 0.45f, 0.32f);
                judgeRunning = false;
                yield break;
            }

            ExamJudgeResult result = task.Result;
            if (!game.IsPlaying)
            {
                judgeRunning = false;
                yield break;
            }
            if (!result.CompileSuccess)
            {
                verdictText.text = result.Message;
                verdictText.color = new Color(1f, 0.45f, 0.32f);
                judgeRunning = false;
                yield break;
            }

            if (!submit)
            {
                verdictText.text = $"[{result.Message.ToUpperInvariant()}]\n{result.Output}";
                verdictText.color = result.Message == "Sample Passed"
                    ? new Color(0.35f, 0.95f, 0.65f)
                    : new Color(1f, 0.66f, 0.26f);
            }
            else
            {
                game.Session.RecordExamScore(problem.id, "editor", result.Score);
                verdictText.text = $"{result.Message}\n{result.Score}/100\n{result.Output}";
                verdictText.color = result.Score >= 100
                    ? new Color(0.35f, 0.95f, 0.65f)
                    : new Color(1f, 0.66f, 0.26f);
                game.AudioDirector.PlayConfirm();
                game.Session.SetObjective($"考试进行中：{problem.id} 当前 {result.Score}/100，总分 {game.Session.ExamScore}/300");
            }
            judgeRunning = false;
        }

        private void OnCodeChanged(string code)
        {
            if (selectedProblemIndex >= 0 && selectedProblemIndex < editorCode.Length)
            {
                editorCode[selectedProblemIndex] = code;
            }
            if (codeContent != null)
            {
                codeLayoutDirty = true;
            }
        }

        private void OnEarlySubmitClicked()
        {
            if (judgeRunning)
            {
                verdictText.text = "编译评测正在进行，请等待结果后再交卷。";
                verdictText.color = new Color(1f, 0.66f, 0.26f);
                return;
            }

            if (Time.realtimeSinceStartup <= earlySubmitConfirmUntil)
            {
                game.SubmitInsight();
                return;
            }

            earlySubmitConfirmUntil = Time.realtimeSinceStartup + 3.5f;
            earlySubmitLabel.text = "确认交卷？  再点一次";
            earlySubmitImage.color = new Color(0.78f, 0.19f, 0.12f);
            verdictText.text = "提前交卷将按当前每题最高分结算，未作答题目计 0 分。";
            verdictText.color = new Color(1f, 0.66f, 0.26f);
        }

        private void ResetEarlySubmitButton()
        {
            if (earlySubmitLabel == null)
            {
                return;
            }
            earlySubmitConfirmUntil = 0f;
            earlySubmitLabel.text = "提前交卷  /  FINAL SUBMIT";
            earlySubmitImage.color = new Color(0.48f, 0.16f, 0.12f);
        }

        private void LateUpdate()
        {
            if (IsOpen && codeLayoutDirty)
            {
                UpdateCodeContentHeight(false);
            }
        }

        private void UpdateCodeContentHeight(bool resetScroll)
        {
            if (codeContent == null || codeText == null)
            {
                return;
            }

            float previousScroll = codeScroll.verticalNormalizedPosition;
            Canvas.ForceUpdateCanvases();
            int lineCount = Mathf.Max(1, codeInput.text.Split('\n').Length);
            float requiredHeight = Mathf.Max(
                codeScroll.viewport.rect.height,
                codeText.preferredHeight + 28f,
                lineCount * 21f + 20f);
            codeContent.sizeDelta = new Vector2(0f, requiredHeight);
            Canvas.ForceUpdateCanvases();
            codeScroll.verticalNormalizedPosition = resetScroll ? 1f : Mathf.Clamp01(previousScroll);
            codeHeaderText.text = LocalCppJudge.IsCompilerAvailable
                ? $"C++14 CODE EDITOR  ·  {lineCount} 行  ·  本地编译"
                : $"CODE LIBRARY  ·  {lineCount} 行  ·  离线兼容模式";
            codeLayoutDirty = false;
        }

        private void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private GameObject CreatePanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            return CreatePanel(name, parent, color, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), position, size);
        }

        private GameObject CreatePanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return panel;
        }

        private Text CreateText(string name, Transform parent, string content, int size, Color color, TextAnchor alignment, FontStyle style = FontStyle.Normal)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = uiFont;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 size, Color background, Color foreground)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = background;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Text text = CreateText("Label", buttonObject.transform, label, 17, foreground, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8f, 4f);
            text.rectTransform.offsetMax = new Vector2(-8f, -4f);
            return button;
        }

        private Text CreateScrollView(Transform parent, Vector2 position, Vector2 size, Font font, int fontSize, float lineHeight, out RectTransform content, out ScrollRect scroll)
        {
            GameObject scrollObject = new GameObject("Scroll view");
            scrollObject.transform.SetParent(parent, false);
            RectTransform scrollRectTransform = scrollObject.AddComponent<RectTransform>();
            Place(scrollRectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObject.transform, false);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.25f);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewport.transform, false);
            content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, size.y);

            Text text = CreateText("Text", contentObject.transform, string.Empty, fontSize, new Color(0.78f, 0.84f, 0.84f), TextAnchor.UpperLeft);
            text.font = font;
            text.lineSpacing = lineHeight / Mathf.Max(1f, fontSize);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(16f, 12f);
            text.rectTransform.offsetMax = new Vector2(-16f, -12f);

            scroll = scrollObject.AddComponent<ScrollRect>();
            scrollObject.AddComponent<Image>().color = Color.clear;
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;

            GameObject scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(scrollObject.transform, false);
            Image scrollbarBackground = scrollbarObject.AddComponent<Image>();
            scrollbarBackground.color = new Color(0.05f, 0.1f, 0.12f, 0.92f);
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = new Vector2(-2f, 0f);
            scrollbarRect.sizeDelta = new Vector2(10f, 0f);

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(0.22f, 0.74f, 0.72f, 0.92f);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(2f, 2f);
            handleRect.offsetMax = new Vector2(-2f, -2f);

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarSpacing = 0f;
            return text;
        }

        private void CreateDivider(Transform parent, Vector2 position, float width, Color color)
        {
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            Image image = divider.AddComponent<Image>();
            image.color = color;
            Place(image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(width, 1f));
        }
    }
}
