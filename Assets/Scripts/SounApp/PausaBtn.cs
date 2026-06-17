using UnityEngine;
using UnityEngine.UI;

// Botón de pausa/stop del nivel. Aborta el entrenamiento en curso y deja el
// juego listo para volver a empezar.
//
// IMPORTANTE: este botón NO debe estar incluido en el array
// UIInteractionManager.buttons. Esos botones se desactivan con LockAll() al
// empezar el nivel, y entonces el botón de pausa quedaría sin poder pulsarse
// justo cuando hace falta. Dejalo fuera de esa lista.
public class PausaBtn : MonoBehaviour
{
    public Button pausaBtn;

    private void Start()
    {
        if (pausaBtn != null)
            pausaBtn.onClick.AddListener(OnPausaPressed);
    }

    void OnPausaPressed()
    {
        if (GameAudioManager.Instance == null)
            return;

        // Corta el nivel en curso (si no hay nivel corriendo, no hace nada).
        GameAudioManager.Instance.StopTraining();
    }
}
