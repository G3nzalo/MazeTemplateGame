using System.Collections;
using UnityEngine;

public class BirdControllerMov : MonoBehaviour
{
    public Transform[] notePositions;

    [Header("Visual")]
    public float arcHeight = 120f;

    public float hoverOffsetY = 107f;

    [Header("Orientación")]
    [Tooltip("Giro en Y (grados) cuando el pájaro se mueve de IZQUIERDA a DERECHA.")]
    public float yawMovingRight = 180f;

    [Tooltip("Giro en Y (grados) cuando el pájaro se mueve de DERECHA a IZQUIERDA.")]
    public float yawMovingLeft = 0f;

    [Tooltip("Diferencia mínima en X para considerar que hubo movimiento horizontal. " +
             "Evita giros cuando el destino está prácticamente en la misma columna.")]
    public float directionThreshold = 0.01f;

    // Yaw destino actual según la última dirección horizontal.
    private float currentYaw;

    private void Awake()
    {
        currentYaw = transform.localEulerAngles.y;
    }

    public IEnumerator FlyTo(
        int targetIndex,
        float duration)
    {
        Vector3 start =
            transform.position;

        Vector3 end =
            notePositions[targetIndex].position;

        end.y += hoverOffsetY;

        // Decide la orientación según la dirección horizontal del movimiento:
        // izquierda -> derecha = yawMovingRight (180); derecha -> izquierda = yawMovingLeft (0).
        // Si el destino está en la misma columna, conserva la orientación actual.
        if (end.x > start.x + directionThreshold)
            currentYaw = yawMovingRight;
        else if (end.x < start.x - directionThreshold)
            currentYaw = yawMovingLeft;

        // Gira ANTES de empezar a volar, así arranca el vuelo ya orientado.
        Vector3 euler =
            transform.localEulerAngles;

        transform.localRotation =
            Quaternion.Euler(
                euler.x,
                currentYaw,
                euler.z);

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
