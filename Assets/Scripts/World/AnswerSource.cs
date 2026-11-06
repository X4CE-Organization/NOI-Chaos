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

        public bool CanCopy =>
            GameManager.Instance != null &&
            GameManager.Instance.Session != null &&
            !GameManager.Instance.Session.HasCopiedAnswer(sourceId);

        public void Configure(int id, string candidate, string answerText, float answerQuality)
        {
            sourceId = id;
            sourceName = candidate;
            answer = answerText;
            quality = answerQuality;
        }

        public void Copy(ThirdPersonPlayer player)
        {
            GameManager game = GameManager.Instance;
            if (game == null || !game.IsPlaying || !CanCopy)
            {
                return;
            }

            game.Session.AddCopiedAnswer(sourceId, sourceName, answer, quality);
            float risk = player.IsCrouching ? 5f : 11f;
            game.AddSuspicion(risk);
            game.ReportNoise(transform.position, player.IsCrouching ? 1.8f : 3.5f);
            game.AudioDirector.PlayConfirm();

            string warning = player.IsCrouching
                ? "你压低了身体，把答案记进草稿纸。"
                : "你站得太直，附近的人可能都看见了。";
            game.Toast($"抄录 {game.Session.CopiedAnswerCount}/{game.Session.MaxCopiedAnswers}：{answer}\n{warning}", new Color(1f, 0.7f, 0.27f));
        }
    }
}
