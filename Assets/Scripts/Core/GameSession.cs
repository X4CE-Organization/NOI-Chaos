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
        private float copiedQualityTotal;

        public GamePhase Phase { get; private set; } = GamePhase.Title;
        public float Suspicion { get; private set; }
        public string Objective { get; private set; } = "确认第一题的提交状态";
        public int ClueCount => clueIds.Count;
        public int CopiedAnswerCount => copiedAnswerIds.Count;
        public float AverageCopyQuality => copiedAnswerIds.Count == 0 ? 0f : copiedQualityTotal / copiedAnswerIds.Count;
        public int PaperBallsThrown { get; private set; }
        public int WrongSubmissions { get; private set; }
        public int MaxClues => 3;
        public int MaxCopiedAnswers => 4;
        public bool Victory { get; private set; }
        public bool EndingVisible { get; private set; }
        public string EndingTitle { get; private set; } = string.Empty;
        public string EndingBody { get; private set; } = string.Empty;
        public bool WorkstationChecked { get; private set; }
        public bool SideRoomFound { get; private set; }
        public bool MedalTouched { get; private set; }
        public float CoffeeBoostUntil { get; private set; }
        public float RunStartedAt { get; private set; }
        public float RemainingSeconds => Mathf.Max(0f, RunDurationSeconds - (Time.time - RunStartedAt));

        public IReadOnlyList<string> ClueNames => clueNames;
        public IReadOnlyList<string> CopiedAnswers => copiedAnswers;
        public IReadOnlyList<string> Inventory => inventory;

        public void ResetRun()
        {
            clueIds.Clear();
            clueNames.Clear();
            copiedAnswerIds.Clear();
            copiedAnswers.Clear();
            inventory.Clear();
            copiedQualityTotal = 0f;
            Suspicion = 0f;
            Phase = GamePhase.Playing;
            Victory = false;
            EndingVisible = false;
            EndingTitle = string.Empty;
            EndingBody = string.Empty;
            WorkstationChecked = false;
            SideRoomFound = false;
            MedalTouched = false;
            CoffeeBoostUntil = 0f;
            PaperBallsThrown = 0;
            WrongSubmissions = 0;
            RunStartedAt = Time.time;
            Objective = "随便走走。先确认自己的机位，或者直接开始搞事";
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
            UpdateOpenObjective();
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
            if (clueIds.Count >= MaxClues)
            {
                Objective = "三条灵感已齐。可以交卷，也可以继续把考场搅乱";
            }
            else if (copiedAnswerIds.Count >= 3)
            {
                Objective = "已经抄到足够多的答案。继续冒险，或者现在就交卷";
            }
            else if (clueIds.Count + copiedAnswerIds.Count > 0)
            {
                Objective = $"自由行动：灵感 {clueIds.Count}/3 · 抄录 {copiedAnswerIds.Count}/{MaxCopiedAnswers}";
            }
            else
            {
                Objective = "这不是规定路线：找灵感、抄答案、扔纸团、直接交卷，随你";
            }
        }
    }
}
