using UnityEngine;

/// <summary>
/// Defines a single tile TYPE as a data asset, separate from any board or
/// gameplay logic. This mirrors the CharacterData pattern from my Last Decision
/// project: each "case" (there, a character; here, a tile) is an authored
/// ScriptableObject that game logic reads from, rather than being hardcoded
/// into a manager class.
///
/// Practical benefit: a designer can create a new tile type (new sprite, new
/// special behavior, new score value) by right-clicking in the Project window
/// and filling in fields -- no code changes, no recompiling, no merge conflicts
/// in a shared GameManager script.
/// </summary>
[CreateAssetMenu(fileName = "NewTile", menuName = "Match3/Tile Data")]
public class TileData : ScriptableObject
{
    [Header("Identity")]
    public string tileId;          // stable key, e.g. "red", "blue", "bomb"
    public Sprite sprite;
    public Color tileColor = Color.white;

    [Header("Special Behavior")]
    public TileSpecialType specialType = TileSpecialType.None;

    [Header("Scoring")]
    public int baseScoreValue = 10;

    public enum TileSpecialType
    {
        None,
        RowClear,
        ColumnClear,
        ColorBomb
    }
}
