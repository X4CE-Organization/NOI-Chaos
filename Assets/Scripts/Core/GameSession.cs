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

        public GamePhase Phase { get; private set; } = GamePhase.Title;
        public float Suspicion { get; private set; }
        public string Objective { get; private set; } = "确认第一题的提交状态";
        public int ClueCount => clueIds.Count;
        public int MaxClues => 3;
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

        public void ResetRun()
        {
            clueIds.Clear();
            clueNames.Clear();
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
            RunStartedAt = Time.time;
            Objective = "回到自己的机位，确认第一题已提交";
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
            if (clueIds.Count == 1)
            {
                Objective = "收集 3 条灵感，再前往讲台旁的评测终端";
            }
            else if (clueIds.Count >= MaxClues)
            {
                Objective = "三条灵感已齐：避开监考，提交第二题思路";
            }

            RaiseChanged();
            return true;
        }

        public void MarkWorkstationChecked()
        {
            if (WorkstationChecked)
            {
                return;
            }

            WorkstationChecked = true;
            Objective = "观察考场，寻找关于第二题的 3 条灵感";
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
    }
}
