using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioController : MonoSingleton<AudioController> , IAudioManager
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private List<SoundEntry> soundsSources;
    [SerializeField] private float musicStandardDB;

    private Dictionary<string, AudioSource> soundsDictionary = new Dictionary<string, AudioSource>();

    protected override void Awake()
    {
        base.Awake();
        foreach (var sound in soundsSources)
        {
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
        if (!soundsDictionary.ContainsKey(soundId))
        {
            Debug.LogError("Sound with id " + soundId + " not found");
            return;
        }

        if (soundsDictionary[soundId].isPlaying)
        {
            StartCoroutine(FadeOut(soundsDictionary[soundId], 1));
            return;
        }

        PlayNextSound(soundsDictionary[soundId]);
    }

    public void SetMusicVolume(float volume, bool valueOnDb = false)
    {
        float dB = 0;
        if (!valueOnDb)
        {
            if (volume != 0)
            {
                dB = 20.0f * Mathf.Log10(volume);
            }
            else
            {
                dB = -144.0f;
            }
        }
        else
        {
            dB = volume;
        }
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
        float dB = 0;
        if (!valueOnDb)
        {
            if (volume != 0)
            {
                dB = 20.0f * Mathf.Log10(volume);
            }
            else
            {
                dB = -144.0f;
            }
        }
        else
        {
            dB = volume;
        }
        mixer.SetFloat("SoundEffectsVolume", dB);
    }

    private void PlayNextSound(AudioSource currentSource)
    {
        StartCoroutine(FadeIn(currentSource));
    }

    private IEnumerator FadeIn(AudioSource audioSource, float fadeDuration = 1)
    {
        audioSource.volume = 0f;
        audioSource.Play();
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, musicStandardDB, timer / fadeDuration);
            yield return null;
        }

        audioSource.volume = musicStandardDB;
    }


    private IEnumerator FadeOut(AudioSource audioSource, float fadeDuration = 1)
    {
        float startVolume = GetMuiscVolume();

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }

        audioSource.volume = 0f;
    }


    [Serializable]
    public class SoundEntry
    {
        public string name;
        public AudioSource source;
    }
}
