using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class TileGoal
{
    public TileData tileType;
    public int targetCount;
    public int currentCount;
}


public class Match3GameManager : MonoBehaviour
{
    public enum BoardState { Idle, Swapping, Resolving, Refilling, CheckEnd }

    [Header("Board Config")]
    public int boardWidth = 8;
    public int boardHeight = 8;
    public TileData[] availableTileTypes;

    [Header("Tuning")]
    public float swapAnimDuration = 0.15f;
    public float resolveStepDelay = 0.2f;

    [Header("Game Rules")]
    public int maxMoves = 20;
    public List<TileGoal> levelGoals;

    public BoardState CurrentState { get; private set; }
    public GravityDirection CurrentGravity { get; private set; }
    public int MovesLeft { get; private set; }

    
    public event System.Action<int, int, int, int> OnTilesSwapped;
    public event System.Action<int, int, int, int> OnTilesSwapFailed;
    public event System.Action<List<List<BoardCoord>>> OnMatchesFound;
    public event System.Action<List<TileMove>> OnGravityApplied;
    public event System.Action<List<TileSpawnMove>> OnTilesSpawned;
    public event System.Action<int> OnScoreChanged;
    public event System.Action<GravityDirection> OnGravityDirectionChanged;
    public event System.Action<List<TileGoal>> OnGoalsUpdated;
    public event System.Action<bool> OnGameEnded; // true = Win, false = Loss
    public event System.Action OnBoardReset;

    private BoardModel board;
    private int score;
    private bool isProcessingSwap;

   
    public string GetTileIdAt(int x, int y) => board.GetTile(x, y);

    public TileData GetTileDataById(string tileId)
    {
        return System.Array.Find(availableTileTypes, t => t.tileId == tileId);
    }

    private void Awake()
    {
        board = new BoardModel(boardWidth, boardHeight);
        CurrentGravity = GravityDirection.Down;
        MovesLeft = maxMoves;

      
        if (levelGoals == null || levelGoals.Count == 0)
        {
            levelGoals = new List<TileGoal>();
            if (availableTileTypes != null)
            {
                foreach (var tileType in availableTileTypes)
                {
                    if (levelGoals.Count < 3 && tileType != null)
                    {
                        levelGoals.Add(new TileGoal
                        {
                            tileType = tileType,
                            targetCount = 15,
                            currentCount = 0
                        });
                    }
                }
            }
        }

        PopulateInitialBoard();
    }

    private void Start()
    {
        
        OnScoreChanged?.Invoke(score);
        OnGoalsUpdated?.Invoke(levelGoals);
        OnGravityDirectionChanged?.Invoke(CurrentGravity);
        SetState(BoardState.Idle);
    }


    public void SetState(BoardState newState)
    {
        CurrentState = newState;
        StopAllCoroutines();

        switch (CurrentState)
        {
            case BoardState.Idle:
                isProcessingSwap = false;
                break;

            case BoardState.Swapping:
                break;

            case BoardState.Resolving:
                StartCoroutine(ResolveMatchesRoutine());
                break;

            case BoardState.Refilling:
                StartCoroutine(RefillRoutine());
                break;

            case BoardState.CheckEnd:
                StartCoroutine(CheckEndRoutine());
                break;
        }
    }

  
    public void TryPlayerSwap(int x1, int y1, int x2, int y2)
    {
        if (CurrentState != BoardState.Idle || isProcessingSwap) return;

        isProcessingSwap = true;
        SetState(BoardState.Swapping);

        bool swapped = board.SwapTiles(x1, y1, x2, y2);
        if (!swapped)
        {
            isProcessingSwap = false;
            SetState(BoardState.Idle);
            return;
        }

        var matchesAfterSwap = MatchResolver.FindMatches(board);
        if (matchesAfterSwap.Count == 0)
        {
       
            board.SwapTiles(x1, y1, x2, y2);
            
            // Penalize wrong move: cost 1 move!
            MovesLeft--;
            
            OnTilesSwapFailed?.Invoke(x1, y1, x2, y2);
            StartCoroutine(RevertSwapStateRoutine());
            return;
        }

        // Valid move
        MovesLeft--;
        OnTilesSwapped?.Invoke(x1, y1, x2, y2);
        StartCoroutine(CompleteSwapAndResolveRoutine());
    }

