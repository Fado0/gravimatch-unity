using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI HUD controller. Subscribes to Match3GameManager events to render
/// move counts, scores, tile goal collection status, current gravity direction,
/// and victory/defeat screens.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public Match3GameManager gameManager;

    [Header("UI Text Fields")]
    public TMP_Text movesText;
    public TMP_Text scoreText;
    public TMP_Text objectivesText;
    public TMP_Text gravityText;

    [Header("End Game Panel")]
    public GameObject endPanel;
    public TMP_Text endTitleText;
    public Button restartButton;

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<Match3GameManager>();
        }

        if (gameManager != null)
        {
            gameManager.OnScoreChanged += HandleScoreChanged;
            gameManager.OnGoalsUpdated += HandleGoalsUpdated;
            gameManager.OnGravityDirectionChanged += HandleGravityChanged;
            gameManager.OnGameEnded += HandleGameEnded;
            gameManager.OnBoardReset += HandleBoardReset;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartPressed);
        }

        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }

        // Initialize display values
        RefreshAllDisplays();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnScoreChanged -= HandleScoreChanged;
            gameManager.OnGoalsUpdated -= HandleGoalsUpdated;
            gameManager.OnGravityDirectionChanged -= HandleGravityChanged;
            gameManager.OnGameEnded -= HandleGameEnded;
            gameManager.OnBoardReset -= HandleBoardReset;
        }
    }

    private void RefreshAllDisplays()
    {
        if (gameManager == null) return;
        HandleScoreChanged(0);
        HandleGravityChanged(gameManager.CurrentGravity);
        HandleGoalsUpdated(gameManager.levelGoals);
    }

    private void HandleScoreChanged(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {newScore}";
        }
        UpdateMovesDisplay();
    }

    private void HandleGravityChanged(GravityDirection newGravity)
    {
        if (gravityText != null)
        {
            gravityText.text = $"Gravity: {newGravity.ToString().ToUpper()}";
        }
        UpdateMovesDisplay();
    }

    private void HandleGoalsUpdated(List<TileGoal> goals)
    {
        if (objectivesText == null || goals == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("LEVEL OBJECTIVES:");
        
        foreach (var goal in goals)
        {
            if (goal.tileType == null) continue;
            string colorName = goal.tileType.tileId.ToUpper();
            int remaining = Mathf.Max(0, goal.targetCount - goal.currentCount);

            if (remaining <= 0)
            {
                sb.AppendLine($"<color=#00FF00>• {colorName}: Complete!</color>");
            }
            else
            {
                sb.AppendLine($"• {colorName}: {goal.currentCount} / {goal.targetCount}");
            }
        }

        objectivesText.text = sb.ToString();
        UpdateMovesDisplay();
    }

    private void UpdateMovesDisplay()
    {
        if (movesText != null && gameManager != null)
        {
            movesText.text = $"Moves Left: {gameManager.MovesLeft}";
        }
    }

    private void HandleGameEnded(bool won)
    {
        if (endPanel != null)
        {
            endPanel.SetActive(true);
            if (endTitleText != null)
            {
                endTitleText.text = won ? "VICTORY!" : "DEFEAT!";
                endTitleText.color = won ? Color.green : Color.red;
            }
        }
    }

    private void HandleBoardReset()
    {
        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }
        RefreshAllDisplays();
    }

    private void OnRestartPressed()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }

    // ====================================================================
    //  PUBLIC gravity shift buttons callback helpers
    // ====================================================================
    public void ShiftGravityUp() => gameManager?.TryGravityShift(GravityDirection.Up);
    public void ShiftGravityDown() => gameManager?.TryGravityShift(GravityDirection.Down);
    public void ShiftGravityLeft() => gameManager?.TryGravityShift(GravityDirection.Left);
    public void ShiftGravityRight() => gameManager?.TryGravityShift(GravityDirection.Right);
}
