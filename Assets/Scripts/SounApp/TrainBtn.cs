using UnityEngine;
using UnityEngine.UI;

public class TrainBtn : MonoBehaviour
{
    public Button trainButton;

    public BirdController bird;

    private void Start()
    {
        trainButton.onClick.AddListener(OnTrainPressed);
    }

    private void OnTrainPressed()
    {
        if (!UIInteractionManager.Instance.CanInteract())
            return;

        UIInteractionManager.Instance.LockAll();

        Debug.Log("Entrenamiento iniciado");

        bird.Play();

        // Aquí cargas state/gameplay/etc
    }
}