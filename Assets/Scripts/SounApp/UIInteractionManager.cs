using UnityEngine;
using UnityEngine.UI;

public class UIInteractionManager : MonoBehaviour
{
    public static UIInteractionManager Instance;

    public Button[] buttons;

    private bool isLocked;

    private void Awake()
    {
        Instance = this;
    }

    public bool CanInteract()
    {
        return !isLocked;
    }

    public void LockAll()
    {
        isLocked = true;

        foreach (Button btn in buttons)
        {
            btn.interactable = false;
        }
    }

    public void UnlockAll()
    {
        isLocked = false;

        foreach (Button btn in buttons)
        {
            btn.interactable = true;
        }
    }
}