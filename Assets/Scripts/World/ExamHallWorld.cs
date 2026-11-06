using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace NOIChaos
{
    public sealed class ExamHallWorld : MonoBehaviour
    {
        private readonly List<InvigilatorAgent> invigilators = new List<InvigilatorAgent>();
        private HallEffects effects;

        public IReadOnlyList<InvigilatorAgent> Invigilators => invigilators;

        public void RegisterInvigilator(InvigilatorAgent agent)
        {
            if (agent != null)
            {
                invigilators.Add(agent);
            }
        }

        public void SetEffects(HallEffects hallEffects)
        {
            effects = hallEffects;
        }

        public void AlertInvigilators(Vector3 position, float radius, bool dramatic)
        {
            for (int i = 0; i < invigilators.Count; i++)
            {
                invigilators[i].AlertTo(position, radius, dramatic);
            }
        }

        public void AlertSecurity(Vector3 position)
        {
            for (int i = 0; i < invigilators.Count; i++)
            {
                if (invigilators[i].IsSecurityGuard)
                {
                    invigilators[i].AlertTo(position, float.MaxValue, true);
                }
            }
        }

        public void DistractInvigilators(Vector3 position, float duration)
        {
            for (int i = 0; i < invigilators.Count; i++)
            {
                invigilators[i].Distract(position, duration);
            }
        }

        public void ToggleLights()
        {
            if (effects != null)
            {
                effects.ToggleLights();
            }
        }

        public void FreezeClock()
        {
            if (effects != null)
            {
                effects.FreezeClock();
            }
        }
    }

    public sealed class HallEffects : MonoBehaviour
    {
        private readonly List<Light> ceilingLights = new List<Light>();
        private readonly List<float> baseIntensities = new List<float>();
        private bool lightsOff;
        private bool dimming;
        private Transform clockHand;
        private float clockSpin;
        private Color originalAmbient;

        public void Initialize(IEnumerable<Light> lights, Transform minuteHand)
        {
            foreach (Light light in lights)
            {
                if (light != null)
                {
                    ceilingLights.Add(light);
                    baseIntensities.Add(light.intensity);
                }
            }
            clockHand = minuteHand;
            originalAmbient = RenderSettings.ambientLight;
        }

        public void ToggleLights()
        {
            StopAllCoroutines();
            dimming = true;
            StartCoroutine(ToggleLightsRoutine());
        }

        public void FreezeClock()
        {
            clockSpin = 14f;
        }

        private IEnumerator ToggleLightsRoutine()
        {
            lightsOff = !lightsOff;
            int flickers = 7;
            for (int i = 0; i < flickers; i++)
            {
                SetLightLevel(lightsOff ? 0.1f : 1.1f);
                yield return new WaitForSeconds(0.045f);
                SetLightLevel(lightsOff ? 1f : 0.25f);
                yield return new WaitForSeconds(0.035f);
            }

            SetLightLevel(lightsOff ? 0.22f : 1f);
            RenderSettings.ambientLight = lightsOff ? originalAmbient * 0.34f : originalAmbient;
            dimming = false;
        }

        private void Update()
        {
            if (clockHand != null && clockSpin > 0f)
            {
                float rotation = Mathf.Min(clockSpin, 360f);
                clockHand.Rotate(0f, 0f, rotation * Time.deltaTime, Space.Self);
                clockSpin = Mathf.Max(0f, clockSpin - Time.deltaTime * 36f);
            }

            if (!dimming && lightsOff && Random.value < Time.deltaTime * 0.8f)
            {
                StartCoroutine(SmallFlicker());
            }
        }

        private IEnumerator SmallFlicker()
        {
            float before = 0.22f;
            SetLightLevel(0.08f);
            yield return new WaitForSeconds(0.05f);
            SetLightLevel(before);
        }

        private void SetLightLevel(float multiplier)
        {
            for (int i = 0; i < ceilingLights.Count; i++)
            {
                ceilingLights[i].intensity = baseIntensities[i] * multiplier;
            }
        }
    }
}
