using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NOIChaos
{
    [Serializable]
    public sealed class ExamProblemPack
    {
        public ExamProblemDefinition[] problems;
    }

    [Serializable]
    public sealed class ExamProblemDefinition
    {
        public string id;
        public string title;
        public string sourceUrl;
        public string statement;
        public string sampleInput;
        public string sampleOutput;
        public int testCount = 8;
        public ExamSolutionVariant[] variants;
    }

    [Serializable]
    public sealed class ExamSolutionVariant
    {
        public string id;
        public string owner;
        public string kind;
        public string description;
        public string codeResource;
        public int passMask;
        public bool passesSample;
    }

    [Serializable]
    public sealed class ExamTestPack
    {
        public string problemId;
        public int seed;
        public ExamTestCase[] tests;
    }

    [Serializable]
    public sealed class ExamTestCase
    {
        public int index;
        public string input;
        public string output;
    }

    public sealed class ProblemRunState
    {
        private readonly Dictionary<string, int> bestScores = new Dictionary<string, int>();

        public string ProblemId { get; private set; }
        public int[] TestIndices { get; private set; }
        public string BestVariantId { get; private set; } = string.Empty;
        public int BestScore { get; private set; }
        public bool Accepted => BestScore >= 100;

        public ProblemRunState(string problemId, int[] testIndices)
        {
            ProblemId = problemId;
            TestIndices = testIndices;
        }

        public void RecordScore(string variantId, int score)
        {
            bestScores[variantId] = Mathf.Max(GetBestForVariant(variantId), score);
            if (score >= BestScore)
            {
                BestScore = score;
                BestVariantId = variantId;
            }
        }

        public int GetBestForVariant(string variantId)
        {
            return bestScores.TryGetValue(variantId, out int score) ? score : 0;
        }
    }

    public sealed class ExamRunState
    {
        public int Seed { get; private set; }
        public int GoldLine { get; private set; }
        public int SilverLine { get; private set; }
        public int BronzeLine { get; private set; }
        public IReadOnlyList<ExamProblemDefinition> Problems { get; private set; }
        public IReadOnlyList<ProblemRunState> ProblemStates { get; private set; }
        public int TotalScore => ProblemStates.Sum(state => state.BestScore);
        public bool AllAccepted => ProblemStates.All(state => state.Accepted);

        public static ExamRunState Create(IReadOnlyList<ExamProblemDefinition> catalog, int seed)
        {
            System.Random random = new System.Random(seed);
            ExamProblemDefinition ab = catalog.First(problem => problem.id == "P1001");
            ExamProblemDefinition[] blackProblems = catalog
                .Where(problem => problem.id != "P1001")
                .OrderBy(problem => random.Next())
                .Take(2)
                .ToArray();

            List<ExamProblemDefinition> selected = new List<ExamProblemDefinition> { ab };
            selected.AddRange(blackProblems);

            List<ProblemRunState> states = new List<ProblemRunState>();
            for (int i = 0; i < selected.Count; i++)
            {
                ExamSolutionVariant partial = selected[i].variants.FirstOrDefault(variant => variant.kind == "partial");
                int partialMask = partial != null ? partial.passMask : 0;
                states.Add(new ProblemRunState(selected[i].id, PickTests(selected[i].testCount, 5, random, partialMask)));
            }

            return new ExamRunState
            {
                Seed = seed,
                GoldLine = random.Next(200, 261),
                SilverLine = random.Next(130, 181),
                BronzeLine = random.Next(70, 121),
                Problems = selected,
                ProblemStates = states
            };
        }

        public ProblemRunState GetProblemState(string problemId)
        {
            return ProblemStates.First(state => state.ProblemId == problemId);
        }

        public ProblemRunState GetProblemState(int index)
        {
            return ProblemStates[index];
        }

        public string GetMedalName()
        {
            return GetMedalName(TotalScore);
        }

        public string GetMedalName(int score)
        {
            if (score >= GoldLine)
            {
                return "金牌";
            }
            if (score >= SilverLine)
            {
                return "银牌";
            }
            if (score >= BronzeLine)
            {
                return "铜牌";
            }
            return "无牌";
        }

        private static int[] PickTests(int testCount, int pickCount, System.Random random, int partialPassMask)
        {
            int[] indices = Enumerable.Range(0, testCount).ToArray();
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                int temporary = indices[i];
                indices[i] = indices[swap];
                indices[swap] = temporary;
            }
            int[] selected = indices.Take(pickCount).ToArray();
            if (partialPassMask > 0 && partialPassMask != (1 << testCount) - 1)
            {
                int passingSelected = selected.Count(index => (partialPassMask & (1 << index)) != 0);
                int failingSelected = selected.Length - passingSelected;
                if (passingSelected == 0)
                {
                    List<int> passing = Enumerable.Range(0, testCount)
                        .Where(index => (partialPassMask & (1 << index)) != 0)
                        .ToList();
                    selected[random.Next(selected.Length)] = passing[random.Next(passing.Count)];
                }
                else if (failingSelected == 0)
                {
                    List<int> failing = Enumerable.Range(0, testCount)
                        .Where(index => (partialPassMask & (1 << index)) == 0)
                        .ToList();
                    selected[random.Next(selected.Length)] = failing[random.Next(failing.Count)];
                }
            }
            return selected.Distinct().OrderBy(index => index).ToArray();
        }
    }

    public static class ProblemCatalog
    {
        private static ExamProblemDefinition[] cachedProblems;

        public static IReadOnlyList<ExamProblemDefinition> Problems
        {
            get
            {
                EnsureLoaded();
                return cachedProblems;
            }
        }

        public static ExamProblemDefinition Find(string problemId)
        {
            EnsureLoaded();
            return cachedProblems.FirstOrDefault(problem => problem.id == problemId);
        }

        public static ExamSolutionVariant FindVariant(string problemId, string variantId)
        {
            ExamProblemDefinition problem = Find(problemId);
            return problem == null ? null : problem.variants.FirstOrDefault(variant => variant.id == variantId);
        }

        public static string LoadCode(ExamSolutionVariant variant)
        {
            if (variant == null)
            {
                return string.Empty;
            }

            TextAsset asset = Resources.Load<TextAsset>(variant.codeResource);
            if (asset == null)
            {
                Debug.LogError("[NOI Exam] Missing code resource: " + variant.codeResource);
                return "// 离线代码资源缺失";
            }
            return asset.text;
        }

        private static void EnsureLoaded()
        {
            if (cachedProblems != null)
            {
                return;
            }

            TextAsset manifest = Resources.Load<TextAsset>("NOI/Exam/problems");
            if (manifest == null)
            {
                Debug.LogError("[NOI Exam] Missing Resources/NOI/Exam/problems.json");
                cachedProblems = Array.Empty<ExamProblemDefinition>();
                return;
            }

            ExamProblemPack pack = JsonUtility.FromJson<ExamProblemPack>(manifest.text);
            cachedProblems = pack != null && pack.problems != null
                ? pack.problems
                : Array.Empty<ExamProblemDefinition>();
        }
    }

    public static class OfflineJudge
    {
        public const int PointsPerTest = 20;

        public static int Score(ExamProblemDefinition problem, ProblemRunState runState, string variantId)
        {
            ExamSolutionVariant variant = problem.variants.FirstOrDefault(candidate => candidate.id == variantId);
            if (variant == null)
            {
                return 0;
            }

            int passed = 0;
            for (int i = 0; i < runState.TestIndices.Length; i++)
            {
                int testIndex = runState.TestIndices[i];
                if ((variant.passMask & (1 << testIndex)) != 0)
                {
                    passed++;
                }
            }
            return passed * PointsPerTest;
        }
    }
}
