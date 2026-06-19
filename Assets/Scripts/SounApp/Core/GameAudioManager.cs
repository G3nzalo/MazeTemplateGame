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

    // levelID del nivel vigente. Hace match con el "id" de level_data.json para
    // elegir el texto del popup informativo (ver LevelInstructions.ShowForLevel).
    public int CurrentLevelID => levelData != null ? levelData.levelID : 0;

    [Header("Popup informativo de nivel")]
    [Tooltip("Popup que se muestra al inicio de cada nivel con la misión del mismo.")]
    public LevelInstructions levelInstructions;

    [Header("Tempo")]
    [Tooltip("Límite inferior de BPM que el jugador puede fijar con los botones +/-.")]
    [Range(20, 240)]
    public int minBpm = 40;

    [Tooltip("Límite superior de BPM que el jugador puede fijar con los botones +/-.")]
    [Range(20, 240)]
    public int maxBpm = 200;

    [Tooltip("Cuánto sube/baja el BPM por cada pulsación del botón +/-.")]
    public int bpmStep = 5;

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

    [Header("Debug")]
    [Tooltip("Modo prueba: permite SALTEAR el gameplay para testear los popups de " +
             "resultado e informativos end-to-end, sin tener que cantar.\n" +
             "Teclas en el editor (con debugMode activo y sin nivel en curso):\n" +
             "  P = completar el nivel como SUPERADO\n" +
             "  F = completar el nivel como NO superado")]
    public bool debugMode = false;

    private bool levelRunning;

    private bool currentEvaluationResult;

    private string lastDetectedNote = "NONE";

    private void Awake()
    {
        Instance = this;

        // Si hay lista de niveles, el nivel actual sale de ahí.
        ApplyCurrentLevel();

        // La octava NO se persiste: arranca con el valor por defecto del
        // Inspector en cada sesión. El jugador la cambia libremente antes de
        // entrenar (igual que el BPM), sin que quede guardada entre sesiones.

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

        // No se persiste: vale solo para esta sesión, como el BPM.
        octaveOffset = Mathf.Clamp(octaves, -2, 3);
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

    private void Update()
    {
        if (!debugMode || levelRunning)
            return;

        // Atajos para testear los popups sin jugar el nivel.
        if (Input.GetKeyDown(KeyCode.P))
            DebugFinishLevel(true);
        else if (Input.GetKeyDown(KeyCode.F))
            DebugFinishLevel(false);
    }

    // DEBUG: completa el nivel actual con un resultado forzado (superado o no),
    // sin jugarlo, y dispara el flujo normal de fin de nivel (popup de resultado
    // y, si corresponde, popup informativo del siguiente). Útil para recorrer
    // todos los popups end-to-end. También se puede enganchar a botones de debug.
    public void DebugPassLevel() => DebugFinishLevel(true);
    public void DebugFailLevel() => DebugFinishLevel(false);

    private void DebugFinishLevel(bool passed)
    {
        if (levelRunning || levelData == null)
            return;

        // Simula el puntaje para que EndLevel calcule el resultado deseado.
        int total = (levelData.notes != null && levelData.notes.Count > 0)
            ? levelData.notes.Count
            : 10;

        scoreSystem.totalNotes = total;
        scoreSystem.correctNotes = passed ? total : 0;

        if (DebugAudio.Instance != null)
        {
            DebugAudio.Instance.Clear();
            if (!passed)
                DebugAudio.Instance.AddError("[DEBUG] Nivel marcado como NO superado.");
        }

        Debug.Log($"[DEBUG] Completando nivel {CurrentLevelID} como {(passed ? "SUPERADO" : "NO superado")}.");

        EndLevel();
    }

    // Aborta el entrenamiento EN CURSO y deja el juego en un estado limpio,
    // listo para volver a entrenar. Lo llama el botón de pausa/stop.
    //
    // A diferencia de los setters de BPM/octava (que se ignoran con el nivel en
    // curso), este método SÍ actúa durante el nivel: su razón de ser es cortarlo
    // a mitad. Por eso NO debe depender de UIInteractionManager.CanInteract(),
    // que está bloqueado mientras se entrena.
    public void StopTraining()
    {
        if (!levelRunning)
            return;

        // Corta la secuencia completa: LevelRoutine, el planeo del pájaro
        // (bird.FlyTo) y la evaluación de la nota (EvaluatePlayerPitch). Todas se
        // lanzaron con StartCoroutine sobre este MonoBehaviour.
        StopAllCoroutines();

        // Cierra la captura del micrófono que pudiera haber quedado abierta.
        if (pitchDetector != null)
            pitchDetector.EndCapture();

        levelRunning = false;
        currentState = GameAudioState.Menu;

        // Devuelve el control de la UI: LockAll() la había bloqueado al empezar.
        if (uiManager != null)
            uiManager.UnlockAll();

        // Oculta el popup de resultado y limpia el scroll de errores.
        if (DebugAudio.Instance != null)
        {
            DebugAudio.Instance.Hide();
            DebugAudio.Instance.Clear();
        }
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

        // Cuenta de entrada: coloca al pájaro SOBRE la primera nota tocable antes
        // de que empiece la música (vuelo de un pulso, sin evaluar). A partir de
        // ahí, en cada figura el pájaro YA está sobre la nota que toca cantar.
        int firstIndex = NextPlayableIndex(0);
        if (firstIndex >= 0)
            yield return StartCoroutine(
                bird.FlyTo(
                    GetVisualIndex(levelData.notes[firstIndex]),
                    GetDuration(levelData.notes[firstIndex].duration)));

        for (int i = 0; i < levelData.notes.Count; i++)
        {
            NoteEvent note =
                levelData.notes[i];

            // Los silencios no se cantan: su tiempo ya lo consume el planeo de la
            // nota anterior, que cruza por encima de ellos hasta la próxima nota.
            if (note.isRest)
                continue;

            float duration =
                GetDuration(note.duration);

            // El pájaro YA está sobre esta nota (llegó en el planeo anterior o en
            // la cuenta de entrada). Disparamos su referencia justo en el pulso.
            if (playReferenceBeforeNote)
                PlayReference(note.note);

            // Próxima nota tocable y tiempo TOTAL de planeo hasta ella: la
            // duración de ESTA figura más la de los silencios intermedios, para
            // que el pájaro aterrice en la siguiente nota justo en su pulso.
            int nextIndex = NextPlayableIndex(i + 1);
            float glideTime = duration;
            int glideEnd = nextIndex < 0 ? levelData.notes.Count : nextIndex;
            for (int r = i + 1; r < glideEnd; r++)
                glideTime += GetDuration(levelData.notes[r].duration);

            // MOVIMIENTO MUSICAL: durante TODA la figura el pájaro planea hacia la
            // SIGUIENTE nota (no un salto brusco al final), en paralelo con la
            // evaluación de la nota ACTUAL. Así el jugador ve con anticipación a
            // dónde tendrá que cantar la próxima.
            Coroutine glide = nextIndex >= 0
                ? StartCoroutine(
                    bird.FlyTo(
                        GetVisualIndex(levelData.notes[nextIndex]),
                        glideTime))
                : null;

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

            // Si entre esta nota y la próxima había silencios, esperamos a que el
            // planeo termine de cruzarlos para no romper el compás.
            if (glide != null)
                yield return glide;
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
            // Etiqueta esperada desde targetMidi (incluye octaveOffset) para
            // mantener la misma referencia de octava que el resto de mensajes.
            DebugAudio.Instance.AddError($"{MidiToNoteName(targetMidi)} → no llegaste a esa nota.");
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
        // La nota esperada se etiqueta desde targetMidi (que ya incluye
        // octaveOffset), igual que lastDetectedNote, para que ambas usen la
        // MISMA referencia de octava. Si se imprime el enum crudo ({targetNote})
        // la etiqueta esperada ignora octaveOffset y queda desfasada una octava
        // respecto a la cantada (p. ej. muestra "C4 → cantaste C3").
        if (!currentEvaluationResult)
            DebugAudio.Instance.AddError(
                $"{MidiToNoteName(targetMidi)} → cantaste {lastDetectedNote} ({tuningPercent:F0}%)");

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

        // Dejamos 'currentLevelIndex' apuntando al nivel que toca la próxima vez:
        // si superó y hay siguiente, avanza; si no, queda en el mismo (reintento).
        // Así da igual cómo se vuelva a entrenar —con el botón del panel o
        // cerrando el panel y pulsando "Entrenar"—: siempre carga el nivel
        // correcto según el resultado.
        if (passed && hasNextLevel)
        {
            currentLevelIndex++;
            ApplyCurrentLevel();

            // SUPERÓ el nivel: al cerrar el popup de resultado (botón Siguiente o
            // cerrar) NO entrenamos directo, sino que mostramos el popup
            // informativo del NUEVO nivel. El jugador lo lee, lo cierra y arranca
            // cuando pulsa "Entrenar".
            DebugAudio.Instance.nextBtn.onClick.RemoveAllListeners();
            DebugAudio.Instance.nextBtn.onClick.AddListener(ShowNextLevelInfo);

            DebugAudio.Instance.closeBtn.onClick.RemoveAllListeners();
            DebugAudio.Instance.closeBtn.onClick.AddListener(ShowNextLevelInfo);
        }
        else
        {
            // NO superó (o no hay siguiente nivel): se reintenta el MISMO nivel
            // como antes, sin mostrar el popup informativo de nuevo.
            DebugAudio.Instance.nextBtn.onClick.RemoveAllListeners();
            DebugAudio.Instance.nextBtn.onClick.AddListener(ResumeTraining);

            // Restaura el cierre normal del popup (por si quedó enganchado a
            // ShowNextLevelInfo de un nivel superado anterior).
            DebugAudio.Instance.closeBtn.onClick.RemoveAllListeners();
            DebugAudio.Instance.closeBtn.onClick.AddListener(DebugAudio.Instance.Hide);
        }

        // NO se desbloquea la UI acá: el popup de resultado queda abierto y sus
        // botones de atrás deben seguir inactivos. El control se devuelve al
        // cerrar el popup (DebugAudio.Hide) o al arrancar el próximo flujo
        // (ResumeTraining → StartTraining, o el popup informativo del nuevo nivel).
        levelRunning = false;
    }

    // Muestra el popup informativo del nivel vigente (su misión), eligiendo el
    // texto por el levelID en level_data.json. Lo usan los tutoriales (Nivel 1)
    // y el cierre del popup de resultado tras superar un nivel (Niveles 2..8).
    public void ShowCurrentLevelInfo()
    {
        LevelInstructions panel = levelInstructions != null
            ? levelInstructions
            : LevelInstructions.Instance;

        if (panel != null)
            panel.ShowForLevel(CurrentLevelID);
    }

    // Cierra el popup de resultado y abre el informativo del nuevo nivel.
    void ShowNextLevelInfo()
    {
        DebugAudio.Instance.Hide();
        ShowCurrentLevelInfo();
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

    // Botón del panel de resultado ("Siguiente Nivel" / "Reintentar"): cierra el
    // popup y arranca el entrenamiento del nivel vigente. EndLevel ya dejó
    // 'currentLevelIndex' en el nivel correcto, así que no hay que decidir nada
    // acá. Es exactamente lo mismo que cerrar el panel y pulsar "Entrenar".
    public void ResumeTraining()
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

    // Índice de la primera nota TOCABLE (no silencio) desde 'from' inclusive.
    // Devuelve -1 si de ahí en adelante solo quedan silencios. Se usa para saber
    // hacia qué nota debe planear el pájaro (saltándose los silencios del medio).
    int NextPlayableIndex(int from)
    {
        for (int i = from; i < levelData.notes.Count; i++)
            if (!levelData.notes[i].isRest)
                return i;

        return -1;
    }

    // Reproduce la referencia de la nota (p.ej. "Nota Do") completa y al volumen
    // configurado en su AudioSource. No corta ni desvanece el sonido y no bloquea
    // la secuencia: la nota empieza a evaluarse en el mismo pulso.
    void PlayReference(NoteName note)
    {
        AudioSource source = GetReferenceSource(note);
        if (source == null)
            return;

        source.Play();
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