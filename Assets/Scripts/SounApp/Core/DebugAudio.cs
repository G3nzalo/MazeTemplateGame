using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugAudio : MonoBehaviour
{
    public static DebugAudio Instance;

    public TMP_Text text;
    public ScrollRect scrollRect;

    private StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        Instance = this;
    }

    public void AddLine(string line)
    {
        sb.AppendLine(line);
        Refresh();
    }

    public void AddHeader(string _text)
    {
        sb.AppendLine("\n=== " + _text + " ===");
        Refresh();
    }

    public void AddResult(string _text)
    {
        sb.AppendLine(_text);
        Refresh();
    }

    public void Clear()
    {
        sb.Clear();
        Refresh();
    }

    void Refresh()
    {
        text.text = sb.ToString();

        // 🔥 auto scroll al final
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}