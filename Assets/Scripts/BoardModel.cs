using System.Collections.Generic;


public struct BoardCoord
{
    public readonly int X;
    public readonly int Y;

    public BoardCoord(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object obj)
    {
        return obj is BoardCoord other && other.X == X && other.Y == Y;
    }

    public override int GetHashCode()
    {
        return X * 397 ^ Y;
    }
}


public struct TileMove
{
    public readonly int FromX;
    public readonly int FromY;
    public readonly int ToX;
    public readonly int ToY;

    public TileMove(int fromX, int fromY, int toX, int toY)
    {
        FromX = fromX;
        FromY = fromY;
        ToX = toX;
        ToY = toY;
    }
}


public struct TileSpawnMove
{
    public readonly int SpawnX;
    public readonly int SpawnY;
    public readonly int DestX;
    public readonly int DestY;
    public readonly string TileId;

    public TileSpawnMove(int spawnX, int spawnY, int destX, int destY, string tileId)
    {
        SpawnX = spawnX;
        SpawnY = spawnY;
        DestX = destX;
        DestY = destY;
        TileId = tileId;
    }
}


/// Pure C# representation of the board grid. Deliberately has ZERO dependency
/// on MonoBehaviour, UnityEngine.UI, or any Unity lifecycle methods.
///
/// Why this matters: in my Last Decision jam project, GameManager.cs ended up
/// owning UI references, audio playback, fade coroutines, AND game-state logic
/// in a single 750-line MonoBehaviour. It shipped on time and the bug-tracking
/// logic inside it was solid, but it was hard to unit test or reuse outside
/// the scene it was wired into. BoardModel is my fix for that specific problem:
/// by keeping the grid data and match-finding logic in plain C#, this class can
/// be tested with a console app or NUnit, with no Unity editor required at all.

public class BoardModel
{
    public readonly int Width;
    public readonly int Height;

    private readonly string[,] grid;

    public BoardModel(int width, int height)
    {
        Width = width;
        Height = height;
        grid = new string[width, height];
    }

    public string GetTile(int x, int y)
    {
        if (!InBounds(x, y)) return null;
        return grid[x, y];
    }

    public void SetTile(int x, int y, string tileId)
    {
        if (!InBounds(x, y)) return;
        grid[x, y] = tileId;
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }


    public bool SwapTiles(int x1, int y1, int x2, int y2)
    {
        if (!InBounds(x1, y1) || !InBounds(x2, y2)) return false;

        bool adjacent = (System.Math.Abs(x1 - x2) == 1 && y1 == y2) ||
                         (System.Math.Abs(y1 - y2) == 1 && x1 == x2);
        if (!adjacent) return false;

        string temp = grid[x1, y1];
        grid[x1, y1] = grid[x2, y2];
        grid[x2, y2] = temp;
        return true;
    }


    public void ClearTiles(IEnumerable<BoardCoord> coords)
    {
        foreach (var coord in coords)
        {
            if (InBounds(coord.X, coord.Y)) grid[coord.X, coord.Y] = null;
        }
    }

  
    public List<TileMove> ApplyGravity(GravityDirection direction)
    {
        var moves = new List<TileMove>();

        if (direction == GravityDirection.Down)
        {
            for (int x = 0; x < Width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (grid[x, y] == null) continue;

                    if (writeY != y)
                    {
                        grid[x, writeY] = grid[x, y];
                        grid[x, y] = null;
                        moves.Add(new TileMove(x, y, x, writeY));
                    }
                    writeY++;
                }
            }
        }
        else if (direction == GravityDirection.Up)
        {
            for (int x = 0; x < Width; x++)
            {
                int writeY = Height - 1;
                for (int y = Height - 1; y >= 0; y--)
                {
                    if (grid[x, y] == null) continue;

                    if (writeY != y)
                    {
                        grid[x, writeY] = grid[x, y];
                        grid[x, y] = null;
                        moves.Add(new TileMove(x, y, x, writeY));
                    }
                    writeY--;
                }
            }
        }
        else if (direction == GravityDirection.Left)
        {
            for (int y = 0; y < Height; y++)
            {
                int writeX = 0;
                for (int x = 0; x < Width; x++)
                {
                    if (grid[x, y] == null) continue;

                    if (writeX != x)
                    {
                        grid[writeX, y] = grid[x, y];
                        grid[x, y] = null;
                        moves.Add(new TileMove(x, y, writeX, y));
                    }
                    writeX++;
                }
            }
        }
        else if (direction == GravityDirection.Right)
        {
            for (int y = 0; y < Height; y++)
            {
                int writeX = Width - 1;
                for (int x = Width - 1; x >= 0; x--)
                {
                    if (grid[x, y] == null) continue;

                    if (writeX != x)
                    {
                        grid[writeX, y] = grid[x, y];
                        grid[x, y] = null;
                        moves.Add(new TileMove(x, y, writeX, y));
                    }
                    writeX--;
                }
            }
        }

        return moves;
    }

   

    public List<TileSpawnMove> FillEmptyCellsAndGetMoves(GravityDirection direction, System.Func<string> randomTileGenerator)
    {
        var spawnMoves = new List<TileSpawnMove>();

        if (direction == GravityDirection.Down)
        {
         
            for (int x = 0; x < Width; x++)
            {
                int emptyCount = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (grid[x, y] == null)
                    {
                        string tileId = randomTileGenerator();
                        grid[x, y] = tileId;
                        int spawnX = x;
                        int spawnY = Height + emptyCount;
                        spawnMoves.Add(new TileSpawnMove(spawnX, spawnY, x, y, tileId));
                        emptyCount++;
                    }
                }
            }
        }
        else if (direction == GravityDirection.Up)
        {
      
            for (int x = 0; x < Width; x++)
            {
                int emptyCount = 0;
                for (int y = Height - 1; y >= 0; y--)
                {
                    if (grid[x, y] == null)
                    {
                        string tileId = randomTileGenerator();
                        grid[x, y] = tileId;
                        int spawnX = x;
                        int spawnY = -1 - emptyCount;
                        spawnMoves.Add(new TileSpawnMove(spawnX, spawnY, x, y, tileId));
                        emptyCount++;
                    }
                }
            }
        }
        else if (direction == GravityDirection.Left)
        {
          
            for (int y = 0; y < Height; y++)
            {
                int emptyCount = 0;
                for (int x = 0; x < Width; x++)
                {
                    if (grid[x, y] == null)
                    {
                        string tileId = randomTileGenerator();
                        grid[x, y] = tileId;
                        int spawnX = Width + emptyCount;
                        int spawnY = y;
                        spawnMoves.Add(new TileSpawnMove(spawnX, spawnY, x, y, tileId));
                        emptyCount++;
                    }
                }
            }
        }
        else if (direction == GravityDirection.Right)
        {
      
            for (int y = 0; y < Height; y++)
            {
                int emptyCount = 0;
                for (int x = Width - 1; x >= 0; x--)
                {
                    if (grid[x, y] == null)
                    {
                        string tileId = randomTileGenerator();
                        grid[x, y] = tileId;
                        int spawnX = -1 - emptyCount;
                        int spawnY = y;
                        spawnMoves.Add(new TileSpawnMove(spawnX, spawnY, x, y, tileId));
                        emptyCount++;
                    }
                }
            }
        }

        return spawnMoves;
    }

    public bool HasEmptyCells()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (grid[x, y] == null) return true;
        return false;
    }
}
