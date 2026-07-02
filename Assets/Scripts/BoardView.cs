using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation layer for the match-3 board. Renders tiles, handles mouse inputs
/// (selection & swaps) and keyboard gravity inputs, triggers board shake,
/// manages smooth movement animations, and spawns particle VFX.
/// Spawns its own dark grid background slots and a dynamic world-space HUD.
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

    [Header("Visual Layout")]
    [Tooltip("Enlarges the tile local scale factor (1.0 = normal size)")]
    public float tileScale = 1.15f;

    private GameObject[,] tileViews;
    private Vector2Int? firstSelected;
    private TMPro.TextMeshPro worldHUDText;
    private GameObject bgParent;

    private void Start()
    {
        if (gameManager == null)
        {
            Debug.LogError("BoardView: gameManager reference not set.");
            return;
        }

        tileViews = new GameObject[gameManager.boardWidth, gameManager.boardHeight];
        
        // Spawn layout styling
        SpawnGridBackground();
        BuildInitialView();
        CreateWorldSpaceHUD();

        // Subscribe to GameManager's events
        gameManager.OnTilesSwapped += HandleTilesSwapped;
        gameManager.OnTilesSwapFailed += HandleTilesSwapFailed;
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
        gameManager.OnTilesSwapFailed -= HandleTilesSwapFailed;
        gameManager.OnMatchesFound -= HandleMatchesFound;
        gameManager.OnGravityApplied -= HandleGravityApplied;
        gameManager.OnTilesSpawned -= HandleTilesSpawned;
        gameManager.OnScoreChanged -= HandleScoreChanged;
        gameManager.OnBoardReset -= HandleBoardReset;

        if (worldHUDText != null)
        {
            gameManager.OnScoreChanged -= UpdateWorldHUD;
            gameManager.OnGoalsUpdated -= UpdateWorldHUD;
            gameManager.OnGravityDirectionChanged -= UpdateWorldHUD;
        }
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
        view.transform.localScale = Vector3.one * tileScale * (on ? 1.15f : 1f);
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

        // Swap instantly so visual matches target the correct objects during resolutions
        tileViews[x1, y1] = b;
        tileViews[x2, y2] = a;

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
    }

    private void HandleTilesSwapFailed(int x1, int y1, int x2, int y2)
    {
        StartCoroutine(AnimateFailedSwap(x1, y1, x2, y2));
    }

    private IEnumerator AnimateFailedSwap(int x1, int y1, int x2, int y2)
    {
        GameObject a = tileViews[x1, y1];
        GameObject b = tileViews[x2, y2];
        if (a == null || b == null) yield break;

        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;
        float t = 0f;

        // Slide towards each other
        while (t < swapAnimSeconds)
        {
            t += Time.deltaTime;
            float frac = t / swapAnimSeconds;
            a.transform.position = Vector3.Lerp(posA, posB, frac);
            b.transform.position = Vector3.Lerp(posB, posA, frac);
            yield return null;
        }

        // Slide back
        t = 0f;
        while (t < swapAnimSeconds)
        {
            t += Time.deltaTime;
            float frac = t / swapAnimSeconds;
            a.transform.position = Vector3.Lerp(posB, posA, frac);
            b.transform.position = Vector3.Lerp(posA, posB, frac);
            yield return null;
        }

        a.transform.position = posA;
        b.transform.position = posB;
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

        Vector3 baseScale = Vector3.one * tileScale;
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
        view.transform.localScale = Vector3.one * tileScale;

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
        UpdateWorldHUD();
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
        view.transform.localScale = Vector3.one * tileScale;

        TileData data = gameManager.GetTileDataById(tileId);
        SpriteRenderer renderer = view.GetComponent<SpriteRenderer>();
        if (renderer != null && data != null)
        {
            renderer.sprite = data.sprite;
            renderer.color = GetTileColor(data); // Apply color tinting
        }

        tileViews[x, y] = view;
    }

    private void SpawnGridBackground()
    {
        if (bgParent != null) Destroy(bgParent);
        bgParent = new GameObject("GridBackground");
        bgParent.transform.SetParent(transform);

        Sprite slotSprite = null;
        if (gameManager.availableTileTypes != null && gameManager.availableTileTypes.Length > 0)
        {
            slotSprite = gameManager.availableTileTypes[0].sprite;
        }

        for (int x = 0; x < gameManager.boardWidth; x++)
        {
            for (int y = 0; y < gameManager.boardHeight; y++)
            {
                GameObject slot = new GameObject($"SlotBackground_{x}_{y}");
                slot.transform.SetParent(bgParent.transform);
                slot.transform.position = CellToWorld(x, y);
                slot.transform.localScale = Vector3.one * cellSize * 0.95f;

                SpriteRenderer sr = slot.AddComponent<SpriteRenderer>();
                sr.sprite = slotSprite;
                sr.color = new Color(0.08f, 0.08f, 0.08f, 0.4f); // Dark translucent backing
                sr.sortingOrder = -2;
            }
        }
    }

    private void CreateWorldSpaceHUD()
    {
        GameObject hudGo = new GameObject("WorldSpaceHUD");
        hudGo.transform.SetParent(transform);

        // Place it directly to the right side of the board
        float hudX = boardOrigin.x + gameManager.boardWidth * cellSize + 0.6f;
        float hudY = boardOrigin.y + gameManager.boardHeight * cellSize - 1.0f;
        hudGo.transform.position = new Vector3(hudX, hudY, 0f);

        worldHUDText = hudGo.AddComponent<TMPro.TextMeshPro>();
        worldHUDText.fontSize = 5.2f;
        worldHUDText.rectTransform.sizeDelta = new Vector2(6f, 8f);
        worldHUDText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        worldHUDText.color = Color.white;

        // Hook game events to trigger HUD updates
        gameManager.OnScoreChanged += UpdateWorldHUD;
        gameManager.OnGoalsUpdated += UpdateWorldHUD;
        gameManager.OnGravityDirectionChanged += UpdateWorldHUD;

        UpdateWorldHUD();
    }

    private void UpdateWorldHUD(int dummy) => UpdateWorldHUD();
    private void UpdateWorldHUD(GravityDirection dummy) => UpdateWorldHUD();
    private void UpdateWorldHUD(List<TileGoal> dummy) => UpdateWorldHUD();

    private void UpdateWorldHUD()
    {
        if (worldHUDText == null || gameManager == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<color=#FFBB33><b>GRAVIMATCH</b></color>");
        sb.AppendLine("───────────────");
        sb.AppendLine($"<b>Moves Left:</b> <size=125%><color=#FFAA00>{gameManager.MovesLeft}</color></size>");
        sb.AppendLine($"<b>Gravity:</b> {gameManager.CurrentGravity.ToString().ToUpper()}");
        sb.AppendLine();
        sb.AppendLine("<b>Objectives:</b>");

        if (gameManager.levelGoals != null && gameManager.levelGoals.Count > 0)
        {
            foreach (var goal in gameManager.levelGoals)
            {
                if (goal.tileType == null) continue;
                string name = goal.tileType.tileId.ToUpper();
                int remaining = Mathf.Max(0, goal.targetCount - goal.currentCount);
                if (remaining == 0)
                {
                    sb.AppendLine($"<color=#44FF44>✔ {name}: CLEARED!</color>");
                }
                else
                {
                    string hexColor = ColorUtility.ToHtmlStringRGBA(GetTileColor(goal.tileType));
                    sb.AppendLine($"• <color=#{hexColor}>■ {name}</color>: {goal.currentCount}/{goal.targetCount}");
                }
            }
        }
        else
        {
            sb.AppendLine("No Active Goals");
        }

        sb.AppendLine();
        sb.AppendLine("───────────────");
        sb.AppendLine("<size=65%><color=#888888>WASD / Arrows\nto Shift Gravity\n\nClick adjacent\ntiles to swap</color></size>");

        worldHUDText.text = sb.ToString();
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
