using System.Collections;
using UnityEngine;

public class BirdController : MonoBehaviour
{
    public Transform[] notePositions;

    [Header("Music")]
    public float bpm = 77f;

    [Header("Visual")]
    public float arcHeight = 120f;
    public float hoverOffsetY = 107f;

    [Header("Sequence")]
    public int[] melodySequence;


    public void Play() => StartCoroutine(PlayMelody());


    IEnumerator PlayMelody()
    {
        foreach (int noteIndex in melodySequence)
        {
            yield return StartCoroutine(
                FlyToBeat(noteIndex, 1f)
            );
        }
    }

    public IEnumerator FlyToBeat(int targetIndex, float beats)
    {
        float duration = BeatsToSeconds(beats);

        Vector3 start = transform.position;

        Vector3 end = notePositions[targetIndex].position;

        end.y += hoverOffsetY;

        float time = 0;

        while (time < duration)
        {
            float t = time / duration;

            Vector3 pos = Vector3.Lerp(start, end, t);

            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            transform.position = pos;

            time += Time.deltaTime;

            yield return null;
        }

        transform.position = end;
    }

    float BeatsToSeconds(float beats)
    {
        return (60f / bpm) * beats;
    }
}
// FlyToBeat(0, 1f); // negra
// FlyToBeat(1, 0.5f); // corchea
// FlyToBeat(2, 2f); // blanca
// FlyToBeat(3, 4f); // redonda