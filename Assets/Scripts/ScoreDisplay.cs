using UnityEngine;
using TMPro;



public class ScoreDisplay : MonoBehaviour
{
    public BoardView boardView;
    public TMP_Text scoreLabel;

    private void Start()
    {
        if (boardView != null)
            boardView.OnScoreDisplayChanged += HandleScoreChanged;

        if (scoreLabel != null)
            scoreLabel.text = "Score: 0";
    }

    private void OnDestroy()
    {
        if (boardView != null)
            boardView.OnScoreDisplayChanged -= HandleScoreChanged;
    }

    private void HandleScoreChanged(int newScore)
    {
        if (scoreLabel != null)
            scoreLabel.text = $"Score: {newScore}";
    }
}
