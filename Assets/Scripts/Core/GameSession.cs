using System;
using System.Collections.Generic;
using UnityEngine;

namespace NOIChaos
{
    public enum GamePhase
    {
        Title,
        Playing,
        Paused,
        Ended
    }

    /// <summary>
    /// Runtime state for one run. Kept separate from scene construction so the
    /// objective, clue and suspicion systems stay easy to test and extend.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        private const float RunDurationSeconds = 9f * 60f;
        public event Action Changed;

        private readonly HashSet<int> clueIds = new HashSet<int>();
        private readonly List<string> clueNames = new List<string>();
        private readonly HashSet<int> copiedAnswerIds = new HashSet<int>();
        private readonly List<string> copiedAnswers = new List<string>();
        private readonly List<string> inventory = new List<string>();
        private readonly HashSet<string> copiedVariantKeys = new HashSet<string>();
        private float copiedQualityTotal;

        public GamePhase Phase { get; private set; } = GamePhase.Title;
        public float Suspicion { get; private set; }
        public string Objective { get; private set; } = "找到发光的 047 号座位并坐下";
        public int ClueCount => clueIds.Count;
        public int CopiedAnswerCount => copiedAnswerIds.Count + copiedVariantKeys.Count;
        public float AverageCopyQuality => copiedAnswerIds.Count == 0 ? 0f : copiedQualityTotal / copiedAnswerIds.Count;
        public int PaperBallsThrown { get; private set; }
        public int WrongSubmissions { get; private set; }
        public int MaxClues => 3;
        public int MaxCopiedAnswers => 6;
        public bool Victory { get; private set; }
        public bool EndingVisible { get; private set; }
        public string EndingTitle { get; private set; } = string.Empty;
        public string EndingBody { get; private set; } = string.Empty;
        public bool WorkstationChecked { get; private set; }
        public bool HasSatDown { get; private set; }
        public bool IsSeated { get; private set; }
        public bool SideRoomFound { get; private set; }
        public bool MedalTouched { get; private set; }
        public float CoffeeBoostUntil { get; private set; }
        public float RunStartedAt { get; private set; }
        public float RemainingSeconds => Mathf.Max(0f, RunDurationSeconds - (Time.time - RunStartedAt));

        public IReadOnlyList<string> ClueNames => clueNames;
        public IReadOnlyList<string> CopiedAnswers => copiedAnswers;
        public IReadOnlyList<string> Inventory => inventory;
        public ExamRunState ExamRun { get; private set; }
        public int ExamScore => ExamRun != null ? ExamRun.TotalScore : 0;

        public void ResetRun()
        {
            clueIds.Clear();
            clueNames.Clear();
            copiedAnswerIds.Clear();
            copiedAnswers.Clear();
            inventory.Clear();
            copiedVariantKeys.Clear();
            copiedQualityTotal = 0f;
            Suspicion = 0f;
            Phase = GamePhase.Playing;
            Victory = false;
            EndingVisible = false;
            EndingTitle = string.Empty;
            EndingBody = string.Empty;
            WorkstationChecked = false;
            HasSatDown = false;
            IsSeated = false;
            SideRoomFound = false;
            MedalTouched = false;
            CoffeeBoostUntil = 0f;
            PaperBallsThrown = 0;
            WrongSubmissions = 0;
            RunStartedAt = Time.time;
            int seed = unchecked(Environment.TickCount ^ DateTime.UtcNow.Millisecond ^ DateTime.UtcNow.DayOfYear);
            ExamRun = ExamRunState.Create(ProblemCatalog.Problems, seed);
            Objective = "找到发光的 047 号座位并坐下";
            RaiseChanged();
        }

        public void SetObjective(string objective)
        {
            Objective = objective;
            RaiseChanged();
        }

        public bool HasClue(int id)
        {
            return clueIds.Contains(id);
        }

        public bool AddClue(int id, string displayName)
        {
            if (!clueIds.Add(id))
            {
                return false;
            }

            clueNames.Add(displayName);
            UpdateOpenObjective();
            RaiseChanged();
            return true;
        }

        public bool AddCopiedAnswer(int id, string source, string answer, float quality)
        {
            if (!copiedAnswerIds.Add(id))
            {
                return false;
            }

            copiedAnswers.Add($"{source}：{answer}  [{Mathf.RoundToInt(quality * 100f)}%]");
            copiedQualityTotal += Mathf.Clamp01(quality);
            UpdateOpenObjective();
            RaiseChanged();
            return true;
        }

        public bool HasCopiedAnswer(int id)
        {
            return copiedAnswerIds.Contains(id);
        }

