using UnityEngine;

public class PitchEvaluator : MonoBehaviour
{
    public static PitchEvaluator Instance;

    private void Awake()
    {
        Instance = this;
    }

    public int FrequencyToMidi(float freq)
    {
        return Mathf.RoundToInt(
            69f + 12f *
            Mathf.Log(freq / 440f, 2f));
    }

    public int NoteToMidi(NoteName note)
    {
        switch (note)
        {
            case NoteName.C3: return 48;
            case NoteName.D3: return 50;
            case NoteName.E3: return 52;
            case NoteName.F3: return 53;
            case NoteName.G3: return 55;
            case NoteName.A3: return 57;
            case NoteName.B3: return 59;
            case NoteName.C4: return 60;
        }

        return 48;
    }

    public bool IsIntervalCorrect(
        int playerBaseMidi,
        float detectedFreq,
        NoteName targetNote,
        NoteName referenceNote,
        float toleranceSemitones = 1f)
    {
        int detectedMidi =
            FrequencyToMidi(detectedFreq);

        int targetMidi =
            NoteToMidi(targetNote);

        int referenceMidi =
            NoteToMidi(referenceNote);

        int expectedInterval =
            targetMidi - referenceMidi;

        int playerInterval =
            detectedMidi - playerBaseMidi;

        float difference =
            Mathf.Abs(
                playerInterval -
                expectedInterval);

        return difference <= toleranceSemitones;
    }
}