using UnityEngine;

namespace NOIChaos
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonPlayer : MonoBehaviour
    {
        private const float WalkSpeed = 3.35f;
        private const float SprintSpeed = 6.2f;
        private const float CrouchSpeed = 1.85f;
        private const float Gravity = -25f;

        private CharacterController controller;
        private Transform visualRoot;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private float verticalVelocity;
        private float animationTime;
        private float footstepTimer;
        private bool controlEnabled;
        private bool crouching;
        private bool seated;
        private Transform seatedChair;
        private float seatedExposureUntil;
        private InteractionPoint currentInteraction;
        private AnswerSource currentAnswerSource;
        private InteractionPoint highlightedInteraction;
        private float paperBallCooldown;
        private float punchCooldown;
        private float punchAnimation;
        private readonly Collider[] interactionHits = new Collider[24];

        public bool IsCrouching => crouching;
        public bool IsSeated => seated;
        public bool IsConcealedAtSeat => seated && Time.time >= seatedExposureUntil;
        public bool IsSprinting { get; private set; }
        public float EyeHeight => seated ? 1.32f : (crouching ? 1.05f : 1.58f);
        public Transform ViewTarget { get; private set; }

        public static ThirdPersonPlayer Create(GameManager manager, Vector3 position)
        {
            GameObject playerObject = new GameObject("Player // 你");
            playerObject.transform.position = position;
            playerObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            CharacterController characterController = playerObject.AddComponent<CharacterController>();
            characterController.height = 1.82f;
            characterController.radius = 0.34f;
            characterController.center = new Vector3(0f, 0.91f, 0f);
            characterController.stepOffset = 0.32f;
            characterController.skinWidth = 0.04f;

            ThirdPersonPlayer player = playerObject.AddComponent<ThirdPersonPlayer>();
            player.controller = characterController;
            player.BuildVisual();
            return player;
        }

        private void BuildVisual()
        {
            visualRoot = new GameObject("Character visual").transform;
            visualRoot.SetParent(transform, false);

            Transform body = CreatePart("Hoodie", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.08f, 0f), new Vector3(0.52f, 0.65f, 0.34f), new Color(0.08f, 0.55f, 0.63f));
            body.localRotation = Quaternion.Euler(0f, 0f, 1.5f);

            CreatePart("Chest accent", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.17f, 0.19f), new Vector3(0.23f, 0.2f, 0.02f), new Color(1f, 0.68f, 0.16f));
            CreatePart("Neck", PrimitiveType.Cylinder, visualRoot, new Vector3(0f, 1.45f, 0f), new Vector3(0.16f, 0.08f, 0.16f), new Color(0.72f, 0.46f, 0.3f));
            CreatePart("Head", PrimitiveType.Sphere, visualRoot, new Vector3(0f, 1.68f, 0f), new Vector3(0.37f, 0.41f, 0.37f), new Color(0.85f, 0.62f, 0.43f));
            CreatePart("Hair", PrimitiveType.Sphere, visualRoot, new Vector3(0f, 1.82f, -0.03f), new Vector3(0.38f, 0.22f, 0.39f), new Color(0.035f, 0.045f, 0.06f));
            CreatePart("Glasses bridge", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.7f, 0.3f), new Vector3(0.25f, 0.035f, 0.035f), new Color(0.1f, 0.1f, 0.12f));
            CreatePart("Left lens", PrimitiveType.Cube, visualRoot, new Vector3(-0.12f, 1.7f, 0.31f), new Vector3(0.18f, 0.12f, 0.025f), new Color(0.1f, 0.75f, 0.76f));
            CreatePart("Right lens", PrimitiveType.Cube, visualRoot, new Vector3(0.12f, 1.7f, 0.31f), new Vector3(0.18f, 0.12f, 0.025f), new Color(0.1f, 0.75f, 0.76f));

            leftArm = CreatePart("Left arm", PrimitiveType.Capsule, visualRoot, new Vector3(-0.39f, 1.0f, 0f), new Vector3(0.17f, 0.48f, 0.17f), new Color(0.06f, 0.43f, 0.52f));
            rightArm = CreatePart("Right arm", PrimitiveType.Capsule, visualRoot, new Vector3(0.39f, 1.0f, 0f), new Vector3(0.17f, 0.48f, 0.17f), new Color(0.06f, 0.43f, 0.52f));
            leftLeg = CreatePart("Left leg", PrimitiveType.Capsule, visualRoot, new Vector3(-0.16f, 0.42f, 0f), new Vector3(0.19f, 0.48f, 0.19f), new Color(0.055f, 0.07f, 0.1f));
            rightLeg = CreatePart("Right leg", PrimitiveType.Capsule, visualRoot, new Vector3(0.16f, 0.42f, 0f), new Vector3(0.19f, 0.48f, 0.19f), new Color(0.055f, 0.07f, 0.1f));
            CreatePart("Backpack", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.12f, -0.3f), new Vector3(0.46f, 0.54f, 0.2f), new Color(0.95f, 0.54f, 0.11f));
            CreatePart("Backpack X", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.12f, -0.412f), new Vector3(0.31f, 0.06f, 0.025f), new Color(0.1f, 0.12f, 0.16f));

            ViewTarget = new GameObject("Camera target").transform;
            ViewTarget.SetParent(transform, false);
            ViewTarget.localPosition = new Vector3(0f, EyeHeight, 0f);
        }

        private void Update()
        {
            GameSession session = GameManager.Instance != null ? GameManager.Instance.Session : null;
            if (session == null || session.Phase != GamePhase.Playing || !controlEnabled)
            {
                SetInteraction(null);
                return;
            }

            if (GameManager.Instance.UI != null && GameManager.Instance.UI.IsNotebookOpen)
            {
                SetInteraction(null);
                return;
            }

            if (seated)
            {
                HandleSeatedActions();
                AnimateSeated();
                if (session.Suspicion > 0f)
                {
                    session.ReduceSuspicion(Time.deltaTime * 0.7f);
                }
                return;
            }

            HandleMovement();
            HandleInteraction();
            Animate();

            if (session.Suspicion > 0f)
            {
                session.ReduceSuspicion(Time.deltaTime * 1.15f);
            }
        }

        public void SetControl(bool enabled)
        {
            controlEnabled = enabled;
        }

        public void SitAt(Transform chair)
        {
            if (seated || chair == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (crouching)
            {
                TryToggleCrouch();
            }

            seated = true;
            seatedChair = chair;
            SetInteraction(null);
            controller.enabled = false;
            transform.position = chair.position + Vector3.up * 0.03f;
            transform.rotation = Quaternion.Euler(0f, chair.eulerAngles.y, 0f);
            visualRoot.localPosition = new Vector3(0f, -0.23f, 0f);
            leftLeg.localRotation = Quaternion.Euler(78f, 0f, -4f);
            rightLeg.localRotation = Quaternion.Euler(78f, 0f, 4f);
            leftArm.localRotation = Quaternion.Euler(-42f, 0f, -8f);
            rightArm.localRotation = Quaternion.Euler(-42f, 0f, 8f);
            ViewTarget.localPosition = new Vector3(0f, EyeHeight, 0f);
            GameManager.Instance.Session.SetSeated(true);
            GameManager.Instance.AudioDirector.PlayConfirm();
            GameManager.Instance.Toast("坐下了。移动键失效：你现在只能观察、偷看、扔纸团或站起。", new Color(0.45f, 1f, 0.8f));
        }

        public void StandUp()
        {
            if (!seated)
            {
                return;
            }

            Vector3 standPosition = seatedChair != null
                ? seatedChair.position + new Vector3(0f, 0.05f, -0.9f)
                : transform.position + Vector3.up * 0.05f;
            seated = false;
            seatedChair = null;
            transform.position = standPosition;
            transform.rotation = Quaternion.identity;
            visualRoot.localPosition = Vector3.zero;
            leftLeg.localRotation = Quaternion.identity;
            rightLeg.localRotation = Quaternion.identity;
            leftArm.localRotation = Quaternion.identity;
            rightArm.localRotation = Quaternion.identity;
            ViewTarget.localPosition = new Vector3(0f, EyeHeight, 0f);
            controller.enabled = true;
            GameManager.Instance.Session.SetSeated(false);
            GameManager.Instance.AudioDirector.PlayStep(false);
            GameManager.Instance.Toast("站起来了。移动、跳跃、奔跑和蹲伏已恢复。");
        }

        public void ExposeFromSeat(float duration)
        {
            if (seated)
            {
                seatedExposureUntil = Mathf.Max(seatedExposureUntil, Time.time + duration);
            }
        }

        private void HandleMovement()
        {
            ThirdPersonCamera playerCamera = GameManager.Instance.PlayerCamera;
            Transform cameraTransform = playerCamera != null ? playerCamera.transform : Camera.main.transform;

            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();

            float horizontal = 0f;
            float vertical = 0f;
            if (Input.GetKey(KeyCode.A))
            {
                horizontal -= 1f;
            }
            if (Input.GetKey(KeyCode.D))
            {
                horizontal += 1f;
            }
            if (Input.GetKey(KeyCode.S))
            {
                vertical -= 1f;
            }
            if (Input.GetKey(KeyCode.W))
            {
                vertical += 1f;
            }

            Vector3 move = (forward * vertical + right * horizontal);
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                TryToggleCrouch();
            }

            bool moving = move.sqrMagnitude > 0.01f;
            IsSprinting = moving && Input.GetKey(KeyCode.LeftShift) && !crouching;
            float speed = crouching ? CrouchSpeed : (IsSprinting ? SprintSpeed : WalkSpeed);
            if (GameManager.Instance.Session.HasCoffeeBoost)
            {
                speed *= 1.16f;
            }

            if (moving)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 13f);
                controller.Move(move * (speed * Time.deltaTime));
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space) && !crouching)
            {
                verticalVelocity = 6.2f;
                GameManager.Instance.ReportNoise(transform.position, 3.5f);
            }

            paperBallCooldown -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.G) && paperBallCooldown <= 0f)
            {
                paperBallCooldown = 0.35f;
                PaperBallProjectile.Throw(GameManager.Instance, GameManager.Instance.PlayerCamera.GetComponent<Camera>(), this);
            }

            punchCooldown -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Q) && punchCooldown <= 0f)
            {
                punchCooldown = 0.48f;
                TryPunch();
            }

            verticalVelocity += Gravity * Time.deltaTime;
            controller.Move(Vector3.up * (verticalVelocity * Time.deltaTime));

            if (moving && controller.isGrounded)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    footstepTimer = IsSprinting ? 0.29f : 0.48f;
                    GameManager.Instance.AudioDirector.PlayStep(IsSprinting);
                    if (IsSprinting)
                    {
                        GameManager.Instance.ReportNoise(transform.position, 5.5f);
                    }
                }
            }
            else
            {
                footstepTimer = 0.1f;
            }
        }

        private void TryToggleCrouch()
        {
            if (!crouching)
            {
                crouching = true;
                controller.height = 1.25f;
                controller.center = new Vector3(0f, 0.625f, 0f);
                visualRoot.localScale = new Vector3(1f, 0.72f, 1f);
                ViewTarget.localPosition = new Vector3(0f, EyeHeight, 0f);
            }
            else
            {
                crouching = false;
                controller.height = 1.82f;
                controller.center = new Vector3(0f, 0.91f, 0f);
                visualRoot.localScale = Vector3.one;
                ViewTarget.localPosition = new Vector3(0f, EyeHeight, 0f);
            }
        }

        private void HandleInteraction()
        {
            FindNearbyTargets(out InteractionPoint found, out AnswerSource answerSource);
            SetInteraction(found);
            currentAnswerSource = answerSource;

            string prompt = string.Empty;
            if (currentInteraction != null)
            {
                prompt = $"[E]  {currentInteraction.Prompt}";
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                {
                    currentInteraction.Interact();
                }
            }

            if (currentAnswerSource != null && currentAnswerSource.CanCopy)
            {
                prompt += (string.IsNullOrEmpty(prompt) ? string.Empty : "     ") + "[F]  偷看并抄下答案";
                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentAnswerSource.Copy(this);
                    currentAnswerSource = null;
                }
            }

            GameManager.Instance.UI.SetPrompt(prompt);
        }

        private void HandleSeatedActions()
        {
            AnswerSource bestSource = null;
            float bestDistance = 6.5f;
            AnswerSource[] sources = FindObjectsOfType<AnswerSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                AnswerSource source = sources[i];
                if (!source.CanCopy)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, source.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSource = source;
                }
            }

            string sourcePrompt = bestSource != null
                ? $"     [F]  侧头偷看 {bestSource.SourceName} 的屏幕"
                : string.Empty;
            GameManager.Instance.UI.SetPrompt($"[E / 左键]  站起{sourcePrompt}     [G]  扔纸团");

            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            {
                StandUp();
                return;
            }

            if (bestSource != null && Input.GetKeyDown(KeyCode.F))
            {
                bestSource.Copy(this);
            }

            paperBallCooldown -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.G) && paperBallCooldown <= 0f)
            {
                paperBallCooldown = 0.45f;
                PaperBallProjectile.Throw(GameManager.Instance, GameManager.Instance.PlayerCamera.GetComponent<Camera>(), this);
                GameManager.Instance.Session.AddSuspicion(4f);
                GameManager.Instance.Toast("你坐着把纸团扔了出去。动作幅度过于明显。", new Color(1f, 0.58f, 0.3f));
            }
        }

        private void AnimateSeated()
        {
            animationTime += Time.deltaTime * 1.4f;
            float breathing = Mathf.Sin(animationTime) * 0.008f;
            visualRoot.localPosition = new Vector3(0f, -0.23f + breathing, 0f);
            leftArm.localRotation = Quaternion.Euler(-42f + Mathf.Sin(animationTime * 0.7f) * 2f, 0f, -8f);
            rightArm.localRotation = Quaternion.Euler(-42f - Mathf.Sin(animationTime * 0.7f) * 2f, 0f, 8f);
        }

        private void TryPunch()
        {
            punchAnimation = 0.22f;
            Camera viewCamera = GameManager.Instance.PlayerCamera.GetComponent<Camera>();
            Vector3 direction = viewCamera.transform.forward;
            direction.y = 0f;
            direction.Normalize();
            Vector3 origin = transform.position + Vector3.up * 1.05f + direction * 0.55f;
            int count = Physics.OverlapSphereNonAlloc(origin, 1.05f, interactionHits, ~0, QueryTriggerInteraction.Collide);
            PunchableTarget nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider hit = interactionHits[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                PunchableTarget target = hit.GetComponentInParent<PunchableTarget>();
                if (target == null)
                {
                    continue;
                }

                Vector3 toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;
                if (Vector3.Dot(direction, toTarget.normalized) < 0.15f)
                {
                    continue;
                }

                float distance = toTarget.sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = target;
                }
            }

            if (nearest != null)
            {
                nearest.ReceivePunch(this);
            }
            else
            {
                GameManager.Instance.ReportNoise(transform.position, 2.5f);
                GameManager.Instance.Toast("你对着空气打了一拳。至少姿势很标准。");
            }
        }

        private void FindNearbyTargets(out InteractionPoint nearestInteraction, out AnswerSource nearestAnswer)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, 2.35f, interactionHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            nearestInteraction = null;
            nearestAnswer = null;
            float nearestDistance = float.MaxValue;
            float nearestAnswerDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider hit = interactionHits[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                float distance = (hit.transform.position - transform.position).sqrMagnitude;
                InteractionPoint point = hit.GetComponentInParent<InteractionPoint>();
                if (point != null && point.IsAvailable && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteraction = point;
                }

                AnswerSource answerSource = hit.GetComponentInParent<AnswerSource>();
                if (answerSource != null && answerSource.CanCopy && distance < nearestAnswerDistance)
                {
                    nearestAnswerDistance = distance;
                    nearestAnswer = answerSource;
                }
            }
        }

        private void SetInteraction(InteractionPoint point)
        {
            currentInteraction = point;
            if (highlightedInteraction == point)
            {
                return;
            }

            if (highlightedInteraction != null)
            {
                highlightedInteraction.SetHighlighted(false);
            }

            highlightedInteraction = point;
            if (highlightedInteraction != null)
            {
                highlightedInteraction.SetHighlighted(true);
            }

            if (GameManager.Instance != null && GameManager.Instance.UI != null)
            {
                GameManager.Instance.UI.SetPrompt(point != null ? $"[E]  {point.Prompt}" : string.Empty);
            }
        }

        private void Animate()
        {
            float planarSpeed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
            float blend = Mathf.Clamp01(planarSpeed / SprintSpeed);
            animationTime += Time.deltaTime * Mathf.Lerp(3f, 10f, blend);
            float swing = Mathf.Sin(animationTime) * (9f + blend * 30f);
            leftArm.localRotation = Quaternion.Euler(swing, 0f, -5f);
            punchAnimation -= Time.deltaTime;
            rightArm.localRotation = punchAnimation > 0f
                ? Quaternion.Euler(-82f, 0f, -4f)
                : Quaternion.Euler(-swing, 0f, 5f);
            leftLeg.localRotation = Quaternion.Euler(-swing * 0.75f, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(swing * 0.75f, 0f, 0f);
            float bob = planarSpeed > 0.1f ? Mathf.Abs(Mathf.Sin(animationTime * 2f)) * 0.035f : 0f;
            visualRoot.localPosition = new Vector3(0f, crouching ? -0.12f + bob * 0.6f : bob, 0f);
        }

        private static Transform CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.layer = LayerMask.NameToLayer("Ignore Raycast");
            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                Destroy(partCollider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            Material material = RenderAssets.CreateStandard(color, 0.05f, type == PrimitiveType.Sphere ? 0.55f : 0.2f);
            if (material != null)
            {
                renderer.material = material;
            }
            return part.transform;
        }
    }
}
