using UnityEngine;

namespace NOIChaos
{
    /// <summary>
    /// A neighboring terminal that can be inspected for a riskier, faster route
    /// through the exam. Each source can only be copied once per run.
    /// </summary>
    public sealed class AnswerSource : MonoBehaviour
    {
        private int sourceId;
        private string sourceName;
        private string answer;
        private float quality;
        private string examProblemId;
        private string examVariantId;

        public string SourceName => sourceName;
        public string ProblemId => examProblemId;
        public string VariantId => examVariantId;

        public bool CanCopy =>
            GameManager.Instance != null &&
            GameManager.Instance.Session != null &&
            (string.IsNullOrEmpty(examVariantId)
                ? !GameManager.Instance.Session.HasCopiedAnswer(sourceId)
                : !GameManager.Instance.Session.HasCopiedVariant(examProblemId, examVariantId));

        public void Configure(int id, string candidate, string answerText, float answerQuality)
        {
            sourceId = id;
            sourceName = candidate;
            answer = answerText;
            quality = answerQuality;
        }

        public void ConfigureExam(string problemId, string variantId, string candidate)
        {
            examProblemId = problemId;
            examVariantId = variantId;
            sourceName = candidate;
        }

        public void Copy(ThirdPersonPlayer player)
        {
            GameManager game = GameManager.Instance;
            if (game == null || !game.IsPlaying || !CanCopy)
            {
                return;
            }

            if (!string.IsNullOrEmpty(examVariantId))
            {
                ExamSolutionVariant variant = ProblemCatalog.FindVariant(examProblemId, examVariantId);
                string variantName = variant != null ? variant.kind : examVariantId;
                game.Session.RegisterExamCopy(examProblemId, examVariantId, $"{sourceName}：{examProblemId} {variantName}");
            }
            else
            {
                game.Session.AddCopiedAnswer(sourceId, sourceName, answer, quality);
            }
            player.ExposeFromSeat(2.8f);
            float risk = player.IsCrouching ? 5f : (player.IsSeated ? 7f : 11f);
            game.AddSuspicion(risk);
            game.ReportNoise(transform.position, player.IsCrouching ? 1.8f : 3.5f);
            game.AudioDirector.PlayConfirm();

            string warning = player.IsCrouching
                ? "你压低了身体，把答案记进草稿纸。"
                : (player.IsSeated
                    ? "你没有离开座位，只是把头转得有点过分。"
                    : "你站得太直，附近的人可能都看见了。");
            string copiedName = string.IsNullOrEmpty(examVariantId) ? answer : $"{examProblemId} 的 {examVariantId} 代码";
            game.Toast($"抄录：{copiedName}\n{warning}", new Color(1f, 0.7f, 0.27f));
        }
    }
}
