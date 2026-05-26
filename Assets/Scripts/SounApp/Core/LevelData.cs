using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Bird Trainer/Level")]
public class LevelData : ScriptableObject
{
    public int bpm = 77;

    public List<NoteEvent> notes;

    [Header("Difficulty")]

    [Tooltip("Porcentaje máximo permitido de error")]
    [Range(0f, 10f)]
    public float pitchTolerancePercent = 2f;

    [Tooltip("Porcentaje necesario para aprobar")]
    [Range(0f, 100f)]
    public float passPercentage = 85f;
}
