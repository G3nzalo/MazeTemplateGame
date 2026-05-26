using UnityEngine;
using UnityEngine.UI;

public class TrainBtn : MonoBehaviour
{
    public Button trainButton;

    private void Start()
    {
        trainButton.onClick.AddListener(OnTrainPressed);
    }

    void OnTrainPressed()
    {
        if (!UIInteractionManager.Instance.CanInteract())
            return;

        GameAudioManager.Instance.StartTraining();
    }
}