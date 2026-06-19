using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sistema de instrucciones con efecto máquina de escribir.
/// Carga slides desde un JSON en StreamingAssets o Resources.
///
/// SETUP EN INSPECTOR:
///   - titleText      → TextMeshProUGUI del título (puede estar vacío en el JSON)
///   - bodyText       → TextMeshProUGUI del cuerpo principal
///   - prevButton     → Button "Anterior"
///   - nextButton     → Button "Siguiente"
///   - backgroundImage→ Image compartida (1 sola imagen reutilizada)
///   - progressText   → TextMeshProUGUI "1 / 8" (opcional)
///   - panelRoot      → GameObject raíz del panel (para Show/Hide)
///
/// REGLA DE SALTOS DE LÍNEA EN EL JSON:
///   Usá \n dentro del string. Ejemplo:
///   "text": "Línea 1\n\nLínea 2"
/// </summary>
public class TypewriterInstructions : MonoBehaviour
{
    // ─── Referencias UI ─────────────────────────────────────────────────────
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button          prevButton;
    [SerializeField] private Button          nextButton;
    [SerializeField] private Image           backgroundImage;   // 1 sola imagen
    [SerializeField] private TextMeshProUGUI progressText;      // "1 / 8" opcional
    [SerializeField] private GameObject      panelRoot;

    // ─── Configuración ──────────────────────────────────────────────────────

    [Header("Configuración")]
    [Tooltip("Nombre del JSON sin extensión (en StreamingAssets/ o Resources/)")]
    [SerializeField] private string jsonFileName = "instructions_data";

    [Tooltip("Si es true carga desde Resources/, si es false desde StreamingAssets/")]
    [SerializeField] private bool loadFromResources = false;

    [Tooltip("Texto del botón en el último slide (puede cambiarse a 'Comenzar', 'Listo', etc.)")]
    [SerializeField] private string lastSlideNextLabel = "Comenzar";

    [Header("Audio (opcional)")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip   typingClip;
    [SerializeField] [Range(0f, 1f)] private float typingVolume = 0.4f;

    // ─── Estado interno ─────────────────────────────────────────────────────

    private List<SlideData> slides          = new List<SlideData>();
    private int             currentIndex    = 0;
    private Coroutine       typingCoroutine = null;
    private bool            isTyping        = false;

    // Caracteres que generan pausa extra
    private static readonly HashSet<char> PauseChars = new HashSet<char> { '.', ',', '!', '?', ':', ';' };

    // Label original del botón siguiente (se restaura en slides normales)
    private string defaultNextLabel = "Siguiente";

    // ════════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Guardar label original del botón siguiente
        if (nextButton != null)
        {
            var label = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) defaultNextLabel = label.text;
        }

        nextButton?.onClick.AddListener(OnNextClicked);
        prevButton?.onClick.AddListener(OnPrevClicked);

        SetPrevButtonVisible(false);
    }

    private void Start()
    {
        LoadJSON();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Carga del JSON
    // ════════════════════════════════════════════════════════════════════════

    private void LoadJSON()
    {
        string json = string.Empty;

        if (loadFromResources)
        {
            TextAsset asset = Resources.Load<TextAsset>(jsonFileName);
            if (asset == null)
            {
                Debug.LogError($"[Typewriter] No se encontró '{jsonFileName}' en Resources/");
                return;
            }
            json = asset.text;
        }
        else
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, jsonFileName + ".json");

            // En Android y WebGL, StreamingAssets vive dentro del APK/servidor comprimido
            // y NO se puede leer con System.IO. Hay que usar UnityWebRequest.
#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(LoadJSONFromUWR(path));
            return;
#elif UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(LoadJSONFromUWR(path));
            return;
#else
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError($"[Typewriter] Archivo no encontrado: {path}");
                return;
            }
            json = System.IO.File.ReadAllText(path);
