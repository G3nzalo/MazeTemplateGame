using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Control de tempo (BPM) en pantalla.
//
// Uso: asignar dos botones (+ / -) y un TMP_Text donde se muestra el BPM actual.
//   - Cada pulsación cambia el BPM en GameAudioManager.bpmStep, con clamp
//     entre minBpm y maxBpm.
//   - El BPM queda persistido entre sesiones (PlayerPrefs).
//   - Los cambios se ignoran mientras un entrenamiento está EN CURSO (igual que
//     la octava), pero el display sigue mostrando el valor vigente.
public class BpmControlUI : MonoBehaviour
{
    [Header("Botones")]
    public Button increaseButton;
    public Button decreaseButton;

    [Header("Display")]
    [Tooltip("Texto donde se muestra el BPM actual.")]
    public TMP_Text bpmLabel;

    [Tooltip("Formato del texto. {0} se reemplaza por el valor de BPM.")]
    public string format = "{0} BPM";

    private void Start()
    {
        if (increaseButton != null)
            increaseButton.onClick.AddListener(OnIncrease);

        if (decreaseButton != null)
            decreaseButton.onClick.AddListener(OnDecrease);

        Refresh();
    }

    private void OnIncrease()
    {
        Change(+1);
    }

    private void OnDecrease()
    {
        Change(-1);
    }

    private void Change(int sign)
    {
        if (GameAudioManager.Instance == null)
            return;

        GameAudioManager.Instance.ChangeBpm(
            sign * GameAudioManager.Instance.bpmStep);

        Refresh();
    }

    // Actualiza el texto con el BPM vigente. Llamable también desde fuera
    // (p.ej. al terminar un entrenamiento) para refrescar el display.
    public void Refresh()
    {
        if (bpmLabel == null || GameAudioManager.Instance == null)
            return;

        bpmLabel.text = string.Format(
            format,
            GameAudioManager.Instance.Bpm);
    }
}
