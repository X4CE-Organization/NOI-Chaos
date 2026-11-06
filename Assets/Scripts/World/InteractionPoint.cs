using System;
using UnityEngine;

namespace NOIChaos
{
    public sealed class InteractionPoint : MonoBehaviour
    {
        public string Prompt { get; private set; } = "交互";
        public bool IsAvailable { get; private set; } = true;
        public bool IsHighlighted { get; private set; }

        private Action interactAction;
        private Transform marker;
        private Renderer markerRenderer;
        private Material markerMaterial;
        private float markerSeed;
        private Vector3 markerBasePosition = new Vector3(0f, 0.62f, 0f);

        public void Configure(string prompt, Action action, bool createMarker = true, Vector3 markerOffset = default)
        {
            Prompt = prompt;
            interactAction = action;
            markerSeed = UnityEngine.Random.Range(0f, 10f);
            if (markerOffset != default)
            {
                markerBasePosition = markerOffset;
            }

            if (createMarker)
            {
                BuildMarker();
            }
        }

        public void SetAvailable(bool available)
        {
            IsAvailable = available;
            if (marker != null)
            {
                marker.gameObject.SetActive(available);
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            IsHighlighted = highlighted;
            if (markerRenderer == null)
            {
                return;
            }

            if (highlighted)
            {
                marker.gameObject.SetActive(IsAvailable);
            }
            markerRenderer.sharedMaterial = highlighted ? CreateMaterial(new Color(1f, 0.72f, 0.2f)) : markerMaterial;
            marker.localScale = Vector3.one * (highlighted ? 0.17f : 0.09f);
        }

        public void Interact()
        {
            if (!IsAvailable)
            {
                return;
            }

            interactAction?.Invoke();
        }

        private void Update()
        {
            if (marker == null)
            {
                return;
            }

            ThirdPersonPlayer player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            bool nearPlayer = player != null && (player.transform.position - transform.position).sqrMagnitude < 30f;
            marker.gameObject.SetActive(IsAvailable && (nearPlayer || IsHighlighted));
            if (!marker.gameObject.activeSelf)
            {
                return;
            }

            marker.Rotate(0f, 55f * Time.deltaTime, 0f, Space.World);
            float bob = Mathf.Sin(Time.time * 2.2f + markerSeed) * 0.05f;
            Vector3 local = marker.localPosition;
            local.x = markerBasePosition.x;
            local.y = markerBasePosition.y + bob;
            local.z = markerBasePosition.z;
            marker.localPosition = local;
        }

        private void BuildMarker()
        {
            GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            markerObject.name = "Interaction marker";
            markerObject.transform.SetParent(transform, false);
            markerObject.transform.localPosition = markerBasePosition;
            markerObject.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            markerObject.transform.localScale = Vector3.one * 0.09f;
            Collider markerCollider = markerObject.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            marker = markerObject.transform;
            markerRenderer = markerObject.GetComponent<Renderer>();
            markerMaterial = CreateMaterial(new Color(0.25f, 0.95f, 0.9f));
            if (markerMaterial != null)
            {
                markerRenderer.sharedMaterial = markerMaterial;
            }
        }

        private static Material CreateMaterial(Color color)
        {
            return RenderAssets.CreateStandard(color, 0.1f, 0.1f, 2.5f);
        }
    }
}
