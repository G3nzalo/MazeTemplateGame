using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create GameDataConfiguration", fileName = "GameDataConfiguration", order = 0)]
public class GameDataConfiguration : ScriptableObject
{
    [SerializeField] int _scorePerCoin;
    public int ScorePerCoin => _scorePerCoin;
}
