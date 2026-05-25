using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SoundsBtns : MonoBehaviour
{
    public Button button;
    public AudioSource audioSource;

    private void Start()
    {
        button.onClick.AddListener(OnPressed);
    }

    private void OnPressed()
    {
        if (!UIInteractionManager.Instance.CanInteract())
            return;

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        UIInteractionManager.Instance.LockAll();

        audioSource.Play();

        yield return new WaitWhile(() => audioSource.isPlaying);

        UIInteractionManager.Instance.UnlockAll();
    }
}