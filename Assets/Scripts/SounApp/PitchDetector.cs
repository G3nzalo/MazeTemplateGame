using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class VocalPitchDetector : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text noteText;
    public TMP_Text freqText;
    public TMP_Text centsText;
    public TMP_Text debugText;

    [Header("Detection")]
    public float minVolume = 0.01f;
    public float clarityThreshold = 0.6f;

    private AudioSource audioSource;
    private AudioClip micClip;
    private string micDevice;

    private const int sampleRate = 44100;
    private const int sampleSize = 2048;

    private float[] samples = new float[sampleSize];

    private bool micReady = false;

    private readonly string[] noteNames =
    {
        "C","C#","D","D#","E","F",
        "F#","G","G#","A","A#","B"
    };

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;

        StartCoroutine(InitMic());
    }

    IEnumerator InitMic()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

        if (Microphone.devices.Length == 0)
        {
            debugText.text = "NO MIC FOUND";
            yield break;
        }

        micDevice = Microphone.devices[0];

        micClip = Microphone.Start(micDevice, true, 10, sampleRate);

        while (Microphone.GetPosition(micDevice) <= 0)
            yield return null;

        audioSource.clip = micClip;
        audioSource.Play();

        micReady = true;

        debugText.text = "Mic Ready";
    }

    void Update()
    {
        if (!micReady)
            return;

        int micPos = Microphone.GetPosition(micDevice);

        if (micPos < sampleSize)
            return;

        micClip.GetData(samples, micPos - sampleSize);

        float rms = CalculateRMS(samples);

        // Noise Gate
        if (rms < minVolume)
        {
            noteText.text = "--";
            freqText.text = "";
            centsText.text = "";
            debugText.text = "No voice";
            return;
        }

        float frequency = DetectPitch(samples, sampleRate);

        // Filtrar frecuencias raras
        if (frequency < 70 || frequency > 1000)
        {
            noteText.text = "--";
            freqText.text = "";
            centsText.text = "";
            debugText.text = "Out of vocal range";
            return;
        }

        DisplayPitch(frequency);

        debugText.text = $"RMS: {rms:F4}";
    }

    float CalculateRMS(float[] data)
    {
        float sum = 0f;

        for (int i = 0; i < data.Length; i++)
            sum += data[i] * data[i];

        return Mathf.Sqrt(sum / data.Length);
    }

    float DetectPitch(float[] data, int rate)
    {
        int maxShift = data.Length / 2;

        float bestCorrelation = 0f;
        int bestLag = -1;

        for (int lag = 20; lag < maxShift; lag++)
        {
            float correlation = 0f;

            for (int i = 0; i < maxShift; i++)
            {
                correlation += data[i] * data[i + lag];
            }

            if (correlation > bestCorrelation)
            {
                bestCorrelation = correlation;
                bestLag = lag;
            }
        }

        if (bestLag == -1)
            return -1;

        float frequency = (float)rate / bestLag;

        return frequency;
    }

    void DisplayPitch(float frequency)
    {
        float midi =
            69 + 12 * Mathf.Log(frequency / 440f, 2);

        int roundedMidi = Mathf.RoundToInt(midi);

        int noteIndex = roundedMidi % 12;
        int octave = (roundedMidi / 12) - 1;

        string noteName = noteNames[noteIndex];

        float noteFreq =
            440f * Mathf.Pow(2f, (roundedMidi - 69) / 12f);

        float cents =
            1200f * Mathf.Log(frequency / noteFreq, 2);

        noteText.text = $"{noteName}{octave}";
        freqText.text = $"{frequency:F2} Hz";
        centsText.text = $"{cents:+0;-0} cents";
    }
}