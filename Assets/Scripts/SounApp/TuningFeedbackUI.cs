using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Retroalimentación visual de afinación (estilo SingStar/Smule):
//   - Gráfico en tiempo real de la desviación (cents) respecto a la nota objetivo,
//     dibujado sobre una Texture2D que se muestra en un RawImage.
//   - Banda verde central = zona afinada (±tolerancia).
//   - Barra final verde/roja según el % de afinación de la nota.
//
// Se alimenta del evento VocalPitchDetector.OnPitchSample (un valor por hop),
// por lo que el gráfico avanza al ritmo del análisis y NO del frame rate.
//
// Conexión en el inspector (todo es opcional / null-safe):
//   detector      -> el VocalPitchDetector de la escena
//   graphImage    -> un RawImage donde se pinta la curva
//   resultBar     -> un Image (Image Type = Filled, Horizontal) para el % final
//   resultText    -> un TMP_Text opcional con el porcentaje
public class TuningFeedbackUI : MonoBehaviour
{
    [Header("References")]
    public VocalPitchDetector detector;
    public RawImage graphImage;

    [Header("Result Bar")]
    public Image resultBar;
    public TMP_Text resultText;

    [Header("Graph")]
    [Tooltip("Resolución de la textura del gráfico.")]
    public int texWidth = 256;
    public int texHeight = 96;

    [Tooltip("Rango vertical del gráfico en cents (±).")]
    public float centsRange = 50f;

    [Tooltip("Tolerancia de afinación (cents) para la banda verde. " +
             "Idealmente igual a LevelData.tuningToleranceCents.")]
    public float toleranceCents = 10f;

    [Header("Colors")]
    public Color background = new Color(0.08f, 0.08f, 0.10f, 1f);
    public Color toleranceBand = new Color(0.15f, 0.45f, 0.20f, 1f);
    public Color centerLine = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color inTune = new Color(0.30f, 0.95f, 0.40f, 1f);
    public Color outTune = new Color(0.95f, 0.35f, 0.30f, 1f);

    public Color passColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    public Color failColor = new Color(0.90f, 0.30f, 0.30f, 1f);

    private Texture2D tex;
    private Color32[] clearRow;          // plantilla de una columna "vacía" (fondo + grid)
    private Color32[] pixels;

    private float[] centsHistory;        // ring buffer de desviaciones
    private bool[] validHistory;
    private int head;
    private int count;

    private float targetFreq;
    private bool dirty;

    private void Awake()
    {
        if (texWidth < 8) texWidth = 8;
        if (texHeight < 8) texHeight = 8;

        tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        pixels = new Color32[texWidth * texHeight];
        centsHistory = new float[texWidth];
        validHistory = new bool[texWidth];

        if (graphImage) graphImage.texture = tex;

        BuildBackground();
        RedrawAll();
    }

    private void OnEnable()
    {
        if (detector != null)
            detector.OnPitchSample += HandleSample;
    }

    private void OnDisable()
    {
        if (detector != null)
            detector.OnPitchSample -= HandleSample;
    }

    // --- API llamada por GameAudioManager ---

    // Inicia el gráfico para una nota nueva (centra en su frecuencia objetivo).
    public void BeginNote(float targetFrequencyHz, float toleranceCentsForNote)
    {
        targetFreq = targetFrequencyHz;
        toleranceCents = toleranceCentsForNote;
        count = 0;
        head = 0;

        BuildBackground();
        RedrawAll();

        if (resultBar) resultBar.fillAmount = 0f;
        if (resultText) resultText.text = "";
    }

    // Pinta el resultado final de la nota (barra verde/roja).
    public void ShowResult(float tuningPercent, float passPercent)
    {
        bool passed = tuningPercent >= passPercent;

        if (resultBar)
        {
            resultBar.fillAmount = Mathf.Clamp01(tuningPercent / 100f);
            resultBar.color = passed ? passColor : failColor;
        }

        if (resultText)
            resultText.text = $"{tuningPercent:F0}%";
    }

    // --- Núcleo del gráfico ---

    private void HandleSample(float freqHz)
    {
        float cents;
        bool valid;

        if (targetFreq > 0f && freqHz > 0f)
        {
            cents = 1200f * Mathf.Log(freqHz / targetFreq, 2f);
            valid = true;
        }
        else
        {
            cents = 0f;
            valid = false;
        }

        centsHistory[head] = cents;
        validHistory[head] = valid;
        head = (head + 1) % texWidth;
        if (count < texWidth) count++;

        dirty = true;
    }

    private void LateUpdate()
    {
        if (!dirty) return;
        dirty = false;
        RedrawAll();
    }

    // Construye la columna base (fondo + banda de tolerancia + línea central).
    private void BuildBackground()
    {
        clearRow = new Color32[texHeight];

        int centerY = texHeight / 2;
        int bandHalf = CentsToHalfSpan(toleranceCents);

        for (int y = 0; y < texHeight; y++)
        {
            Color c = background;

            if (Mathf.Abs(y - centerY) <= bandHalf)
                c = toleranceBand;

            if (y == centerY)
                c = centerLine;

            clearRow[y] = c;
        }
    }

    private int CentsToHalfSpan(float cents)
    {
        float halfHeight = texHeight / 2f;
        return Mathf.RoundToInt(Mathf.Clamp01(cents / centsRange) * halfHeight);
    }

    private int CentsToY(float cents)
    {
        float halfHeight = texHeight / 2f;
        float norm = Mathf.Clamp(cents / centsRange, -1f, 1f);
        int y = Mathf.RoundToInt(texHeight / 2f + norm * halfHeight);
        return Mathf.Clamp(y, 0, texHeight - 1);
    }

    private void RedrawAll()
    {
        // Fondo (replica la columna base en todo el ancho).
        for (int x = 0; x < texWidth; x++)
            for (int y = 0; y < texHeight; y++)
                pixels[y * texWidth + x] = clearRow[y];

        // Dibuja las muestras de izquierda (más antigua) a derecha (más reciente).
        for (int i = 0; i < count; i++)
        {
            int bufIdx = (head - count + i + texWidth) % texWidth;
            if (!validHistory[bufIdx]) continue;

            float cents = centsHistory[bufIdx];
            int x = i; // columna = orden temporal (izq = más antigua)
            int y = CentsToY(cents);

            Color32 c = Mathf.Abs(cents) <= toleranceCents ? inTune : outTune;

            // Punto 3px de alto para que la línea sea visible.
            PlotColumn(x, y, c);
        }

        tex.SetPixels32(pixels);
        tex.Apply(false);
    }

    private void PlotColumn(int x, int y, Color32 c)
    {
        if (x < 0 || x >= texWidth) return;

        for (int dy = -1; dy <= 1; dy++)
        {
            int yy = y + dy;
            if (yy >= 0 && yy < texHeight)
                pixels[yy * texWidth + x] = c;
        }
    }
}