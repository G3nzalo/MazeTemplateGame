using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Detector de afinación vocal basado en YIN (Cheveigné & Kawahara, 2002).
//
// Pipeline (spec .claude/instructions.md):
//   Mic -> HPF 60Hz / LPF 1200Hz (IIR biquad) -> ventana 1024 con hop 512 (50% overlap)
//        -> gate de silencio (dBFS) + gate de consonantes (ZCR)
//        -> YIN (difference function + CMNDF + umbral + interpolación parabólica)
//        -> mediana rodante (suavizado de vibrato) -> UI / captura para el juego.
//
// El análisis se dispara por HOP de muestras, NO por frame de Unity, así el
// resultado es independiente del frame rate del dispositivo.
public class VocalPitchDetector : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text noteText;
    public TMP_Text freqText;
    public TMP_Text centsText;
    public TMP_Text debugText;

    [Header("Detection")]
    [Tooltip("Umbral de silencio en dBFS. Por debajo se descarta el bloque (spec: -40 dBFS).")]
    public float silenceFloorDb = -40f;

    [Tooltip("Umbral de ZCR (cruces por cero / muestras). Por encima = consonante/ruido percusivo.")]
    [Range(0.05f, 0.6f)]
    public float zcrThreshold = 0.3f;

    [Tooltip("Claridad mínima (0-1) para aceptar un pitch. " +
             "Más alto = más estricto contra ruido/sonidos no tonales.")]
    [Range(0f, 1f)]
    public float clarityThreshold = 0.6f;

    [Tooltip("Rango vocal de búsqueda (Hz). Coherente con HPF/LPF.")]
    public float vocalMinHz = 65f;
    public float vocalMaxHz = 1200f;

    [Tooltip("Nº de valores para la mediana rodante (suavizado de vibrato, spec: 3-5).")]
    [Range(1, 9)]
    public int medianWindow = 5;

    [Tooltip("Si está activo, detecta siempre (afinador en vivo). " +
             "Si no, solo detecta entre BeginDetection/EndDetection (ahorra CPU).")]
    public bool runContinuously = true;

    // ---- Configuración fija de análisis (spec: bloque 1024, overlap 50%) ----
    private const int requestedSampleRate = 44100;
    private const int sampleSize = 1024;          // bloque de análisis
    private const int hopSize = 512;              // 50% overlap
    private const int prerollSamples = 512;       // pre-roll para asentar los filtros IIR
    private const int micLengthSec = 2;           // buffer circular del micrófono

    // Umbral absoluto YIN sobre la CMNDF (spec ~0.1).
    private const float yinThreshold = 0.1f;

    // ---- Estado del micrófono ----
    private AudioClip micClip;
    private string micDevice;
    private int actualSampleRate;                 // frecuencia REAL del clip del micro
    private int clipSamples;
    private bool micReady;

    private int lastMicPos;
    private long wraps;
    private long nextWindowStart = -1;            // inicio absoluto de la próxima ventana

    // ---- Buffers reutilizados (sin asignaciones por frame) ----
    private readonly float[] filterInput = new float[sampleSize + prerollSamples];
    private readonly float[] samples = new float[sampleSize];
    private readonly float[] yinDiff = new float[sampleSize];
    private readonly float[] yinCmnd = new float[sampleSize];

    private Biquad hpf;
    private Biquad lpf;

    // ---- Suavizado de vibrato (mediana rodante para el display) ----
    private readonly List<float> recentPitches = new List<float>();

    // ---- Salida ----
    private float currentPitch;
    private float lastClarity;

    public float CurrentPitch => currentPitch;
    public bool HasValidPitch => currentPitch > 0f;
    public float Clarity => lastClarity;
    public int SampleRate => actualSampleRate;

    // Emite el F0 CRUDO de cada hop analizado (Hz), o 0 si el bloque no es tonal.
    // Lo consume la UI de feedback (gráfico en tiempo real).
    public event System.Action<float> OnPitchSample;

    // ---- API on-demand (cuando runContinuously = false) ----
    private bool detectionRequested;
    public void BeginDetection() => detectionRequested = true;
    public void EndDetection() => detectionRequested = false;

    // ---- API de captura para el juego (frames crudos por hop) ----
    private bool capturing;
    private readonly List<float> capturedPitches = new List<float>();

    public void BeginCapture()
    {
        capturedPitches.Clear();
        capturing = true;
        detectionRequested = true;
    }

    // Descarta lo capturado hasta ahora (p.ej. para ignorar el ataque inicial).
    public void ClearCapture() => capturedPitches.Clear();

    public List<float> EndCapture()
    {
        capturing = false;
        if (!runContinuously)
            detectionRequested = false;
        return new List<float>(capturedPitches);
    }

    void Start()
    {
        StartCoroutine(InitMic());
    }

    IEnumerator InitMic()
    {
        yield return null;

#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
            UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(
                UnityEngine.Android.Permission.Microphone);
        }

        while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
            UnityEngine.Android.Permission.Microphone))
        {
            yield return null;
        }
