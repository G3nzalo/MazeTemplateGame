using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel de resultado de fin de nivel.
//
// Muestra:
//   - title_txt    : "Superaste el nivel!" / "No superaste el nivel!"
//   - subtitle_txt : "% de las notas cantadas correctamente" (solo aciertos)
//   - text         : lista de SOLO los errores del nivel (en el scroll)
//   - nextBtn      : "Siguiente Nivel" (si pasó) / "Reintentar" (si no pasó)
//
// El panel (panelPopUpResult) arranca apagado y solo se enciende al terminar
// cada nivel mediante ShowResult().
public class DebugAudio : MonoBehaviour
{
    public static DebugAudio Instance;

    [Header("Panel")]
    [Tooltip("GameObject del popup de resultado. Se enciende SOLO al final del nivel.")]
    public GameObject panelPopUpResult;

    [Header("Resultado")]
    public TMP_Text title_txt;
    public TMP_Text subtitle_txt;

    [Header("Botón continuar / reintentar")]
    public Button nextBtn;
    [Tooltip("Texto del nextBtn. Si se deja vacío, se busca en sus hijos.")]
    public TMP_Text nextBtnLabel;

    [Header("Botón cerrar panel")]
    [Tooltip("btn_closePanel: cierra el popup sin entrenar, para poder cambiar " +
             "BPM/octava entre niveles. Asignar el botón hijo del panel.")]
    public Button closeBtn;

    [Header("Errores")]
    [Tooltip("Texto donde se listan SOLO los errores del nivel.")]
    public TMP_Text text;
    public ScrollRect scrollRect;

    private StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        Instance = this;

        if (panelPopUpResult != null)
            panelPopUpResult.SetActive(false);

        // btn_closePanel: cierra el popup para dejar tocar las configuraciones
        // (BPM / octava) entre nivel y nivel. No entrena: solo oculta el panel.
        if (closeBtn != null)
            closeBtn.onClick.AddListener(Hide);
    }

    // Agrega una línea de ERROR al scroll (solo notas falladas).
    public void AddError(string line)
    {
        sb.AppendLine(line);
        Refresh();
    }

    public void Clear()
    {
        sb.Clear();
        Refresh();
    }

    // Enciende el panel y vuelca el resultado final del nivel.
    public void ShowResult(bool passed, float accuracyPercent, bool hasNextLevel)
    {
        if (panelPopUpResult != null)
            panelPopUpResult.SetActive(true);

        if (title_txt != null)
            title_txt.text = passed
                ? "Superaste el nivel!"
                : "No superaste el nivel!";

        if (subtitle_txt != null)
            subtitle_txt.text =
                $"{accuracyPercent:F0}% de las notas cantadas correctamente";

        // "Siguiente Nivel" solo si pasó Y existe un nivel siguiente.
        if (NextButtonLabel() != null)
            NextButtonLabel().text = (passed && hasNextLevel)
                ? "Siguiente Nivel"
                : "Reintentar";

        // Si no hubo errores, deja claro que el nivel salió perfecto.
        if (text != null && sb.Length == 0)
            text.text = "¡Sin errores!";
    }

    // Apaga el panel (al pasar al siguiente nivel o reintentar).
    public void Hide()
    {
        if (panelPopUpResult != null)
            panelPopUpResult.SetActive(false);
    }

    private TMP_Text NextButtonLabel()
    {
        if (nextBtnLabel != null)
            return nextBtnLabel;

        if (nextBtn != null)
            nextBtnLabel = nextBtn.GetComponentInChildren<TMP_Text>();

        return nextBtnLabel;
    }

    void Refresh()
    {
        if (text != null)
            text.text = sb.ToString();

        // auto scroll al final
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}