    private IEnumerator CompleteSwapAndResolveRoutine()
    {
        // Wait for visual swap animation to finish before triggering matches
        yield return new WaitForSeconds(swapAnimDuration);
        SetState(BoardState.Resolving);
    }

    private IEnumerator RevertSwapStateRoutine()
    {
        // Wait for double swap animation (swap + swap-back duration)
        yield return new WaitForSeconds(swapAnimDuration * 2.1f);
        isProcessingSwap = false;
        SetState(BoardState.CheckEnd); 
    }


    public void TryGravityShift(GravityDirection newDirection)
    {
        if (CurrentState != BoardState.Idle || isProcessingSwap) return;
        if (newDirection == CurrentGravity) return; 

        isProcessingSwap = true;
        SetState(BoardState.Swapping);

        CurrentGravity = newDirection;
        MovesLeft--;

        OnGravityDirectionChanged?.Invoke(CurrentGravity);

        // Active mechanic: pop 3 random tiles to create slide opportunities
        ClearRandomTiles(3);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGravityShift();


        StartCoroutine(CompleteGravityShiftRoutine());
    }

    private IEnumerator CompleteGravityShiftRoutine()
    {
        yield return new WaitForSeconds(resolveStepDelay * 1.1f);
        SetState(BoardState.Refilling); 
    }

    public void RestartGame()
    {
        score = 0;
        MovesLeft = maxMoves;
        CurrentGravity = GravityDirection.Down;

        if (levelGoals != null)
        {
            foreach (var goal in levelGoals)
            {
                goal.currentCount = 0;
            }
        }

        board = new BoardModel(boardWidth, boardHeight);
        PopulateInitialBoard();

        OnScoreChanged?.Invoke(score);
        OnGoalsUpdated?.Invoke(levelGoals);
        OnGravityDirectionChanged?.Invoke(CurrentGravity);
        OnBoardReset?.Invoke();

        SetState(BoardState.Idle);
    }


    private IEnumerator ResolveMatchesRoutine()
    {
        var matches = MatchResolver.FindMatches(board);
        if (matches.Count == 0)
        {
            SetState(BoardState.CheckEnd);
            yield break;
        }

        OnMatchesFound?.Invoke(matches);

        var allCoords = new List<BoardCoord>();
        int roundScore = 0;
        foreach (var group in matches)
        {
            allCoords.AddRange(group);
            roundScore += ScoreForGroup(group);

           
            TrackObjectivesForGroup(group);
        }

        score += roundScore;
        OnScoreChanged?.Invoke(score);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayMatch();

        yield return new WaitForSeconds(resolveStepDelay);

        board.ClearTiles(allCoords);
        SetState(BoardState.Refilling);
    }

    private IEnumerator RefillRoutine()
    {
    
        var moves = board.ApplyGravity(CurrentGravity);
        if (moves.Count > 0)
        {
            OnGravityApplied?.Invoke(moves);
            yield return new WaitForSeconds(resolveStepDelay);
        }

      
        var spawnMoves = board.FillEmptyCellsAndGetMoves(CurrentGravity, RandomTileId);
        if (spawnMoves.Count > 0)
        {
            OnTilesSpawned?.Invoke(spawnMoves);
            yield return new WaitForSeconds(resolveStepDelay);
        }


        var cascadeMatches = MatchResolver.FindMatches(board);
        SetState(cascadeMatches.Count > 0 ? BoardState.Resolving : BoardState.CheckEnd);
    }

