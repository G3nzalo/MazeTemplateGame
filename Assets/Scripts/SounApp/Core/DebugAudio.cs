using System.Text;
using TMPro;
using UnityEngine;

public class DebugAudio : MonoBehaviour
{
    public static DebugAudio Instance;

    public TMP_Text text;

    private StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        Instance = this;
    }

    public void AddLine(string line)
    {
        sb.AppendLine(line);
        text.text = sb.ToString();
    }

    public void Clear()
    {
        sb.Clear();
        text.text = "";
    }

    public void AddHeader(string _text)
    {
        sb.AppendLine("\n=== " + _text + " ===");
        text.text = sb.ToString();
    }

    public void AddResult(string _text)
    {
        sb.AppendLine(_text);
        text.text = sb.ToString();
    }
}