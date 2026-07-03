using UnityEngine;

[CreateAssetMenu(fileName = "NewTile", menuName = "Match3/Tile Data")]
public class TileData : ScriptableObject
{
    [Header("Identity")]
    public string tileId;          
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