        public bool RegisterExamCopy(string problemId, string variantId, string displayLabel)
        {
            string key = problemId + ":" + variantId;
            if (!copiedVariantKeys.Add(key))
            {
                return false;
            }

            copiedAnswers.Add(displayLabel);
            UpdateOpenObjective();
            RaiseChanged();
            return true;
        }

        public bool HasCopiedVariant(string problemId, string variantId)
        {
            return copiedVariantKeys.Contains(problemId + ":" + variantId);
        }

        public void RecordExamScore(string problemId, string variantId, int score)
        {
            if (ExamRun == null)
            {
                return;
            }

            ExamRun.GetProblemState(problemId).RecordScore(variantId, score);
            RaiseChanged();
        }

        public void AddItem(string item)
        {
            if (inventory.Contains(item))
            {
                return;
            }

            inventory.Add(item);
            RaiseChanged();
        }

        public bool HasItem(string item)
        {
            return inventory.Contains(item);
        }

        public void RegisterPaperBall()
        {
            PaperBallsThrown++;
            RaiseChanged();
        }

        public void RegisterWrongSubmission()
        {
            WrongSubmissions++;
            RaiseChanged();
        }

        public void MarkWorkstationChecked()
        {
            if (WorkstationChecked)
            {
                return;
            }

            WorkstationChecked = true;
            if (!HasSatDown && clueIds.Count == 0 && copiedAnswerIds.Count == 0)
            {
                Objective = "座位在你的机位后方：靠近椅子按 E 或鼠标左键坐下";
            }
            else
            {
                UpdateOpenObjective();
            }
            RaiseChanged();
        }

        public void SetSeated(bool seated)
        {
            IsSeated = seated;
            if (seated)
            {
                HasSatDown = true;
                Objective = "点击自己的电脑开始答题；F 可侧头偷看邻座代码";
            }
            else
            {
                UpdateOpenObjective();
            }
            RaiseChanged();
        }

        public void MarkSideRoomFound()
        {
            if (SideRoomFound)
            {
                return;
            }

            SideRoomFound = true;
            Objective = "回到考场侧区，翻到那本被遗忘的选手手册";
            RaiseChanged();
        }

        public void MarkMedalTouched()
        {
            MedalTouched = true;
            RaiseChanged();
        }

        public void ActivateCoffeeBoost(float duration)
        {
            CoffeeBoostUntil = Time.time + duration;
            RaiseChanged();
        }

        public bool HasCoffeeBoost => Time.time < CoffeeBoostUntil;

        public void AddSuspicion(float amount)
        {
            if (Phase != GamePhase.Playing)
            {
                return;
            }

            Suspicion = Mathf.Clamp(Suspicion + amount, 0f, 100f);
            RaiseChanged();
        }

        public void ReduceSuspicion(float amount)
        {
            Suspicion = Mathf.Clamp(Suspicion - amount, 0f, 100f);
            RaiseChanged();
        }

        public void Pause()
        {
            if (Phase != GamePhase.Playing)
            {
                return;
            }

            Phase = GamePhase.Paused;
            RaiseChanged();
        }

        public void Resume()
        {
            if (Phase != GamePhase.Paused)
            {
                return;
            }

            Phase = GamePhase.Playing;
            RaiseChanged();
        }

        public void End(bool victory, string title, string body)
        {
            Phase = GamePhase.Ended;
            Victory = victory;
            EndingTitle = title;
            EndingBody = body;
            EndingVisible = true;
            RaiseChanged();
        }

        public void RaiseChanged()
        {
            Changed?.Invoke();
        }

        private void UpdateOpenObjective()
        {
            if (IsSeated)
            {
                Objective = "点击自己的电脑开始答题；F 可侧头偷看邻座代码";
            }
            else if (!HasSatDown && clueIds.Count == 0 && copiedAnswerIds.Count == 0)
            {
                Objective = "找到发光的 047 号座位并坐下";
            }
            else if (clueIds.Count >= MaxClues)
            {
                Objective = "三条灵感已齐。可以交卷，也可以继续把考场搅乱";
            }
            else if (CopiedAnswerCount >= 3)
            {
                Objective = "已经抄到足够多的答案。继续冒险，或者现在就交卷";
            }
            else if (clueIds.Count + CopiedAnswerCount > 0)
            {
                Objective = $"自由行动：灵感 {clueIds.Count}/3 · 抄录 {CopiedAnswerCount}/{MaxCopiedAnswers}";
            }
            else
            {
                Objective = "这不是规定路线：找灵感、抄答案、扔纸团、直接交卷，随你";
            }
        }
    }
}
