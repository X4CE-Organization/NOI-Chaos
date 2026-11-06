using UnityEngine;
using UnityEngine.Rendering;

namespace NOIChaos
{
    public sealed class InvigilatorAgent : MonoBehaviour
    {
        private enum AgentState
        {
            Patrol,
            Investigate,
            Observe
        }

        private GameManager game;
        private Vector3[] route;
        private int routeIndex;
        private int direction = 1;
        private AgentState state;
        private Vector3 investigatePoint;
        private float investigateTimer;
        private float waitTimer;
        private float scanTimer;
        private bool seesPlayer;
        private float speed = 1.55f;
        private Transform visualRoot;
        private Transform head;
        private Transform leftArm;
        private Transform rightArm;
        private Transform alertMarker;
        private TextMesh alertText;
        private Renderer coneRenderer;
        private Material coneMaterial;
        private float animationTime;
        private Color patrolConeColor = new Color(1f, 0.68f, 0.2f, 0.065f);
        private Color alertConeColor = new Color(1f, 0.17f, 0.12f, 0.16f);

        public bool SeesPlayer => seesPlayer;

        public void Configure(GameManager manager, Vector3[] patrolRoute, Color uniformColor, string displayName)
        {
            game = manager;
            route = patrolRoute;
            routeIndex = 0;
            name = "Invigilator // " + displayName;
            BuildVisual(uniformColor);
            BuildVisionCone();
            state = AgentState.Patrol;
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            bool active = game.Session != null && game.Session.Phase == GamePhase.Playing && game.Player != null;
            if (coneRenderer != null)
            {
                coneRenderer.enabled = active;
            }
            if (alertMarker != null)
            {
                alertMarker.gameObject.SetActive(active && (seesPlayer || state == AgentState.Investigate));
            }

            if (!active)
            {
                seesPlayer = false;
                return;
            }

            seesPlayer = CanSeePlayer();
            if (seesPlayer)
            {
                investigatePoint = game.Player.transform.position;
                investigateTimer = 4.2f;
                state = AgentState.Observe;

                float distance = Vector3.Distance(transform.position, game.Player.transform.position);
                float suspicionRate = Mathf.Lerp(20f, 7f, Mathf.Clamp01(distance / 11f));
                if (game.Player.IsCrouching)
                {
                    suspicionRate *= 0.5f;
                }
                if (game.Player.IsSprinting)
                {
                    suspicionRate *= 1.45f;
                }
                game.AddSuspicion(suspicionRate * Time.deltaTime);

                if (distance < 1.45f)
                {
                    game.Caught("你与监考老师的距离已经小于一次编译错误。");
                    return;
                }
            }
            else if (state == AgentState.Observe)
            {
                state = AgentState.Investigate;
            }

            UpdateConeColor();
            UpdateAlertMarker();
            UpdateState();
            Animate();
        }

        public void AlertTo(Vector3 position, float radius, bool dramatic)
        {
            if (game == null || game.Session.Phase != GamePhase.Playing)
            {
                return;
            }

            if (Vector3.Distance(transform.position, position) > radius)
            {
                return;
            }

            investigatePoint = position;
            investigateTimer = dramatic ? 7.5f : 3.2f;
            state = AgentState.Investigate;
            waitTimer = 0f;
        }

        public void Distract(Vector3 position, float duration)
        {
            investigatePoint = position;
            investigateTimer = duration;
            state = AgentState.Investigate;
            waitTimer = 0f;
        }

