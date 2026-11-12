using System.Collections.Generic;
using UnityEngine;

namespace LogisticsTycoon.Settlement
{
    public enum ReceiptSound
    {
        Feed,
        PopPositive,
        PopNegative,
        Stamp,
        CalmSlide,
        Tear
    }

    /// <summary>
    /// 원본 HTML(Web Audio)의 효과음 6종을 같은 파라미터로 PCM 합성해 AudioClip으로 만든다.
    /// 실제 사운드 에셋이 준비되면 overrideClips에 넣으면 그쪽이 우선한다.
    /// </summary>
    public class ReceiptSfx : MonoBehaviour
    {
        public AudioSource source;
        public bool muted;
        [Range(0f, 2f)] public float volume = 1f;

        [System.Serializable]
        public struct ClipOverride
        {
            public ReceiptSound sound;
            public AudioClip clip;
        }

        public ClipOverride[] overrideClips;

        private const int Rate = 44100;
        private readonly Dictionary<ReceiptSound, AudioClip> clips = new Dictionary<ReceiptSound, AudioClip>();

        private void Awake()
        {
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
        }

        public void Play(ReceiptSound sound)
        {
            if (muted || source == null) return;
            AudioClip clip = Get(sound);
            if (clip != null) source.PlayOneShot(clip, volume);
        }

        private AudioClip Get(ReceiptSound sound)
        {
            if (overrideClips != null)
            {
                foreach (var o in overrideClips)
                    if (o.sound == sound && o.clip != null) return o.clip;
            }
            if (!clips.TryGetValue(sound, out AudioClip clip) || clip == null)
            {
                clip = Synthesize(sound);
                clips[sound] = clip;
            }
            return clip;
        }

        // ------------------------------------------------------------------ synthesis

        private enum Wave { Sine, Saw, Triangle }

        private static AudioClip Synthesize(ReceiptSound sound)
        {
            float[] buf;
            switch (sound)
            {
                case ReceiptSound.Feed:
                    buf = new float[Samples(0.17f)];
                    Osc(buf, Wave.Saw, 0f, 0.17f, 190f, 250f, 0.14f, false, 0.05f, 0.001f, 0.16f);
                    break;
                case ReceiptSound.PopPositive:
                    buf = new float[Samples(0.045f + 0.24f)];
                    Osc(buf, Wave.Sine, 0f, 0.24f, 523.25f, 523.25f, 0f, false, 0.16f, 0.001f, 0.22f);
                    Osc(buf, Wave.Sine, 0.045f, 0.24f, 783.99f, 783.99f, 0f, false, 0.16f, 0.001f, 0.22f);
                    break;
                case ReceiptSound.PopNegative:
                    buf = new float[Samples(0.21f)];
                    Osc(buf, Wave.Triangle, 0f, 0.21f, 340f, 175f, 0.18f, true, 0.16f, 0.001f, 0.2f);
                    break;
                case ReceiptSound.Stamp:
                    buf = new float[Samples(0.19f)];
                    Osc(buf, Wave.Triangle, 0f, 0.19f, 145f, 32f, 0.15f, true, 0.75f, 0.01f, 0.18f);
                    break;
                case ReceiptSound.CalmSlide:
                    buf = new float[Samples(0.15f)];
                    Osc(buf, Wave.Sine, 0f, 0.15f, 320f, 410f, 0.12f, true, 0.06f, 0.001f, 0.14f);
                    break;
                default:
                    buf = TearNoise();
                    break;
            }

            var clip = AudioClip.Create("Receipt_" + sound, buf.Length, 1, Rate, false);
            clip.SetData(buf, 0);
            return clip;
        }

        private static int Samples(float seconds) => Mathf.CeilToInt(seconds * Rate);

        private static void Osc(float[] buf, Wave wave, float start, float length,
            float f0, float f1, float fRamp, bool expFreq, float g0, float g1, float gRamp)
        {
            int s0 = Samples(start);
            int n = Mathf.Min(Samples(length), buf.Length - s0);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float ft = fRamp > 0f ? Mathf.Clamp01(t / fRamp) : 1f;
                float f = expFreq ? f0 * Mathf.Pow(f1 / f0, ft) : Mathf.Lerp(f0, f1, ft);
                float gt = gRamp > 0f ? Mathf.Clamp01(t / gRamp) : 1f;
                float g = g0 * Mathf.Pow(g1 / g0, gt);

                phase += f / Rate;
                float frac = (float)(phase - System.Math.Floor(phase));
                float s;
                switch (wave)
                {
                    case Wave.Saw: s = 2f * frac - 1f; break;
                    case Wave.Triangle: s = 1f - 4f * Mathf.Abs(frac - 0.5f); break;
                    default: s = Mathf.Sin(frac * Mathf.PI * 2f); break;
                }
                buf[s0 + i] += s * g;
            }
        }

        private static float[] TearNoise()
        {
            int n = Samples(0.22f);
            var buf = new float[n];
            var rng = new System.Random(1234);

            // RBJ band-pass (constant 0 dB peak), f = 1800 Hz, Q = 1 (Web Audio 기본값)
            double w0 = 2 * System.Math.PI * 1800 / Rate;
            double alpha = System.Math.Sin(w0) / 2.0;
            double a0 = 1 + alpha;
            double b0 = alpha / a0, b2 = -alpha / a0;
            double a1 = -2 * System.Math.Cos(w0) / a0, a2 = (1 - alpha) / a0;
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;

            for (int i = 0; i < n; i++)
            {
                double x = (rng.NextDouble() * 2 - 1) * System.Math.Exp(-i / (n * 0.45));
                double y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;

                float t = i / (float)Rate;
                float g = 0.28f * Mathf.Pow(0.01f / 0.28f, Mathf.Clamp01(t / 0.21f));
                buf[i] = (float)y * g;
            }
            return buf;
        }
    }
}
