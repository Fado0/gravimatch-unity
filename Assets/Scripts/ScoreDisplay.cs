using UnityEngine;
using TMPro;

/// <summary>
/// The smallest possible UI feedback element: a score label that updates
/// when BoardView reports a score change. Deliberately tiny and separate
/// from BoardView itself — swapping this for a different UI framework, or
/// adding a combo counter alongside it, never touches the rendering or
/// input code in BoardView.
/// </summary>
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
