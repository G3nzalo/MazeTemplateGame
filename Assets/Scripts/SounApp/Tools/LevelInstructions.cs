using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup informativo que se muestra al INICIO de cada nivel.
///
/// El texto se carga desde level_data.json (StreamingAssets/ o Resources/) y se
/// elige el slide cuyo "id" coincide con el levelID del LevelData del nivel
/// vigente (ver <see cref="LevelData.levelID"/>).
///
/// IMPORTANTE: el panel NO se enciende solo al darle Play. Arranca apagado y solo
/// se muestra cuando alguien llama a <see cref="ShowForLevel(int)"/>:
///   - Nivel 1: al terminar los tutoriales (TypewriterInstructions.FinishInstructions).
///   - Niveles 2..8: al cerrar el popup de resultados SOLO si se superó el nivel
///     anterior (GameAudioManager.EndLevel).
/// </summary>
public class LevelInstructions : MonoBehaviour
{
    public static LevelInstructions Instance;

    [Header("Configuración")]
    [Tooltip("Nombre del JSON sin extensión (en StreamingAssets/ o Resources/)")]
    [SerializeField] private string jsonFileName = "level_data";
    [Tooltip("Si es true carga desde Resources/, si es false desde StreamingAssets/")]
    [SerializeField] private bool loadFromResources = false;

    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button closeBtn;
    [Tooltip("Raíz del popup (incluye el fondo negro semitransparente). Se enciende/apaga acá.")]
    [SerializeField] private GameObject panelRoot;

    private List<SlideData> slides = new List<SlideData>();

    private void Awake()
    {
        Instance = this;

        // El popup arranca SIEMPRE apagado: solo aparece en el momento esperado
        // (inicio de cada nivel), nunca al darle Play.
        ShowPanel(false);

        // closeBtn: cierra el popup informativo sin más. El jugador queda en el
        // menú y arranca el nivel cuando pulsa "Entrenar".
        if (closeBtn != null)
            closeBtn.onClick.AddListener(Hide);
    }

    private void Start()
    {
        LoadJSON();
    }

    // ────────────────────────────────────────────────────────────────────────
    // API pública
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Muestra el popup con el texto del nivel cuyo id coincide con levelID.
    /// Siempre aparece limpio: limpia el contenido previo antes de setear el nuevo.
    /// </summary>
    public void ShowForLevel(int levelID)
    {
        if (slides == null || slides.Count == 0)
        {
            Debug.LogWarning("[LevelInstructions] No hay slides cargados todavía. " +
                             "¿Se llamó a ShowForLevel antes de cargar el JSON?");
            return;
        }

        SlideData slide = slides.Find(s => s.id == levelID);
        if (slide == null)
        {
            Debug.LogError($"[LevelInstructions] No hay ningún slide con id {levelID} " +
                           $"en {jsonFileName}.json. Revisá el levelID del LevelData.");
            return;
        }

        // Limpia info antigua para que el popup SIEMPRE aparezca limpio.
        if (titleText != null) titleText.text = string.Empty;
        if (bodyText != null) bodyText.text = string.Empty;

        // Setea el texto correcto del nivel.
        if (titleText != null)
        {
            titleText.text = slide.title;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(slide.title));
        }

        if (bodyText != null)
            bodyText.text = slide.text;

        ShowPanel(true);
    }

    /// <summary>Cierra el popup informativo.</summary>
    public void Hide()
    {
        ShowPanel(false);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Carga del JSON (idéntica a TypewriterInstructions)
    // ────────────────────────────────────────────────────────────────────────

    private void ShowPanel(bool show)
    {
        if (panelRoot != null)
            panelRoot.SetActive(show);

        // Mientras el popup informativo está abierto, bloquea los botones del
        // juego que quedan por detrás (Entrenar, sonidos…) para que no se puedan
        // pulsar sin querer. Se reactivan al cerrarlo. El botón de cerrar del
        // propio popup NO debe estar en UIInteractionManager.buttons.
        if (UIInteractionManager.Instance != null)
        {
            if (show)
                UIInteractionManager.Instance.LockAll();
            else
                UIInteractionManager.Instance.UnlockAll();
        }
    }

    private void LoadJSON()
    {
        string json = string.Empty;

        if (loadFromResources)
        {
            TextAsset asset = Resources.Load<TextAsset>(jsonFileName);
            if (asset == null)
            {
                Debug.LogError($"[LevelInstructions] No se encontró '{jsonFileName}' en Resources/");
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
                Debug.LogError($"[LevelInstructions] Archivo no encontrado: {path}");
                return;
            }
            json = System.IO.File.ReadAllText(path);
#endif
        }

        ParseJSON(json);
    }

    // Carga vía UnityWebRequest para plataformas donde StreamingAssets no es
    // accesible por System.IO (Android: dentro del APK; WebGL: servido por HTTP).
    private IEnumerator LoadJSONFromUWR(string url)
    {
        using (var req = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LevelInstructions] Error cargando JSON desde '{url}': {req.error}");
                yield break;
            }
            ParseJSON(req.downloadHandler.text);
        }
    }

    private void ParseJSON(string json)
    {
        SlidesRoot root = JsonUtility.FromJson<SlidesRoot>(json);

        if (root == null || root.slides == null || root.slides.Count == 0)
        {
            Debug.LogError("[LevelInstructions] JSON vacío o mal formateado.");
            return;
        }

        // Solo guardamos los datos. NO mostramos el panel: eso lo decide
        // ShowForLevel en el momento esperado.
        slides = root.slides;
    }
}
