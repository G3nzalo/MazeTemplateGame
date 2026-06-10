using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Botón de selección de registro vocal (octava).
//
// Uso: colocar un botón por opción y asignar su 'octaveOffset':
//   0  -> Voz grave  (C3-C4, típico masculino)
//   +1 -> Voz aguda  (C4-C5, típico femenino)
//   +2 -> Más aguda  (opcional, voces infantiles/sopranos)
//
// El jugador puede cambiar de octava LIBREMENTE en cualquier momento entre
// ejercicios; solo se ignora el cambio mientras un entrenamiento está en curso.
// La elección NO se persiste: vale solo para la sesión actual (igual que el BPM).
public class OctaveSelectorButton : MonoBehaviour
{
    public Button button;

    [Tooltip("Octavas a desplazar. 0 = voz grave (C3), +1 = voz aguda (C4).")]
    public int octaveOffset = 0;

    [Header("Visual (opcional)")]
    [Tooltip("Texto que se resalta cuando ESTA opción está seleccionada. " +
             "Ambas opciones se ven habilitadas; la activa solo cambia de color.")]
    public TMP_Text label;

    public Color selectedColor = new Color(0.30f, 0.95f, 0.40f, 1f);
    public Color normalColor = Color.white;

    private void Start()
    {
        if (button != null)
            button.onClick.AddListener(OnPressed);

        Refresh();
    }

    private void OnPressed()
    {
        if (GameAudioManager.Instance == null)
            return;

        // Solo se bloquea durante un entrenamiento EN CURSO. Entre ejercicios el
        // jugador puede alternar de octava cuantas veces quiera.
        if (GameAudioManager.Instance.LevelRunning)
            return;

        GameAudioManager.Instance.SetOctaveOffset(octaveOffset);

        // Actualiza el resaltado de todos los selectores de la escena.
        foreach (var selector in FindObjectsOfType<OctaveSelectorButton>())
            selector.Refresh();
    }

    // Resalta el botón si su octava coincide con la seleccionada actualmente.
    public void Refresh()
    {
        if (label == null || GameAudioManager.Instance == null)
            return;

        bool active = GameAudioManager.Instance.OctaveOffset == octaveOffset;
        label.color = active ? selectedColor : normalColor;
    }
}