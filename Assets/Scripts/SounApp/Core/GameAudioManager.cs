using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance;

    [Header("References")]
    [Tooltip("Lista ordenada de niveles. El botón 'Siguiente Nivel' avanza por aquí.")]
    public List<LevelData> levels = new List<LevelData>();

    [Tooltip("Índice del nivel con el que se arranca dentro de 'levels'.")]
    public int currentLevelIndex = 0;

    // Nivel actualmente en juego. Se deriva de levels[currentLevelIndex];
    // no se asigna en el Inspector.
    private LevelData levelData;

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

    [Header("Tempo")]
    [Tooltip("Límite inferior de BPM que el jugador puede fijar con los botones +/-.")]
    [Range(20, 240)]
    public int minBpm = 40;

    [Tooltip("Límite superior de BPM que el jugador puede fijar con los botones +/-.")]
    [Range(20, 240)]
    public int maxBpm = 200;

    [Tooltip("Cuánto sube/baja el BPM por cada pulsación del botón +/-.")]
    public int bpmStep = 5;

    [Tooltip("Duración (segundos) de la transición de vuelo entre notas. Es una " +
             "transición CORTA: se acota a una fracción de la negra para no romper " +
             "el tempo en notas rápidas. El vuelo + el canto juntos duran una negra, " +
             "así el onset de cada nota cae justo en el pulso elegido.")]
    public float noteTransitionTime = 0.15f;

    // Tempo VIGENTE del juego, en memoria. Es la única fuente de verdad tanto
    // para el display como para la duración de cada nota. Se inicializa con el
    // BPM del nivel y se modifica con los botones +/-. NO se persiste (no se
    // guarda en PlayerPrefs) ni se escribe sobre el asset del nivel.
    private int currentBpm;

    public int Bpm => currentBpm;

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

        // Si hay lista de niveles, el nivel actual sale de ahí.
        ApplyCurrentLevel();

        // Recupera la octava elegida por el jugador en sesiones anteriores.
        octaveOffset = PlayerPrefs.GetInt(OctavePrefKey, octaveOffset);

        // Tempo inicial = el del nivel de arranque. NO se lee de PlayerPrefs:
        // el jugador lo ajusta en cada sesión y no queda persistido.
        if (levelData != null)
            currentBpm = Mathf.Clamp(levelData.bpm, minBpm, maxBpm);
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

    // Fija el tempo (BPM) vigente del juego. Se ignora durante un entrenamiento
    // EN CURSO, igual que la octava. NO se persiste: vale solo para esta sesión.
    public void SetBpm(int bpm)
    {
        if (levelRunning)
            return;

        currentBpm = Mathf.Clamp(bpm, minBpm, maxBpm);
    }

    // Sube/baja el BPM en 'delta' (positivo o negativo). Para los botones +/-.
    public void ChangeBpm(int delta)
    {
        SetBpm(Bpm + delta);
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

        if (DebugAudio.Instance == null)
        {
            Debug.LogError(
                "DebugAudio.Instance es null. El componente DebugAudio debe estar " +
                "en un GameObject SIEMPRE activo (no en el PanelPopUpResult apagado). " +
                "Dejá el panel activo en escena y asignalo en el campo 'panelPopUpResult'.");
        }
        else
        {
            DebugAudio.Instance.Hide();
            DebugAudio.Instance.Clear();
        }

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

            // Reparto del pulso: el vuelo es una transición CORTA y el canto
            // ocupa el resto. vuelo + canto = la negra, así el onset de cada
            // nota cae justo en el beat y el nivel va al tempo elegido.
            float flightTime =
                Mathf.Min(noteTransitionTime, duration * 0.35f);
            float singTime = duration - flightTime;

            // Vuela rápido a la nota mientras suena la referencia con fade out
            // (no agrega tiempo al pulso). La captura del mic sigue apagada.
            yield return StartCoroutine(
                FlyAndCue(visualIndex, flightTime, note.note));

            yield return StartCoroutine(
                EvaluatePlayerPitch(
                    note.note,
                    singTime));

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
        // if (feedbackUI != null)
        //     feedbackUI.BeginNote(targetFreq, levelData.tuningToleranceCents);

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

            // No se captó voz para esta nota = error: se lista en el scroll.
            DebugAudio.Instance.AddError($"{targetNote} → no llegaste a esa nota.");
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

        // SCROLL DE ERRORES: solo las notas falladas.
        if (!currentEvaluationResult)
            DebugAudio.Instance.AddError(
                $"{targetNote} → cantaste {lastDetectedNote} ({tuningPercent:F0}%)");

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

        bool hasNextLevel = HasNextLevel();

        // Enciende el popup de resultado con título, % de aciertos y errores.
        DebugAudio.Instance.ShowResult(passed, accuracy, hasNextLevel);

        // El botón pasa al siguiente nivel (si pasó y existe) o reintenta.
        DebugAudio.Instance.nextBtn.onClick.RemoveAllListeners();

        if (passed && hasNextLevel)
            DebugAudio.Instance.nextBtn.onClick.AddListener(LoadNextLevel);
        else
            DebugAudio.Instance.nextBtn.onClick.AddListener(RetryLevel);

        uiManager.UnlockAll();

        levelRunning = false;
    }

    // ¿Existe un nivel posterior al actual en la lista?
    bool HasNextLevel()
    {
        return levels != null
            && currentLevelIndex < levels.Count - 1;
    }

    // Fija 'levelData' según el índice actual dentro de 'levels' (si hay lista).
    void ApplyCurrentLevel()
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogError(
                "GameAudioManager: la lista 'levels' está vacía. Asigná al menos " +
                "un LevelData en el Inspector.");
            return;
        }

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1);
        levelData = levels[currentLevelIndex];
    }

    // Botón "Siguiente Nivel": avanza al próximo nivel y arranca el entrenamiento.
    public void LoadNextLevel()
    {
        DebugAudio.Instance.Hide();

        if (HasNextLevel())
        {
            currentLevelIndex++;
            ApplyCurrentLevel();
        }

        StartTraining();
    }

    // Botón "Reintentar": vuelve a empezar el mismo nivel.
    public void RetryLevel()
    {
        DebugAudio.Instance.Hide();
        StartTraining();
    }

    float GetDuration(NoteLength length)
    {
        float quarter =
            60f / currentBpm;

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

    // Vuela rápido a la nota y, en paralelo, lanza el audio de referencia con un
    // fade out que termina junto con el vuelo: así la referencia no agrega tiempo
    // al pulso. La captura del mic sigue apagada durante esta fase.
    IEnumerator FlyAndCue(int visualIndex, float flightTime, NoteName note)
    {
        Coroutine cue = playReferenceBeforeNote
            ? StartCoroutine(PlayReferenceWithFade(note, flightTime))
            : null;

        yield return StartCoroutine(bird.FlyTo(visualIndex, flightTime));

        // Asegura que el fade terminó y el audio se detuvo antes de evaluar.
        if (cue != null)
            yield return cue;
    }

    // Reproduce la referencia (p.ej. "Nota Do") transpuesta a la octava del
    // jugador y la apaga con un fade out a lo largo de 'fadeTime', para que no
    // coma tiempo del compás ni de la negra. Restaura pitch/volumen del source.
    IEnumerator PlayReferenceWithFade(NoteName note, float fadeTime)
    {
        AudioSource source = GetReferenceSource(note);
        if (source == null)
            yield break;

        float originalPitch = source.pitch;
        float originalVolume = source.volume;

        source.pitch = Mathf.Pow(2f, octaveOffset);
        source.volume = originalVolume;
        source.Play();

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            source.volume =
                Mathf.Lerp(originalVolume, 0f, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }

        source.Stop();
        source.pitch = originalPitch;
        source.volume = originalVolume;
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