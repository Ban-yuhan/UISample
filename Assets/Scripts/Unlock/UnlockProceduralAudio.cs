using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnlockSystem
{
    /// <summary>
    /// 외부 오디오 파일 없이 절차적 파형(Sine, Triangle, Sawtooth)을 합성해 효과음을 재생합니다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class UnlockProceduralAudio : MonoBehaviour
    {
        public enum WaveType
        {
            Sine,
            Triangle,
            Sawtooth
        }

        [SerializeField] private bool soundEnabled = true;

        private AudioSource _audioSource;
        private readonly List<Coroutine> _activeCoroutines = new List<Coroutine>();
        private const int SampleRate = 44100;

        public bool SoundEnabled
        {
            get => soundEnabled;
            set => soundEnabled = value;
        }

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0f;
            }
        }

        private void OnDestroy()
        {
            StopAllPending();
        }

        public void StopAllPending()
        {
            for (int i = 0; i < _activeCoroutines.Count; i++)
            {
                if (_activeCoroutines[i] != null)
                    StopCoroutine(_activeCoroutines[i]);
            }
            _activeCoroutines.Clear();
        }

        public void PlayButtonClick()
        {
            if (!soundEnabled) return;
            PlayTone(640f, WaveType.Sine, 0.05f, 0.14f, 840f);
        }

        public void PlayCashTick(float progress)
        {
            if (!soundEnabled) return;
            float baseFreq = 460f + Mathf.Clamp01(progress) * 420f;
            PlayTone(baseFreq, WaveType.Triangle, 0.07f, 0.16f, baseFreq * 1.18f);
        }

        public void PlayLockPop()
        {
            if (!soundEnabled) return;
            PlayTone(520f, WaveType.Sine, 0.12f, 0.24f, 880f);
            ScheduleTone(0.09f, 780f, WaveType.Triangle, 0.16f, 0.22f, 1180f);
        }

        public void PlayDropImpact()
        {
            if (!soundEnabled) return;
            PlayTone(145f, WaveType.Sawtooth, 0.28f, 0.28f, 46f);
        }

        public void PlayUnlockFanfare()
        {
            if (!soundEnabled) return;
            PlayTone(523.25f, WaveType.Triangle, 0.16f, 0.20f, 0f);
            ScheduleTone(0.08f, 659.25f, WaveType.Triangle, 0.16f, 0.20f, 0f);
            ScheduleTone(0.16f, 783.99f, WaveType.Triangle, 0.24f, 0.22f, 0f);
            ScheduleTone(0.25f, 1046.50f, WaveType.Sine, 0.35f, 0.24f, 0f);
        }

        private void ScheduleTone(float delay, float startFreq, WaveType wave, float duration, float volume, float endFreq)
        {
            if (!gameObject.activeInHierarchy) return;
            Coroutine c = StartCoroutine(DelayedToneRoutine(delay, startFreq, wave, duration, volume, endFreq));
            _activeCoroutines.Add(c);
        }

        private IEnumerator DelayedToneRoutine(float delay, float startFreq, WaveType wave, float duration, float volume, float endFreq)
        {
            yield return new WaitForSeconds(delay);
            PlayTone(startFreq, wave, duration, volume, endFreq);
        }

        private void PlayTone(float startFreq, WaveType wave, float duration, float volume, float endFreq)
        {
            if (!soundEnabled || _audioSource == null) return;

            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];

            double phase = 0.0;
            bool hasSlide = endFreq > 1f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = hasSlide
                    ? startFreq * Mathf.Pow(endFreq / startFreq, t)
                    : startFreq;

                phase += freq / SampleRate;
                float p = (float)(phase - System.Math.Floor(phase));

                float sample;
                switch (wave)
                {
                    case WaveType.Triangle:
                        sample = 1f - 4f * Mathf.Abs(Mathf.Round(p - 0.25f) - (p - 0.25f));
                        break;
                    case WaveType.Sawtooth:
                        sample = 2f * p - 1f;
                        break;
                    default:
                        sample = Mathf.Sin(p * Mathf.PI * 2f);
                        break;
                }

                // 부드러운 어택(5ms) + 지수 감쇠 엔벨로프
                float attack = Mathf.Clamp01((float)i / (SampleRate * 0.005f));
                float decay = Mathf.Exp(-4.5f * t);
                samples[i] = sample * volume * attack * decay;
            }

            AudioClip clip = AudioClip.Create("ProcTone", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            _audioSource.PlayOneShot(clip, 1f);

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DestroyClipAfterDelay(clip, duration + 0.1f));
            }
        }

        private static IEnumerator DestroyClipAfterDelay(AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (clip != null)
                Destroy(clip);
        }
    }
}
