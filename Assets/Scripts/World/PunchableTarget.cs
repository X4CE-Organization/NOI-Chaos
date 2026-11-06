using System.Collections;
using UnityEngine;

namespace NOIChaos
{
    public sealed class PunchableTarget : MonoBehaviour
    {
        private string displayName;
        private bool authority;
        private Transform recoilVisual;
        private Coroutine recoilRoutine;

        public string DisplayName => displayName;

        public void Configure(string targetName, bool isAuthority, Transform visual)
        {
            displayName = targetName;
            authority = isAuthority;
            recoilVisual = visual;
        }

        public void ReceivePunch(ThirdPersonPlayer attacker)
        {
            GameManager game = GameManager.Instance;
            if (game == null || !game.IsPlaying)
            {
                return;
            }

            game.AudioDirector.PlayPunch();
            game.Session.AddSuspicion(authority ? 48f : 24f);
            game.ReportNoise(transform.position, 12f, true);
            game.AlertSecurity(transform.position, "有人打起来了");
            game.Toast(
                authority
                    ? $"你打了{displayName}。整个考场瞬间安静，保安正在赶来。"
                    : $"你一拳打中了{displayName}。对方开始大声呼叫监考老师。",
                new Color(1f, 0.38f, 0.24f));

            if (recoilVisual != null)
            {
                if (recoilRoutine != null)
                {
                    StopCoroutine(recoilRoutine);
                }
                recoilRoutine = StartCoroutine(RecoilRoutine());
            }
        }

        private IEnumerator RecoilRoutine()
        {
            Quaternion start = recoilVisual.localRotation;
            recoilVisual.localRotation = start * Quaternion.Euler(0f, 0f, -15f);
            yield return new WaitForSeconds(0.16f);
            recoilVisual.localRotation = start * Quaternion.Euler(0f, 0f, 9f);
            yield return new WaitForSeconds(0.12f);
            recoilVisual.localRotation = start;
            recoilRoutine = null;
        }
    }
}
