using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation layer for the match-3 board. Renders tiles, handles mouse inputs
/// (selection & swaps) and keyboard gravity inputs, triggers board shake,
/// manages smooth movement animations, and spawns particle VFX.
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("Wiring")]
    public Match3GameManager gameManager;
    public GameObject tilePrefab;       // Simple prefab with a SpriteRenderer
    public float cellSize = 1f;
    public Vector2 boardOrigin = Vector2.zero;

    [Header("Feedback")]
    public float swapAnimSeconds = 0.12f;
    public float matchPunchSeconds = 0.18f;
    public float matchPunchScale = 1.3f;

    private GameObject[,] tileViews;
    private Vector2Int? firstSelected;

    private void Start()
    {
        if (gameManager == null)
        {
            Debug.LogError("BoardView: gameManager reference not set.");
            return;
        }

        tileViews = new GameObject[gameManager.boardWidth, gameManager.boardHeight];
        BuildInitialView();

        // Subscribe to GameManager's events
        gameManager.OnTilesSwapped += HandleTilesSwapped;
        gameManager.OnMatchesFound += HandleMatchesFound;
        gameManager.OnGravityApplied += HandleGravityApplied;
        gameManager.OnTilesSpawned += HandleTilesSpawned;
        gameManager.OnScoreChanged += HandleScoreChanged;
        gameManager.OnBoardReset += HandleBoardReset;
    }

    private void OnDestroy()
    {
        if (gameManager == null) return;
        gameManager.OnTilesSwapped -= HandleTilesSwapped;
        gameManager.OnMatchesFound -= HandleMatchesFound;
        gameManager.OnGravityApplied -= HandleGravityApplied;
        gameManager.OnTilesSpawned -= HandleTilesSpawned;
        gameManager.OnScoreChanged -= HandleScoreChanged;
        gameManager.OnBoardReset -= HandleBoardReset;
    }

    // ====================================================================
    //  PLAYER INPUT (Mouse/Tap & Keyboard Gravity Shifting)
    // ====================================================================
    private void Update()
    {
        if (gameManager.CurrentState != Match3GameManager.BoardState.Idle) return;

        // 1. Keyboard Gravity Shift
        HandleKeyboardGravityInput();

        // 2. Mouse Selection/Swap
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseSelection();
        }
    }

    private void HandleKeyboardGravityInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            gameManager.TryGravityShift(GravityDirection.Up);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            gameManager.TryGravityShift(GravityDirection.Down);
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            gameManager.TryGravityShift(GravityDirection.Left);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            gameManager.TryGravityShift(GravityDirection.Right);
        }
    }

    private void HandleMouseSelection()
    {
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int? cell = WorldToCell(worldPoint);
        if (cell == null) return;

        if (firstSelected == null)
        {
            firstSelected = cell;
            SetHighlight(cell.Value, true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelect();
            return;
        }

        Vector2Int first = firstSelected.Value;
        SetHighlight(first, false);

        if (first == cell.Value)
        {
            firstSelected = null;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelect();
            return;
        }

        // Validate adjacent swap check in View to avoid redundant manager state locks
        bool adjacent = (Mathf.Abs(first.x - cell.Value.x) == 1 && first.y == cell.Value.y) ||
                        (Mathf.Abs(first.y - cell.Value.y) == 1 && first.x == cell.Value.x);

        if (adjacent)
        {
            gameManager.TryPlayerSwap(first.x, first.y, cell.Value.x, cell.Value.y);
        }
        else
        {
            // Tapped non-adjacent tile: treat it as selecting a new tile instead
            firstSelected = cell;
            SetHighlight(cell.Value, true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelect();
            return;
        }

        firstSelected = null;
    }

    private Vector2Int? WorldToCell(Vector2 worldPoint)
    {
        int x = Mathf.RoundToInt((worldPoint.x - boardOrigin.x) / cellSize);
        int y = Mathf.RoundToInt((worldPoint.y - boardOrigin.y) / cellSize);
        if (x < 0 || x >= gameManager.boardWidth || y < 0 || y >= gameManager.boardHeight) return null;
        return new Vector2Int(x, y);
    }

    private void SetHighlight(Vector2Int cell, bool on)
    {
        GameObject view = tileViews[cell.x, cell.y];
        if (view == null) return;
        view.transform.localScale = on ? Vector3.one * 1.15f : Vector3.one;
    }

    // ====================================================================
    //  EVENT HANDLERS & VISUAL ANIMATIONS
    // ====================================================================
    private void HandleTilesSwapped(int x1, int y1, int x2, int y2)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySwap();
        StartCoroutine(AnimateSwap(x1, y1, x2, y2));
    }

    private IEnumerator AnimateSwap(int x1, int y1, int x2, int y2)
    {
        GameObject a = tileViews[x1, y1];
        GameObject b = tileViews[x2, y2];
        if (a == null || b == null) yield break;

        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;
        float t = 0f;

        while (t < swapAnimSeconds)
        {
            t += Time.deltaTime;
            float frac = t / swapAnimSeconds;
            a.transform.position = Vector3.Lerp(posA, posB, frac);
            b.transform.position = Vector3.Lerp(posB, posA, frac);
            yield return null;
        }

        a.transform.position = posB;
        b.transform.position = posA;

        tileViews[x1, y1] = b;
        tileViews[x2, y2] = a;
    }

    private void HandleMatchesFound(List<List<BoardCoord>> matches)
    {
        foreach (var group in matches)
        {
            foreach (var coord in group)
            {
                StartCoroutine(PunchAndClear(coord.X, coord.Y));
            }
        }

        // Add impact shake!
        if (BoardShake.Instance != null)
        {
            BoardShake.Instance.Shake(0.18f, 0.07f);
        }
    }

    private IEnumerator PunchAndClear(int x, int y)
    {
        GameObject view = tileViews[x, y];
        if (view == null) yield break;

        // Spawn visual burst particles matching the tile's sprite
        string tileId = gameManager.GetTileIdAt(x, y);
        TileData data = gameManager.GetTileDataById(tileId);
        if (data != null && TileVFX.Instance != null)
        {
            TileVFX.Instance.SpawnBurst(view.transform.position, data.tileColor);
        }

        Vector3 baseScale = Vector3.one;
        float t = 0f;
        while (t < matchPunchSeconds)
        {
            t += Time.deltaTime;
            float frac = t / matchPunchSeconds;
            float scale = Mathf.Lerp(matchPunchScale, 0f, frac);
            view.transform.localScale = baseScale * scale;
            yield return null;
        }

        Destroy(view);
        // Safely set null only if it hasn't been replaced by a fall slide
        if (tileViews[x, y] == view)
        {
            tileViews[x, y] = null;
        }
    }

    private void HandleGravityApplied(List<TileMove> moves)
    {
        foreach (var move in moves)
        {
            StartCoroutine(AnimateFall(move));
        }
    }

    private IEnumerator AnimateFall(TileMove move)
    {
        GameObject view = tileViews[move.FromX, move.FromY];
        if (view == null) yield break;

        if (tileViews[move.FromX, move.FromY] == view)
        {
            tileViews[move.FromX, move.FromY] = null;
        }
        tileViews[move.ToX, move.ToY] = view;

        Vector3 start = CellToWorld(move.FromX, move.FromY);
        Vector3 end = CellToWorld(move.ToX, move.ToY);
        float duration = 0.2f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            view.transform.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }
        view.transform.position = end;
    }

    private void HandleTilesSpawned(List<TileSpawnMove> spawnMoves)
    {
        foreach (var spawn in spawnMoves)
        {
            StartCoroutine(AnimateSpawn(spawn));
        }
    }

    private IEnumerator AnimateSpawn(TileSpawnMove spawn)
    {
        string tileId = spawn.TileId;
        GameObject view = Instantiate(tilePrefab, CellToWorld(spawn.SpawnX, spawn.SpawnY), Quaternion.identity, transform);
        
        TileData data = gameManager.GetTileDataById(tileId);
        SpriteRenderer renderer = view.GetComponent<SpriteRenderer>();
        if (renderer != null && data != null)
        {
            renderer.sprite = data.sprite;
            renderer.color = GetTileColor(data); // Apply color tinting
        }

        tileViews[spawn.DestX, spawn.DestY] = view;

        Vector3 start = CellToWorld(spawn.SpawnX, spawn.SpawnY);
        Vector3 end = CellToWorld(spawn.DestX, spawn.DestY);
        float duration = 0.2f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            view.transform.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }
        view.transform.position = end;
    }

    private void HandleBoardReset()
    {
        if (tileViews != null)
        {
            for (int x = 0; x < gameManager.boardWidth; x++)
            {
                for (int y = 0; y < gameManager.boardHeight; y++)
                {
                    if (tileViews[x, y] != null)
                    {
                        Destroy(tileViews[x, y]);
                        tileViews[x, y] = null;
                    }
                }
            }
        }

        firstSelected = null;
        tileViews = new GameObject[gameManager.boardWidth, gameManager.boardHeight];
        BuildInitialView();
    }

    // ====================================================================
    //  SCORE FEEDBACK WRAPPERS
    // ====================================================================
    public event System.Action<int> OnScoreDisplayChanged;
    private void HandleScoreChanged(int newScore)
    {
        OnScoreDisplayChanged?.Invoke(newScore);
    }

    // ====================================================================
    //  VIEW CONSTRUCTION HELPERS
    // ====================================================================
    private void BuildInitialView()
    {
        for (int x = 0; x < gameManager.boardWidth; x++)
        {
            for (int y = 0; y < gameManager.boardHeight; y++)
            {
                SpawnTileView(x, y);
            }
        }
    }

    private void SpawnTileView(int x, int y)
    {
        string tileId = gameManager.GetTileIdAt(x, y);
        if (tileId == null) return;

        GameObject view = Instantiate(tilePrefab, CellToWorld(x, y), Quaternion.identity, transform);
        TileData data = gameManager.GetTileDataById(tileId);
        SpriteRenderer renderer = view.GetComponent<SpriteRenderer>();
        if (renderer != null && data != null)
        {
            renderer.sprite = data.sprite;
            renderer.color = GetTileColor(data); // Apply color tinting
        }

        tileViews[x, y] = view;
    }

    private Color GetTileColor(TileData data)
    {
        if (data == null) return Color.white;
        
        // Fallback for transparent or plain white (uninitialized) tileColor values
        if (data.tileColor.a < 0.05f || data.tileColor == Color.white)
        {
            string id = data.tileId.ToLower();
            if (id.Contains("red")) return new Color(0.9f, 0.25f, 0.25f);
            if (id.Contains("blue")) return new Color(0.25f, 0.55f, 0.9f);
            if (id.Contains("green")) return new Color(0.25f, 0.75f, 0.35f);
            if (id.Contains("yellow")) return new Color(0.9f, 0.8f, 0.15f);
        }
        return data.tileColor;
    }

    private Vector3 CellToWorld(int x, int y)
    {
        return new Vector3(boardOrigin.x + x * cellSize, boardOrigin.y + y * cellSize, 0f);
    }
}