        private bool CanSeePlayer()
        {
            if (game.Player == null)
            {
                return false;
            }

            Vector3 eye = transform.position + Vector3.up * 1.7f;
            Vector3 target = game.Player.transform.position + Vector3.up * game.Player.EyeHeight;
            Vector3 toPlayer = target - eye;
            float distance = toPlayer.magnitude;
            float viewDistance = game.Player.IsCrouching ? 7.4f : 10.2f;
            if (distance > viewDistance)
            {
                return false;
            }

            float angle = Vector3.Angle(transform.forward, new Vector3(toPlayer.x, 0f, toPlayer.z));
            float viewAngle = state == AgentState.Observe ? 72f : 58f;
            if (angle > viewAngle)
            {
                return false;
            }

            return !Physics.Raycast(eye, toPlayer.normalized, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void UpdateState()
        {
            if (state == AgentState.Observe)
            {
                Vector3 face = game.Player.transform.position - transform.position;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.LookRotation(face.normalized),
                        Time.deltaTime * 150f);
                }
                return;
            }

            if (state == AgentState.Investigate)
            {
                investigateTimer -= Time.deltaTime;
                MoveToward(investigatePoint, 2.2f);
                if (Vector3.Distance(transform.position, investigatePoint) < 1.2f)
                {
                    waitTimer += Time.deltaTime;
                    if (waitTimer > 1.1f)
                    {
                        investigateTimer -= Time.deltaTime * 3f;
                    }
                }

                if (investigateTimer <= 0f)
                {
                    state = AgentState.Patrol;
                    waitTimer = 0f;
                }
                return;
            }

            if (route == null || route.Length == 0)
            {
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                scanTimer += Time.deltaTime;
                float scan = Mathf.Sin(scanTimer * 1.6f) * 24f;
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + scan * Time.deltaTime, 0f);
                return;
            }

            MoveToward(route[routeIndex], speed);
            if (Vector3.Distance(transform.position, route[routeIndex]) < 0.4f)
            {
                waitTimer = Random.Range(1.2f, 2.4f);
                scanTimer = 0f;
                routeIndex += direction;
                if (routeIndex >= route.Length)
                {
                    direction = -1;
                    routeIndex = route.Length - 2;
                }
                else if (routeIndex < 0)
                {
                    direction = 1;
                    routeIndex = 1;
                }
                routeIndex = Mathf.Clamp(routeIndex, 0, route.Length - 1);
            }
        }

