using UnityEngine;

public class PitchEvaluator : MonoBehaviour
{
    public static PitchEvaluator Instance;

    // Fuente única de verdad para los nombres de nota.
    public static readonly string[] NoteNames =
    {
        "C", "C#", "D", "D#", "E", "F",
        "F#", "G", "G#", "A", "A#", "B"
    };

    private void Awake()
    {
        Instance = this;
    }

    // ---- Núcleo estático (reutilizable sin instancia) ----

    public static int FreqToMidi(float freq)
    {
        return Mathf.RoundToInt(
            69f + 12f * Mathf.Log(freq / 440f, 2f));
    }

    public static float MidiToFreq(int midi)
    {
        return 440f * Mathf.Pow(2f, (midi - 69f) / 12f);
    }

    public static int NoteNameToMidi(NoteName note)
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

    public static string MidiToName(int midi)
    {
        if (midi < 0 || midi > 127)
            return "INVALID";

        int noteIndex = midi % 12;
        int octave = (midi / 12) - 1;

        return NoteNames[noteIndex] + octave;
    }

    // ---- Wrappers de instancia (compatibilidad con llamadas existentes) ----

    public int FrequencyToMidi(float freq) => FreqToMidi(freq);
    public float MidiToFrequency(int midi) => MidiToFreq(midi);
    public int NoteToMidi(NoteName note) => NoteNameToMidi(note);
}
