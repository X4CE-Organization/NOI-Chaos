using UnityEngine;

namespace NOIChaos
{
    public sealed class PaperBallProjectile : MonoBehaviour
    {
        private GameManager game;
        private bool hasLanded;
        private float lifetime = 7f;

        public static void Throw(GameManager game, Camera viewCamera, ThirdPersonPlayer player)
        {
            if (game == null || viewCamera == null || game.Session == null)
            {
                return;
            }

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Thrown paper ball";
            ball.transform.position = player.transform.position + Vector3.up * (player.IsCrouching ? 1f : 1.35f) + viewCamera.transform.forward * 0.65f;
            ball.transform.localScale = Vector3.one * 0.13f;
            Renderer renderer = ball.GetComponent<Renderer>();
            Material material = RenderAssets.CreateStandard(new Color(0.93f, 0.88f, 0.68f), 0f, 0.1f);
            if (material != null)
            {
                renderer.material = material;
            }

            SphereCollider ballCollider = ball.GetComponent<SphereCollider>();
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider != null)
            {
                Physics.IgnoreCollision(ballCollider, playerCollider);
            }

            Rigidbody body = ball.AddComponent<Rigidbody>();
            body.mass = 0.12f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(viewCamera.transform.forward * 8.5f + Vector3.up * 1.8f, ForceMode.Impulse);

            PaperBallProjectile projectile = ball.AddComponent<PaperBallProjectile>();
            projectile.game = game;
            game.Session.RegisterPaperBall();
            game.Session.AddSuspicion(1.5f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasLanded)
            {
                return;
            }

            hasLanded = true;
            if (game != null)
            {
                game.ReportNoise(transform.position, 7f, true);
                game.AudioDirector.PlayStep(false);
            }
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
