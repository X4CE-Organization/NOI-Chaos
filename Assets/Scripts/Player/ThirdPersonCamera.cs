using UnityEngine;

namespace NOIChaos
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        private ThirdPersonPlayer target;
        private Camera viewCamera;
        private bool controlEnabled;
        private bool cinematic = true;
        private float yaw;
        private float pitch = 12f;
        private float distance = 4.35f;
        private Vector3 velocity;

        public static ThirdPersonCamera Create(GameManager manager, ThirdPersonPlayer player)
        {
            GameObject cameraObject = new GameObject("Third-person camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 61f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.045f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();

            ThirdPersonCamera rig = cameraObject.AddComponent<ThirdPersonCamera>();
            rig.target = player;
            rig.viewCamera = camera;
            return rig;
        }

        public void SetControl(bool enabled)
        {
            controlEnabled = enabled;
            if (enabled)
            {
                cinematic = false;
                yaw = target.transform.eulerAngles.y;
                pitch = 13f;
                distance = 4.35f;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (cinematic)
            {
                yaw += Time.deltaTime * 4.5f;
                pitch = 10f + Mathf.Sin(Time.time * 0.3f) * 3f;
                distance = 8.4f;
            }
            else if (controlEnabled)
            {
                yaw += Input.GetAxisRaw("Mouse X") * 3.4f;
                pitch -= Input.GetAxisRaw("Mouse Y") * 2.7f;
                pitch = Mathf.Clamp(pitch, -20f, 58f);
                distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 0.45f, 2.4f, 6.5f);
            }

            Vector3 focus = target.transform.position + Vector3.up * target.EyeHeight + target.transform.right * 0.2f;
            if (cinematic)
            {
                focus = new Vector3(0f, 1.8f, -1.4f);
            }

            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredOffset = orbit * Vector3.back * distance;
            Vector3 desiredPosition = focus + desiredOffset;

            Vector3 castDirection = desiredPosition - focus;
            float castDistance = castDirection.magnitude;
            if (castDistance > 0.01f)
            {
                RaycastHit hit;
                if (Physics.SphereCast(focus, 0.18f, castDirection.normalized, out hit, castDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.transform.IsChildOf(target.transform) && !hit.collider.isTrigger)
                    {
                        desiredPosition = focus + castDirection.normalized * Mathf.Max(0.65f, hit.distance - 0.12f);
                    }
                }
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 0.055f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus - transform.position), 1f - Mathf.Exp(-12f * Time.deltaTime));
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, target.IsSprinting ? 66f : 61f, Time.deltaTime * 4f);
        }
    }
}
