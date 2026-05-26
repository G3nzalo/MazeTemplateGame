using System.Collections;
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

    private int playerBaseMidi;

    private bool basePitchCaptured;

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

        basePitchCaptured = false;

        yield return new WaitForSeconds(1f);

        NoteName referenceNote =
            levelData.notes[0].note;

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

            float noteDuration =
                GetDuration(note.duration);

            yield return StartCoroutine(
                EvaluatePlayerPitch(
                    note.note,
                    referenceNote,
                    noteDuration));

            bool success =
                currentEvaluationResult;

            scoreSystem.Register(success);

            Debug.Log(
                $"NOTE {i} -> " +
                $"{(success ? "OK" : "FAIL")}");
        }

        EndLevel();
    }

    IEnumerator EvaluatePlayerPitch(
        NoteName targetNote,
        NoteName referenceNote,
        float duration)
    {
        float timer = 0f;

        float correctTime = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (pitchDetector.HasValidPitch)
            {
                if (!basePitchCaptured)
                {
                    playerBaseMidi =
                        pitchEvaluator
                        .FrequencyToMidi(
                            pitchDetector.CurrentPitch);

                    basePitchCaptured = true;
                }

                bool correct =
                    pitchEvaluator
                    .IsIntervalCorrect(
                        playerBaseMidi,
                        pitchDetector.CurrentPitch,
                        targetNote,
                        referenceNote);

                if (correct)
                {
                    correctTime +=
                        Time.deltaTime;
                }
            }

            yield return null;
        }

        float required =
            duration * 0.85f;

        currentEvaluationResult =
            correctTime >= required;
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