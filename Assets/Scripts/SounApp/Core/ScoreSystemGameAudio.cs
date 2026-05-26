using UnityEngine;

public class ScoreSystemGameAudio : MonoBehaviour
{
    public int totalNotes;
    public int correctNotes;

    public void Register(bool success)
    {
        totalNotes++;

        if (success)
            correctNotes++;
    }

    public float AccuracyPercent()
    {
        if (totalNotes == 0)
            return 0;

        return
            (float)correctNotes
            / totalNotes
            * 100f;
    }

    public bool Passed(float required)
    {
        return AccuracyPercent() >= required;
    }
}