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
        private InteractionPoint currentInteraction;
        private InteractionPoint highlightedInteraction;
        private readonly Collider[] interactionHits = new Collider[24];

        public bool IsCrouching => crouching;
        public bool IsSprinting { get; private set; }
        public float EyeHeight => crouching ? 1.05f : 1.58f;
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
            InteractionPoint found = FindNearestInteraction();
            SetInteraction(found);

            if (currentInteraction != null)
            {
                GameManager.Instance.UI.SetPrompt($"[E]  {currentInteraction.Prompt}");
                if (Input.GetKeyDown(KeyCode.E))
                {
                    currentInteraction.Interact();
                }
            }
            else
            {
                GameManager.Instance.UI.SetPrompt(string.Empty);
            }
        }

        private InteractionPoint FindNearestInteraction()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, 2.35f, interactionHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            InteractionPoint nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider hit = interactionHits[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                InteractionPoint point = hit.GetComponentInParent<InteractionPoint>();
                if (point == null || !point.IsAvailable)
                {
                    continue;
                }

                float distance = (point.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = point;
                }
            }

            return nearest;
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
            rightArm.localRotation = Quaternion.Euler(-swing, 0f, 5f);
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
