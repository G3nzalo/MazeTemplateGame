using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance;

    [Header("References")]
    public LevelData levelData;

    public BirdControllerMov bird;

    public VocalPitchDetector pitchDetector;

    public PitchEvaluator pitchEvaluator;

    public ScoreSystemGameAudio scoreSystem;

    public UIInteractionManager uiManager;

    public TuningFeedbackUI feedbackUI;

    [Header("Reference Audio (ayuda al jugador)")]
    [Tooltip("Audio de referencia que suena al LLEGAR a la nota, antes de evaluar " +
             "(p.ej. 'Nota Do'). Asigná el AudioSource del botón correspondiente, " +
             "ej. el de btnNotaDo para C3/C4. Las notas sin entrada no reproducen nada.")]
    public List<NoteReferenceAudio> referenceAudios = new List<NoteReferenceAudio>();

    [Tooltip("Activa/desactiva la reproducción del audio de referencia.")]
    public bool playReferenceBeforeNote = true;

    [Header("Voice / Octave")]
    [Tooltip("Desplazamiento de octava aplicado a TODO el nivel y al audio de referencia. " +
             "0 = voz grave (C3-C4, típico masculino). +1 = voz aguda (C4-C5, típico femenino). " +
             "Lo fija el jugador antes de empezar mediante OctaveSelectorButton.")]
    [Range(-2, 3)]
    public int octaveOffset = 0;

    public int OctaveOffset => octaveOffset;
    public bool LevelRunning => levelRunning;

    private const string OctavePrefKey = "SounApp.OctaveOffset";

    [Header("Evaluation")]
    [Tooltip("Tiempo inicial de la nota que se ignora (ataque vocal).")]
    public float attackIgnoreTime = 0.15f;

    [Header("State")]
    public GameAudioState currentState;

    private bool levelRunning;

    private bool currentEvaluationResult;

    private string lastDetectedNote = "NONE";

    private void Awake()
    {
        Instance = this;

        // Recupera la octava elegida por el jugador en sesiones anteriores.
        octaveOffset = PlayerPrefs.GetInt(OctavePrefKey, octaveOffset);
    }

    // Fija la octava del jugador (voz grave/aguda). Se ignora durante el entrenamiento.
    public void SetOctaveOffset(int octaves)
    {
        if (levelRunning)
            return;

        octaveOffset = Mathf.Clamp(octaves, -2, 3);

        PlayerPrefs.SetInt(OctavePrefKey, octaveOffset);
        PlayerPrefs.Save();
    }

    public void StartTraining()
    {
        if (levelRunning)
            return;

        StartCoroutine(LevelRoutine());
    }

    IEnumerator LevelRoutine()
    {
        levelRunning = true;
        DebugAudio.Instance.Clear();

        currentState =
            GameAudioState.Training;

        uiManager.LockAll();

        scoreSystem.totalNotes = 0;
        scoreSystem.correctNotes = 0;

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < levelData.notes.Count; i++)
        {
            NoteEvent note =
                levelData.notes[i];

            if (note.isRest)
            {
                yield return new WaitForSeconds(
                    GetDuration(note.duration));

                continue;
            }

            int visualIndex =
                GetVisualIndex(note);

            float duration =
                GetDuration(note.duration);

            yield return StartCoroutine(
                bird.FlyTo(
                    visualIndex,
                    duration));

            // Audio de referencia (p.ej. "Nota Do") al LLEGAR a la nota, antes
            // de evaluar. Suena con la captura aún apagada para no contaminar el mic.
            yield return StartCoroutine(
                PlayReferenceIfAny(note.note));

            yield return StartCoroutine(
                EvaluatePlayerPitch(
                    note.note,
                    duration));

            bool success =
                currentEvaluationResult;

            scoreSystem.Register(success);

            Debug.Log(
                $"TARGET: {note.note} | " +
                $"SUNG: {lastDetectedNote} | " +
                $"{(success ? "OK" : "FAIL")}");
        }

        EndLevel();
    }

    IEnumerator EvaluatePlayerPitch(NoteName targetNote, float duration)
    {
        // Frecuencia objetivo (se conoce de antemano por la nota),
        // transpuesta a la octava del jugador (voz grave/aguda).
        int targetMidi = pitchEvaluator.NoteToMidi(targetNote) + 12 * octaveOffset;
        float targetFreq = pitchEvaluator.MidiToFrequency(targetMidi);

        // Prepara el gráfico de afinación para esta nota.
        if (feedbackUI != null)
            feedbackUI.BeginNote(targetFreq, levelData.tuningToleranceCents);

        // Captura frames CRUDOS de F0 por hop (independiente del frame rate).
        pitchDetector.BeginCapture();

        // Ignora el ataque inicial (acotado a 1/3 de la nota para notas cortas).
        float warmup = Mathf.Min(attackIgnoreTime, duration * 0.3f);
        yield return new WaitForSeconds(warmup);
        pitchDetector.ClearCapture();

        // Evalúa la porción sostenida de la nota.
        yield return new WaitForSeconds(Mathf.Max(0f, duration - warmup));

        List<float> detectedFrequencies = pitchDetector.EndCapture();

        // SIN DATOS
        if (detectedFrequencies.Count == 0)
        {
            currentEvaluationResult = false;
            lastDetectedNote = "NONE";
            if (feedbackUI != null)
                feedbackUI.ShowResult(0f, levelData.perNoteTuningPercent);
            yield break;
        }

        // Frecuencia central = mediana (atenúa picos de ataque / gallos).
        float centralFreq = Median(detectedFrequencies);

        int detectedMidi = pitchEvaluator.FrequencyToMidi(centralFreq);

        lastDetectedNote = MidiToNoteName(detectedMidi);

        // % DE FRAMES AFINADOS respecto a la nota OBJETIVO (cents, tolerancia ±N).
        int framesInTune = 0;
        foreach (float f in detectedFrequencies)
        {
            float cents = 1200f * Mathf.Log(f / targetFreq, 2f);
            if (Mathf.Abs(cents) <= levelData.tuningToleranceCents)
                framesInTune++;
        }

        float tuningPercent =
            (float)framesInTune / detectedFrequencies.Count * 100f;

        // Afinada si >= % de frames dentro de tolerancia (spec: 70%).
        currentEvaluationResult = tuningPercent >= levelData.perNoteTuningPercent;

        // Barra final verde/roja con el % de afinación de la nota.
        if (feedbackUI != null)
            feedbackUI.ShowResult(tuningPercent, levelData.perNoteTuningPercent);

        string result = currentEvaluationResult ? "OK" : "FAIL";

        // DEBUG UI PANEL
        DebugAudio.Instance.AddLine(
            $"{targetNote} → {lastDetectedNote} | " +
            $"{tuningPercent:F1}% | {result}"
        );

        // DEBUG CONSOLE
        Debug.Log(
            $"TARGET: {targetFreq:F2} Hz | CENTRAL: {centralFreq:F2} Hz | " +
            $"TUNED: {tuningPercent:F1}% ({framesInTune}/{detectedFrequencies.Count}) | {result}");
    }

    float Median(List<float> values)
    {
        var sorted = new List<float>(values);
        sorted.Sort();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2f
            : sorted[mid];
    }

    string MidiToNoteName(int midi)
    {
        return PitchEvaluator.MidiToName(midi);
    }

    void EndLevel()
    {
        currentState =
            GameAudioState.Result;

        float accuracy =
            scoreSystem.AccuracyPercent();

        bool passed =
            scoreSystem.Passed(
                levelData.passPercentage);

        DebugAudio.Instance.AddLine("");
        DebugAudio.Instance.AddLine("===== RESULT =====");

        DebugAudio.Instance.AddLine(
        $"Accuracy: {accuracy:F1}%");

        DebugAudio.Instance.AddLine(
        passed ? "LEVEL PASSED" : "LEVEL FAILED");

        uiManager.UnlockAll();

        levelRunning = false;
    }

    float GetDuration(NoteLength length)
    {
        float quarter =
            60f / levelData.bpm;

        switch (length)
        {
            case NoteLength.Whole:
                return quarter * 4f;

            case NoteLength.Half:
                return quarter * 2f;

            case NoteLength.Eighth:
                return quarter * 0.5f;

            default:
                return quarter;
        }
    }

    int GetVisualIndex(NoteEvent note)
    {
        switch (note.note)
        {
            case NoteName.C3: return 0;
            case NoteName.D3: return 1;
            case NoteName.E3: return 2;
            case NoteName.F3: return 3;
            case NoteName.G3: return 4;
            case NoteName.A3: return 5;
            case NoteName.B3: return 6;
            case NoteName.C4: return 7;
        }

        return 0;
    }

    // Reproduce el audio de referencia de la nota (si está asignado) y espera a
    // que termine. Reutiliza el AudioSource del botón correspondiente (ej. btnNotaDo).
    IEnumerator PlayReferenceIfAny(NoteName note)
    {
        if (!playReferenceBeforeNote)
            yield break;

        AudioSource source = GetReferenceSource(note);
        if (source == null)
            yield break;

        // Transpone la referencia a la octava del jugador (pitch=2 → +1 octava)
        // y restaura el pitch original al terminar para no afectar al botón.
        float originalPitch = source.pitch;
        source.pitch = Mathf.Pow(2f, octaveOffset);

        source.Play();
        yield return new WaitWhile(() => source.isPlaying);

        source.pitch = originalPitch;
    }

    AudioSource GetReferenceSource(NoteName note)
    {
        for (int i = 0; i < referenceAudios.Count; i++)
        {
            if (referenceAudios[i] != null &&
                referenceAudios[i].note == note &&
                referenceAudios[i].source != null)
            {
                return referenceAudios[i].source;
            }
        }

        return null;
    }
}

// Mapea una nota a su AudioSource de referencia (p.ej. el de btnNotaDo).
[System.Serializable]
public class NoteReferenceAudio
{
    public NoteName note;
    public AudioSource source;
}