        private void MoveToward(Vector3 target, float movementSpeed)
        {
            Vector3 directionToTarget = target - transform.position;
            directionToTarget.y = 0f;
            float distance = directionToTarget.magnitude;
            if (distance < 0.04f)
            {
                return;
            }

            Vector3 direction = directionToTarget / distance;
            transform.position += direction * Mathf.Min(movementSpeed * Time.deltaTime, distance);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                1f - Mathf.Exp(-7f * Time.deltaTime));
        }

        private void Animate()
        {
            float speed = state == AgentState.Observe ? 0f : 1f;
            animationTime += Time.deltaTime * (1.4f + speed * 4.8f);
            float swing = Mathf.Sin(animationTime) * 7f * speed;
            leftArm.localRotation = Quaternion.Euler(swing, 0f, -3f);
            rightArm.localRotation = Quaternion.Euler(-swing, 0f, 3f);
            visualRoot.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(animationTime * 2f)) * 0.018f, 0f);
            head.localRotation = Quaternion.Euler(
                Mathf.Sin(Time.time * 0.8f) * (state == AgentState.Patrol ? 2.5f : 0f),
                Mathf.Sin(Time.time * 1.25f) * (state == AgentState.Patrol ? 14f : 0f),
                0f);
        }

        private void UpdateConeColor()
        {
            if (coneMaterial == null)
            {
                return;
            }

            Color target = seesPlayer ? alertConeColor : patrolConeColor;
            Color current = coneMaterial.color;
            Color next = Color.Lerp(current, target, Time.deltaTime * 7f);
            coneMaterial.color = next;
            if (coneMaterial.HasProperty("_EmissionColor"))
            {
                coneMaterial.SetColor("_EmissionColor", next * 1.3f);
            }
        }

        private void UpdateAlertMarker()
        {
            if (alertText == null)
            {
                return;
            }

            alertText.text = seesPlayer ? "!" : "?";
            alertMarker.localPosition = new Vector3(0f, 2.35f + Mathf.Sin(Time.time * 4f) * 0.08f, 0f);
            alertMarker.rotation = Camera.main != null
                ? Quaternion.LookRotation(alertMarker.position - Camera.main.transform.position)
                : Quaternion.identity;
            alertText.color = seesPlayer ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.74f, 0.2f);
        }

        private void BuildVisual(Color uniformColor)
        {
            visualRoot = new GameObject("Invigilator visual").transform;
            visualRoot.SetParent(transform, false);

            CreatePart("Torso", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.08f, 0f), new Vector3(0.62f, 0.75f, 0.37f), uniformColor);
            CreatePart("Shirt", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.2f, 0.198f), new Vector3(0.28f, 0.4f, 0.025f), new Color(0.87f, 0.91f, 0.92f));
            head = CreatePart("Head", PrimitiveType.Sphere, visualRoot, new Vector3(0f, 1.65f, 0f), new Vector3(0.35f, 0.4f, 0.35f), new Color(0.78f, 0.58f, 0.42f));
            CreatePart("Hair", PrimitiveType.Sphere, visualRoot, new Vector3(0f, 1.8f, -0.02f), new Vector3(0.36f, 0.19f, 0.37f), new Color(0.04f, 0.035f, 0.03f));
            CreatePart("Glasses", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.68f, 0.29f), new Vector3(0.3f, 0.07f, 0.035f), new Color(0.03f, 0.05f, 0.06f));
            leftArm = CreatePart("Left arm", PrimitiveType.Capsule, visualRoot, new Vector3(-0.42f, 1.0f, 0f), new Vector3(0.18f, 0.5f, 0.18f), uniformColor);
            rightArm = CreatePart("Right arm", PrimitiveType.Capsule, visualRoot, new Vector3(0.42f, 1.0f, 0f), new Vector3(0.18f, 0.5f, 0.18f), uniformColor);
            CreatePart("Left leg", PrimitiveType.Capsule, visualRoot, new Vector3(-0.17f, 0.39f, 0f), new Vector3(0.2f, 0.45f, 0.2f), new Color(0.08f, 0.1f, 0.14f));
            CreatePart("Right leg", PrimitiveType.Capsule, visualRoot, new Vector3(0.17f, 0.39f, 0f), new Vector3(0.2f, 0.45f, 0.2f), new Color(0.08f, 0.1f, 0.14f));

            alertMarker = new GameObject("Alert marker").transform;
            alertMarker.SetParent(transform, false);
            TextMesh text = alertMarker.gameObject.AddComponent<TextMesh>();
            text.text = "?";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 80;
            text.characterSize = 0.12f;
            text.color = new Color(1f, 0.74f, 0.2f);
            text.fontStyle = FontStyle.Bold;
            alertText = text;
            alertMarker.gameObject.SetActive(false);
        }

        private void BuildVisionCone()
        {
            GameObject coneObject = new GameObject("Vision cone");
            coneObject.transform.SetParent(transform, false);
            coneObject.transform.localPosition = new Vector3(0f, 0.055f, 0.1f);
            MeshFilter filter = coneObject.AddComponent<MeshFilter>();
            coneRenderer = coneObject.AddComponent<MeshRenderer>();

            const int segments = 18;
            float halfAngle = 58f;
            float range = 10.2f;
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(angle) * range, 0f, Mathf.Cos(angle) * range);
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            Mesh mesh = new Mesh { name = "Vision wedge" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            filter.sharedMesh = mesh;

            coneMaterial = RenderAssets.CreateStandard(patrolConeColor, 0f, 0.05f, 1.3f);
            if (coneMaterial == null)
            {
                return;
            }
            coneMaterial.name = "Vision cone material";
            coneMaterial.color = patrolConeColor;
            coneMaterial.SetFloat("_Mode", 3f);
            coneMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            coneMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            coneMaterial.SetInt("_ZWrite", 0);
            coneMaterial.DisableKeyword("_ALPHATEST_ON");
            coneMaterial.EnableKeyword("_ALPHABLEND_ON");
            coneMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            coneMaterial.renderQueue = 3000;
            coneMaterial.EnableKeyword("_EMISSION");
            coneMaterial.SetColor("_EmissionColor", patrolConeColor * 1.3f);
            coneRenderer.material = coneMaterial;
            coneRenderer.shadowCastingMode = ShadowCastingMode.Off;
            coneRenderer.receiveShadows = false;
        }

        private static Transform CreatePart(string partName, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                Destroy(partCollider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            Material material = RenderAssets.CreateStandard(color, 0.08f, 0.25f);
            if (material == null)
            {
                return part.transform;
            }
            renderer.material = material;
            return part.transform;
        }
    }
}