#endif
        }

        ParseAndStart(json);
    }

    private void ParseAndStart(string json)
    {
        SlidesRoot root = JsonUtility.FromJson<SlidesRoot>(json);

        if (root == null || root.slides == null || root.slides.Count == 0)
        {
            Debug.LogError("[Typewriter] JSON vacío o mal formateado.");
            return;
        }

        slides       = root.slides;
        currentIndex = 0;

        ShowPanel(true);
        ShowSlide(currentIndex);
    }

    // Carga vía UnityWebRequest para plataformas donde StreamingAssets no es accesible
    // por System.IO (Android: dentro del APK; WebGL: servido por HTTP).
    private IEnumerator LoadJSONFromUWR(string url)
    {
        using (var req = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Typewriter] Error cargando JSON desde '{url}': {req.error}");
                yield break;
            }
            ParseAndStart(req.downloadHandler.text);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // Lógica de slides
    // ════════════════════════════════════════════════════════════════════════

    private void ShowSlide(int index)
    {
        if (index < 0 || index >= slides.Count) return;

        SlideData slide = slides[index];

        // Título (puede ser vacío)
        if (titleText != null)
        {
            titleText.text = slide.title;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(slide.title));
        }

        // Progreso
        UpdateProgress();

        // Actualizar visibilidad y labels de botones
        RefreshButtons();

        // Limpiar body y arrancar efecto
        if (bodyText != null)
            bodyText.text = string.Empty;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        CompleteCurrentText();
        // typingCoroutine = StartCoroutine(TypeText(slide));
    }

    private IEnumerator TypeText(SlideData slide)
    {
        isTyping = true;
        bodyText.text = string.Empty;

        // \n en el JSON ya viene como salto de línea real al ser parseado.
        // TMP los renderiza directamente.
        string fullText = slide.text;

        for (int i = 0; i < fullText.Length; i++)
        {
            char c = fullText[i];
            bodyText.text += c;

            // Los saltos de línea no producen sonido ni pausa de puntuación
            if (c == '\n')
            {
                yield return new WaitForSeconds(slide.typingSpeed * 0.5f);
                continue;
            }

            // PlayTypingSound();

            float delay = slide.typingSpeed;

            // Pausa extra tras puntuación seguida de espacio o salto
            if (PauseChars.Contains(c) && i + 1 < fullText.Length)
            {
                char next = fullText[i + 1];
                if (next == ' ' || next == '\n')
                    delay += slide.pauseAfterPunctuation;
            }

            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        // Actualizar botones al terminar (por si el estado "escribiendo" ocultó algo)
        RefreshButtons();
    }

    // ─── Botones ─────────────────────────────────────────────────────────────

    /// <summary>Avanzar al siguiente slide (o completar el texto si aún escribe).</summary>
    public void OnNextClicked()
    {
        if (isTyping)
        {
            // Primer toque: completa el texto al instante
            CompleteCurrentText();
            return;
        }

        currentIndex++;

        if (currentIndex >= slides.Count)
        {
            FinishInstructions();
            return;
        }

        ShowSlide(currentIndex);
    }

    /// <summary>Retroceder al slide anterior.</summary>
    public void OnPrevClicked()
    {
        if (isTyping)
        {
            // Si está escribiendo, cancelar y mostrar texto completo del slide actual
            CompleteCurrentText();
            return;
        }

        if (currentIndex <= 0) return;

        currentIndex--;
        ShowSlide(currentIndex);
    }

    /// <summary>Muestra el texto completo del slide actual sin animación.</summary>
    private void CompleteCurrentText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (bodyText != null && currentIndex < slides.Count)
            bodyText.text = slides[currentIndex].text;

        isTyping = false;
        RefreshButtons();
    }

    private void FinishInstructions()
    {
        Debug.Log("[Typewriter] Instrucciones completadas.");
        ShowPanel(false);

        // Al terminar los tutoriales del inicio, encadenamos el popup informativo
        // del primer nivel (Nivel 1). GameAudioManager sabe cuál es el nivel
        // vigente y qué levelID le corresponde.
        if (GameAudioManager.Instance != null)
            GameAudioManager.Instance.ShowCurrentLevelInfo();
    }

    // ─── Estado de botones ────────────────────────────────────────────────────

    /// <summary>Actualiza visibilidad y labels de Anterior / Siguiente según el slide actual.</summary>
    private void RefreshButtons()
    {
        bool isFirst = currentIndex == 0;
        bool isLast  = currentIndex == slides.Count - 1;

        // Anterior: oculto en el primer slide
        SetPrevButtonVisible(!isFirst);

        // Siguiente: siempre visible; cambia label en el último slide
        if (nextButton != null)
        {
            string label = isLast ? lastSlideNextLabel : defaultNextLabel;
            var tmp = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = label;
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SetPrevButtonVisible(bool visible)
    {
        if (prevButton != null)
            prevButton.gameObject.SetActive(visible);
    }

    private void ShowPanel(bool show)
    {
        if (panelRoot != null)
            panelRoot.SetActive(show);
    }

    private void UpdateProgress()
    {
        if (progressText != null)
            progressText.text = $"{currentIndex + 1} / {slides.Count}";
    }

    private void PlayTypingSound()
    {
        if (typingAudioSource != null && typingClip != null)
            typingAudioSource.PlayOneShot(typingClip, typingVolume);
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>Reinicia las instrucciones desde el primer slide.</summary>
    public void RestartInstructions()
    {
        currentIndex = 0;
        ShowPanel(true);
        ShowSlide(currentIndex);
    }

    /// <summary>Salta directamente a un slide por índice (0-based).</summary>
    public void GoToSlide(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, slides.Count - 1);
        ShowSlide(currentIndex);
    }
}
