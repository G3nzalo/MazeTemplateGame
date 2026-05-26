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

    [Header("State")]
    public GameAudioState currentState;

    private bool levelRunning;

    private bool currentEvaluationResult;

    private string lastDetectedNote = "NONE";

    private void Awake()
    {
        Instance = this;
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

    IEnumerator EvaluatePlayerPitch(
    NoteName targetNote,
    float duration)
    {
        float timer = 0f;

        float warmupTime = 0.15f;

        List<float> detectedFrequencies =
            new List<float>();

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (pitchDetector.HasValidPitch)
            {
                float freq =
                    pitchDetector.CurrentPitch;

                if (freq > 0f)
                {
                    // IGNORAR ATAQUE INICIAL
                    if (timer > warmupTime)
                    {
                        detectedFrequencies.Add(freq);
                    }
                }
            }

            yield return null;
        }

        // SIN DATOS
        if (detectedFrequencies.Count == 0)
        {
            currentEvaluationResult = false;
            lastDetectedNote = "NONE";
            yield break;
        }

        // PROMEDIO
        float averageFreq =
            detectedFrequencies.Average();

        int detectedMidi =
            pitchEvaluator
            .FrequencyToMidi(
                averageFreq);

        int targetMidi =
            pitchEvaluator
            .NoteToMidi(
                targetNote);

        lastDetectedNote =
            MidiToNoteName(
                detectedMidi);

        // MISMA NOTA
        bool sameNote =
            detectedMidi ==
            targetMidi;

        // ERROR %
        float targetFreq =
            pitchEvaluator
            .MidiToFrequency(
                targetMidi);

        float percentError =
            Mathf.Abs(
                averageFreq -
                targetFreq)
            / targetFreq;

        float tolerance =
            levelData.pitchTolerancePercent
            / 100f;

        bool inTune =
            percentError <= tolerance;

        currentEvaluationResult =
            sameNote && inTune;

        Debug.Log(
            $"TARGET FREQ: {targetFreq:F2} Hz | " +
            $"AVG FREQ: {averageFreq:F2} Hz");

        Debug.Log(
            $"ERROR: {(percentError * 100f):F2}%");

        Debug.Log(
            $"FINAL NOTE: {lastDetectedNote}");
    }

    string MidiToNoteName(int midi)
    {
        string[] names =
        {
            "C",
            "C#",
            "D",
            "D#",
            "E",
            "F",
            "F#",
            "G",
            "G#",
            "A",
            "A#",
            "B"
        };

        if (midi < 0 || midi > 127)
            return "INVALID";

        int noteIndex =
            Mathf.Abs(midi % 12);

        int octave =
            (midi / 12) - 1;

        return names[noteIndex] +
               octave;
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

        Debug.Log(
            $"Accuracy: {accuracy}");

        Debug.Log(
            passed
            ? "LEVEL PASSED"
            : "LEVEL FAILED");

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
}