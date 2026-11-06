using UnityEngine;

namespace NOIChaos
{
    /// <summary>
    /// Tiny zero-asset sound layer. Every clip is generated at runtime so the
    /// open-source project remains self-contained and easy to remix.
    /// </summary>
    public sealed class ProceduralAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private AudioSource ambientSource;
        private AudioSource effectsSource;
        private AudioClip stepClip;
        private AudioClip beepClip;
        private AudioClip errorClip;
        private AudioClip successClip;
        private AudioClip caughtClip;

        private void Awake()
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.volume = 0.12f;
            ambientSource.spatialBlend = 0f;

            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.volume = 0.42f;
            effectsSource.spatialBlend = 0f;

            stepClip = CreateNoiseClip("Footstep", 0.1f, 0.3f, 18f);
            beepClip = CreateToneClip("Interface beep", 660f, 0.12f, 0.17f);
            errorClip = CreateChordClip("Error", new[] { 146.8f, 155.6f }, 0.35f, 0.22f);
            successClip = CreateChordClip("Success", new[] { 523.3f, 659.3f, 784f }, 1.2f, 0.2f);
            caughtClip = CreateChordClip("Caught", new[] { 98f, 110f, 130.8f }, 1.1f, 0.24f);
        }

        public void StartAmbient()
        {
            if (ambientSource.clip == null)
            {
                ambientSource.clip = CreateAmbientClip();
                ambientSource.Play();
            }
        }

        public void PlayStep(bool sprinting)
        {
            effectsSource.pitch = sprinting ? 1.28f : Random.Range(0.9f, 1.08f);
            effectsSource.PlayOneShot(stepClip, sprinting ? 0.5f : 0.3f);
        }

        public void PlayConfirm()
        {
            effectsSource.pitch = 1.15f;
            effectsSource.PlayOneShot(beepClip, 0.8f);
        }

        public void PlayError()
        {
            effectsSource.pitch = 1f;
            effectsSource.PlayOneShot(errorClip, 0.75f);
        }

        public void PlayVictory()
        {
            effectsSource.pitch = 1f;
            effectsSource.PlayOneShot(successClip, 0.9f);
        }

        public void PlayCaught()
        {
            effectsSource.pitch = 1f;
            effectsSource.PlayOneShot(caughtClip, 0.9f);
        }

        private static AudioClip CreateToneClip(string name, float frequency, float duration, float amplitude)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 12f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateNoiseClip(string name, float duration, float amplitude, float decay)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[count];
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float raw = Random.Range(-1f, 1f);
                previous = Mathf.Lerp(previous, raw, 0.18f);
                samples[i] = previous * amplitude * Mathf.Exp(-t * decay);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateChordClip(string name, float[] frequencies, float duration, float amplitude)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                float wave = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                {
                    wave += Mathf.Sin(2f * Mathf.PI * frequencies[f] * t);
                }
                samples[i] = wave / frequencies.Length * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateAmbientClip()
        {
            const float duration = 8f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float fade = Mathf.Min(1f, t * 1.5f, (duration - t) * 1.5f);
                float hum =
                    Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.12f +
                    Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.055f +
                    Mathf.Sin(2f * Mathf.PI * 110f * t + 0.8f) * 0.025f;
                samples[i] = hum * fade;
            }

            AudioClip clip = AudioClip.Create("Exam hall ambience", count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