#endif

        yield return new WaitForSeconds(0.2f);

        if (Microphone.devices.Length == 0)
        {
            if (debugText) debugText.text = "NO MIC FOUND";
            yield break;
        }

        micDevice = Microphone.devices[0];

        // 🔥 importante en Android
        Microphone.End(micDevice);

        micClip = Microphone.Start(
            micDevice,
            true,
            micLengthSec,
            requestedSampleRate);

        // Sample rate REAL que entregó el dispositivo (puede no ser 44100).
        actualSampleRate = micClip.frequency;
        clipSamples = micClip.samples;

        // Filtros pasa-banda (HPF 60 Hz, LPF 1200 Hz) a la frecuencia real.
        hpf = Biquad.HighPass(60f, actualSampleRate, 0.707f);
        lpf = Biquad.LowPass(1200f, actualSampleRate, 0.707f);

        while (Microphone.GetPosition(micDevice) <= 0)
            yield return null;

        lastMicPos = Microphone.GetPosition(micDevice);
        micReady = true;

        if (debugText) debugText.text = $"Mic Ready @ {actualSampleRate} Hz";
        Debug.Log($"[VocalPitchDetector] Sample rate real del dispositivo: {actualSampleRate} Hz");
    }

    void Update()
    {
        if (!micReady)
            return;

        // Fuera de modo continuo solo procesa bajo petición.
        if (!runContinuously && !detectionRequested)
            return;

        long absPos = GetAbsolutePosition();

        if (nextWindowStart < 0)
            nextWindowStart = absPos - sampleSize;

        // No quedarse atrás del buffer circular (si hubo un stall, salta).
        long minStart = absPos - clipSamples + prerollSamples + 1;
        if (nextWindowStart < minStart)
            nextWindowStart = minStart;

        // Procesa TODAS las ventanas pendientes a cadencia de hop (independiente del FPS).
        while (nextWindowStart + sampleSize <= absPos)
        {
            long prerollStart = nextWindowStart - prerollSamples;

            bool inRing =
                prerollStart >= 0 &&
                prerollStart >= absPos - clipSamples;

            if (inRing)
            {
                ReadAndFilterWindow(prerollStart);
                ProcessWindow();
            }

            nextWindowStart += hopSize;
        }
    }

    long GetAbsolutePosition()
    {
        int micPos = Microphone.GetPosition(micDevice);

        if (micPos < lastMicPos)
            wraps++;

        lastMicPos = micPos;

        return wraps * clipSamples + micPos;
    }

    // Lee [prerollStart, prerollStart+sampleSize+prerollSamples) del micro,
    // lo filtra (HPF+LPF) reseteando el estado y precalentando con el pre-roll,
    // y deja la ventana de análisis (1024) en 'samples'.
    void ReadAndFilterWindow(long prerollStart)
    {
        int offset = (int)(prerollStart % clipSamples);
        if (offset < 0) offset += clipSamples;

        micClip.GetData(filterInput, offset);

        hpf.Reset();
        lpf.Reset();

        for (int i = 0; i < filterInput.Length; i++)
        {
            float s = hpf.Process(filterInput[i]);
            s = lpf.Process(s);

            int outIdx = i - prerollSamples;
            if (outIdx >= 0 && outIdx < sampleSize)
                samples[outIdx] = s;
        }
    }

    void ProcessWindow()
    {
        float rms = CalculateRMS(samples);
        float dbfs = rms > 0f ? 20f * Mathf.Log10(rms) : -160f;

        // GATE DE SILENCIO (dBFS)
        if (dbfs < silenceFloorDb)
        {
            RegisterInvalid("No voice");
            return;
        }

        // GATE DE CONSONANTES (ZCR alto = ruido percusivo, no tonal)
        if (ComputeZcr(samples) > zcrThreshold)
        {
            RegisterInvalid("Unvoiced");
            return;
        }

        // YIN (frame crudo, sin ventana: YIN no la requiere — spec C7)
        float detectedPitch = DetectPitch(samples, actualSampleRate);

        if (detectedPitch <= 0f ||
            detectedPitch < vocalMinHz ||
            detectedPitch > vocalMaxHz)
        {
            RegisterInvalid("Out of range");
            return;
        }

        // Frame crudo para la lógica del juego (porcentaje de afinación).
        if (capturing)
            capturedPitches.Add(detectedPitch);

        OnPitchSample?.Invoke(detectedPitch);

        // Mediana rodante SOLO para el display (no penaliza el micro-vibrato).
        recentPitches.Add(detectedPitch);
        if (recentPitches.Count > medianWindow)
            recentPitches.RemoveAt(0);

        currentPitch = Median(recentPitches);

        DisplayPitch(currentPitch);

        if (debugText)
            debugText.text =
                $"Pitch: {currentPitch:F2} Hz\n" +
                $"Raw: {detectedPitch:F2}\n" +
                $"dBFS: {dbfs:F1} | clarity: {lastClarity:F2}";
    }

    void RegisterInvalid(string reason)
    {
        currentPitch = 0f;
        recentPitches.Clear();

        if (noteText) noteText.text = "--";
        if (freqText) freqText.text = "";
        if (centsText) centsText.text = "";
        if (debugText) debugText.text = reason;

        OnPitchSample?.Invoke(0f);
    }

    float CalculateRMS(float[] data)
    {
        float sum = 0f;
        for (int i = 0; i < data.Length; i++)
            sum += data[i] * data[i];

        return Mathf.Sqrt(sum / data.Length);
    }

    float ComputeZcr(float[] data)
    {
        int crossings = 0;
        for (int i = 1; i < data.Length; i++)
        {
            if ((data[i] >= 0f) != (data[i - 1] >= 0f))
                crossings++;
        }
        return (float)crossings / data.Length;
    }

    // YIN: difference function + CMNDF + umbral absoluto + interpolación parabólica.
    float DetectPitch(float[] data, int rate)
    {
        int minTau = Mathf.Max(1, Mathf.FloorToInt(rate / vocalMaxHz));
        int maxTau = Mathf.FloorToInt(rate / vocalMinHz);

        if (maxTau >= data.Length)
            maxTau = data.Length - 1;

        // 1) Difference function: d(tau) = Σ (x[j] - x[j+tau])²
        yinDiff[0] = 0f;
        for (int tau = 1; tau <= maxTau; tau++)
        {
            float sum = 0f;
            int count = data.Length - tau;

            for (int j = 0; j < count; j++)
            {
                float delta = data[j] - data[j + tau];
                sum += delta * delta;
            }

            yinDiff[tau] = sum;
        }

        // 2) Cumulative mean normalized difference.
        yinCmnd[0] = 1f;
        float running = 0f;
        for (int tau = 1; tau <= maxTau; tau++)
        {
            running += yinDiff[tau];

            yinCmnd[tau] = running > 0f
                ? yinDiff[tau] * tau / running
                : 1f;
        }

        // 3) Umbral absoluto: primer mínimo local bajo yinThreshold.
        int bestTau = -1;
        for (int tau = minTau; tau <= maxTau; tau++)
        {
            if (yinCmnd[tau] < yinThreshold)
            {
                while (tau + 1 <= maxTau &&
                       yinCmnd[tau + 1] < yinCmnd[tau])
                {
                    tau++;
                }

                bestTau = tau;
                break;
            }
        }

        // Fallback: mínimo global en rango si nada cruzó el umbral.
        if (bestTau == -1)
        {
            float min = float.MaxValue;
            for (int tau = minTau; tau <= maxTau; tau++)
            {
                if (yinCmnd[tau] < min)
                {
                    min = yinCmnd[tau];
                    bestTau = tau;
                }
            }
        }

        if (bestTau <= 0)
        {
            lastClarity = 0f;
            return -1f;
        }

        // Claridad = qué tan periódica es la señal (1 = tono puro).
        lastClarity = Mathf.Clamp01(1f - yinCmnd[bestTau]);

        if (lastClarity < clarityThreshold)
            return -1f;

        // 4) Interpolación parabólica alrededor del mínimo.
        float refinedTau = ParabolicMinimum(yinCmnd, bestTau, maxTau);

        return refinedTau > 0f ? rate / refinedTau : -1f;
    }

    float ParabolicMinimum(float[] arr, int tau, int maxTau)
    {
        if (tau <= 0 || tau >= maxTau)
            return tau;

        float s0 = arr[tau - 1];
        float s1 = arr[tau];
        float s2 = arr[tau + 1];

        float denom = s0 + s2 - 2f * s1;

        if (Mathf.Approximately(denom, 0f))
            return tau;

        float offset = 0.5f * (s0 - s2) / denom;

        if (offset < -1f || offset > 1f)
            return tau;

        return tau + offset;
    }

    float Median(List<float> values)
    {
        if (values.Count == 0) return 0f;

        var sorted = new List<float>(values);
        sorted.Sort();
        int mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2f
            : sorted[mid];
    }

    void DisplayPitch(float frequency)
    {
        int midi = PitchEvaluator.FreqToMidi(frequency);
        float noteFreq = PitchEvaluator.MidiToFreq(midi);
        float cents = 1200f * Mathf.Log(frequency / noteFreq, 2f);

        if (noteText) noteText.text = PitchEvaluator.MidiToName(midi);
        if (freqText) freqText.text = $"{frequency:F2} Hz";
        if (centsText) centsText.text = $"{cents:+0;-0} cents";
    }

    private void OnDisable() => StopMicrophone();
    private void OnApplicationQuit() => StopMicrophone();

    void StopMicrophone()
    {
        if (!string.IsNullOrEmpty(micDevice))
            Microphone.End(micDevice);
    }
}

