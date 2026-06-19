using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Bird Trainer/Level")]
public class LevelData : ScriptableObject
{
    public int bpm = 77;
    public int levelID = 0; 
    
    public List<NoteEvent> notes;

    [Header("Difficulty")]

    [Tooltip("Tolerancia de afinación en cents respecto a la nota objetivo. " +
             "±10 = exigente (spec), valores mayores = más fácil.")]
    [Range(5f, 50f)]
    public float tuningToleranceCents = 10f;

    [Tooltip("Porcentaje de frames dentro de tolerancia para dar UNA nota por afinada (spec: >70%).")]
    [Range(0f, 100f)]
    public float perNoteTuningPercent = 70f;

    [Tooltip("Porcentaje de notas correctas para aprobar el NIVEL completo.")]
    [Range(0f, 100f)]
    public float passPercentage = 85f;
}
