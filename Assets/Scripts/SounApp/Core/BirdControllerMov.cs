using System.Collections;
using UnityEngine;

public class BirdControllerMov : MonoBehaviour
{
    public Transform[] notePositions;

    [Header("Visual")]
    public float arcHeight = 120f;

    public float hoverOffsetY = 107f;

    public IEnumerator FlyTo(
        int targetIndex,
        float duration)
    {
        Vector3 start =
            transform.position;

        Vector3 end =
            notePositions[targetIndex].position;

        end.y += hoverOffsetY;

        float time = 0f;

        while (time < duration)
        {
            float t =
                time / duration;

            Vector3 pos =
                Vector3.Lerp(
                    start,
                    end,
                    t);

            pos.y +=
                Mathf.Sin(t * Mathf.PI)
                * arcHeight;

            transform.position = pos;

            time += Time.deltaTime;

            yield return null;
        }

        transform.position = end;
    }
}