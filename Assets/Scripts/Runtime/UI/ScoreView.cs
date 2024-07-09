using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _scoreTxt = null;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdateScore()
    {
        ScoreSystem.Instance.OnCollectedCoin();
        string currentScore = ScoreSystem.Instance.GetScore;
        _scoreTxt.text = currentScore;
    }

    public void ResetScore()
    {
        _scoreTxt.text = ScoreSystem.Instance.OnResetScore();
    }
}