// Biquad IIR (RBJ cookbook), Direct Form II transposed. Estado continuo.
public struct Biquad
{
    private float b0, b1, b2, a1, a2;
    private float z1, z2;

    public static Biquad LowPass(float freq, float sampleRate, float q)
    {
        float w0 = 2f * Mathf.PI * freq / sampleRate;
        float cos = Mathf.Cos(w0);
        float alpha = Mathf.Sin(w0) / (2f * q);

        float a0 = 1f + alpha;
        var bq = new Biquad
        {
            b0 = ((1f - cos) / 2f) / a0,
            b1 = (1f - cos) / a0,
            b2 = ((1f - cos) / 2f) / a0,
            a1 = (-2f * cos) / a0,
            a2 = (1f - alpha) / a0
        };
        return bq;
    }

    public static Biquad HighPass(float freq, float sampleRate, float q)
    {
        float w0 = 2f * Mathf.PI * freq / sampleRate;
        float cos = Mathf.Cos(w0);
        float alpha = Mathf.Sin(w0) / (2f * q);

        float a0 = 1f + alpha;
        var bq = new Biquad
        {
            b0 = ((1f + cos) / 2f) / a0,
            b1 = (-(1f + cos)) / a0,
            b2 = ((1f + cos) / 2f) / a0,
            a1 = (-2f * cos) / a0,
            a2 = (1f - alpha) / a0
        };
        return bq;
    }

    public void Reset()
    {
        z1 = 0f;
        z2 = 0f;
    }

    public float Process(float x)
    {
        float y = b0 * x + z1;
        z1 = b1 * x - a1 * y + z2;
        z2 = b2 * x - a2 * y;
        return y;
    }
}