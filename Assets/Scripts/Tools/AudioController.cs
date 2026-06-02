using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Maze.Tools
{
    public class AudioController : MonoSingleton<AudioController>, IAudioManager
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private List<SoundEntry> soundsSources;

        [Tooltip("Volumen objetivo (lineal 0-1) al que sube el fade-in.")]
        [FormerlySerializedAs("musicStandardDB")]
        [Range(0f, 1f)]
        [SerializeField] private float musicStandardVolume = 1f;

        private Dictionary<string, AudioSource> soundsDictionary = new Dictionary<string, AudioSource>();

        // Evita que varios fades peleen por el volumen del mismo source.
        private readonly Dictionary<AudioSource, Coroutine> fadeRoutines = new Dictionary<AudioSource, Coroutine>();

        protected override void Awake()
        {
            base.Awake();
            foreach (var sound in soundsSources)
            {
                if (soundsDictionary.ContainsKey(sound.name))
                {
                    Debug.LogWarning("Duplicate sound id ignored: " + sound.name);
                    continue;
                }

                soundsDictionary.Add(sound.name, sound.source);
            }
        }

        private void Start()
        {
            PlaySound("SoundTrackGame");
        }

        public float GetMuiscVolume()
        {
            float volume = 0;
            mixer.GetFloat("MusicVolume", out volume);
            return volume;
        }

        public float GetSfxVolume()
        {
            float volume = 0;
            mixer.GetFloat("SoundEffectsVolume", out volume);
            return volume;
        }

        public void PlaySound(string soundId)
        {
            if (!soundsDictionary.TryGetValue(soundId, out AudioSource source))
            {
                Debug.LogError("Sound with id " + soundId + " not found");
                return;
            }

            // Comportamiento toggle: si ya suena, hace fade-out; si no, fade-in.
            if (source.isPlaying)
            {
                StartFade(source, FadeOut(source, 1));
                return;
            }

            StartFade(source, FadeIn(source, 1));
        }

        public void SetMusicVolume(float volume, bool valueOnDb = false)
        {
            float dB = valueOnDb ? volume : LinearToDb(volume);
            mixer.SetFloat("MusicVolume", dB);
        }

        public void SetPitch(float pitchValue)
        {
            mixer.SetFloat("PitchParameter", pitchValue);
        }

        public void SetRandomPitch()
        {
            float randomPitch = UnityEngine.Random.Range(0.5f, 2.0f);
            SetPitch(randomPitch);
        }

        public void SetSoundsEffectsVolume(float volume, bool valueOnDb = false)
        {
            float dB = valueOnDb ? volume : LinearToDb(volume);
            mixer.SetFloat("SoundEffectsVolume", dB);
        }

        // Convierte un volumen lineal (0-1) a decibelios para el mixer.
        private static float LinearToDb(float volume)
        {
            return volume > 0f ? 20f * Mathf.Log10(volume) : -144f;
        }

        // Cancela cualquier fade en curso sobre este source antes de iniciar otro.
        private void StartFade(AudioSource source, IEnumerator routine)
        {
            if (fadeRoutines.TryGetValue(source, out Coroutine running) && running != null)
                StopCoroutine(running);

            fadeRoutines[source] = StartCoroutine(routine);
        }

        private IEnumerator FadeIn(AudioSource audioSource, float fadeDuration = 1)
        {
            float target = Mathf.Clamp01(musicStandardVolume);

            audioSource.volume = 0f;
            audioSource.Play();
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(0f, target, timer / fadeDuration);
                yield return null;
            }

            audioSource.volume = target;
        }


        private IEnumerator FadeOut(AudioSource audioSource, float fadeDuration = 1)
        {
            // Parte del volumen real del source (lineal), no del dB del mixer.
            float startVolume = audioSource.volume;

            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
                yield return null;
            }

            audioSource.volume = 0f;
            audioSource.Stop();
        }


        [Serializable]
        public class SoundEntry
        {
            public string name;
            public AudioSource source;
        }
    }

}