    private IEnumerator CheckEndRoutine()
    {
        isProcessingSwap = false;

    
        bool allGoalsMet = true;
        if (levelGoals != null && levelGoals.Count > 0)
        {
            foreach (var goal in levelGoals)
            {
                if (goal.currentCount < goal.targetCount)
                {
                    allGoalsMet = false;
                    break;
                }
            }
        }
        else
        {
            allGoalsMet = false;
        }

        if (allGoalsMet)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayVictory();
            OnGameEnded?.Invoke(true);
            yield break; 
        }
        else if (MovesLeft <= 0)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDefeat();
            OnGameEnded?.Invoke(false);
            yield break; 
        }

        yield return null;
        SetState(BoardState.Idle);
    }

    
    private void ClearRandomTiles(int count)
    {
        var targetCoords = new List<BoardCoord>();
        var eligibleCoords = new List<BoardCoord>();

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                string tileId = board.GetTile(x, y);
                if (tileId != null) eligibleCoords.Add(new BoardCoord(x, y));
            }
        }

       
        for (int i = 0; i < eligibleCoords.Count; i++)
        {
            int temp = Random.Range(i, eligibleCoords.Count);
            var hold = eligibleCoords[i];
            eligibleCoords[i] = eligibleCoords[temp];
            eligibleCoords[temp] = hold;
        }

        int cleared = 0;
        for (int i = 0; i < eligibleCoords.Count && cleared < count; i++)
        {
            targetCoords.Add(eligibleCoords[i]);
            cleared++;
        }

        
        TrackObjectivesForCoords(targetCoords);

        board.ClearTiles(targetCoords);

        
         var mockMatchList = new List<List<BoardCoord>> { targetCoords };
        OnMatchesFound?.Invoke(mockMatchList);
    }

    private void TrackObjectivesForGroup(List<BoardCoord> group)
    {
        TrackObjectivesForCoords(group);
    }

    private void TrackObjectivesForCoords(IEnumerable<BoardCoord> coords)
    {
        if (levelGoals == null) return;

        bool updated = false;
        foreach (var coord in coords)
        {
            string tileId = board.GetTile(coord.X, coord.Y);
            if (tileId == null) continue;

            foreach (var goal in levelGoals)
            {
                if (goal.tileType != null && goal.tileType.tileId == tileId)
                {
                    if (goal.currentCount < goal.targetCount)
                    {
                        goal.currentCount++;
                        updated = true;
                    }
                }
            }
        }

        if (updated)
        {
            OnGoalsUpdated?.Invoke(levelGoals);
        }
    }

    private int ScoreForGroup(List<BoardCoord> group)
    {
        int total = 0;
        foreach (var coord in group)
        {
            string tileId = board.GetTile(coord.X, coord.Y);
            TileData data = GetTileDataById(tileId);
            total += data != null ? data.baseScoreValue : 0;
        }
        if (group.Count > 3) total += (group.Count - 3) * 5;
        return total;
    }

    private void PopulateInitialBoard()
    {
         for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                string tileId = GetNonMatchingTileId(x, y);
                board.SetTile(x, y, tileId);
            }
        }
    }

    private string GetNonMatchingTileId(int x, int y)
    {
        List<string> options = new List<string>();
        foreach (var t in availableTileTypes) options.Add(t.tileId);


        for (int i = 0; i < options.Count; i++)
        {
            int temp = Random.Range(i, options.Count);
            string hold = options[i];
            options[i] = options[temp];
            options[temp] = hold;
        }

        foreach (var opt in options)
        {
            // Check left
            if (x >= 2 && board.GetTile(x - 1, y) == opt && board.GetTile(x - 2, y) == opt) continue;
            // Check down
            if (y >= 2 && board.GetTile(x, y - 1) == opt && board.GetTile(x, y - 2) == opt) continue;

            return opt;
        }

        return RandomTileId();
    }

    private string RandomTileId()
    {
        if (availableTileTypes == null || availableTileTypes.Length == 0) return null;
        int i = Random.Range(0, availableTileTypes.Length);
        return availableTileTypes[i].tileId;
    }
}
