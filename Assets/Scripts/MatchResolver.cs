using System.Collections.Generic;
using System.Linq;


public static class MatchResolver
{
    private const int MinMatchLength = 3;

 
    public static List<List<BoardCoord>> FindMatches(BoardModel board)
    {
        var matches = new List<List<BoardCoord>>();
        matches.AddRange(FindRuns(board, horizontal: true));
        matches.AddRange(FindRuns(board, horizontal: false));
        return MergeOverlappingMatches(matches);
    }

    private static List<List<BoardCoord>> FindRuns(BoardModel board, bool horizontal)
    {
        var runs = new List<List<BoardCoord>>();
        int outerLimit = horizontal ? board.Height : board.Width;
        int innerLimit = horizontal ? board.Width : board.Height;

        for (int outer = 0; outer < outerLimit; outer++)
        {
            int runStart = 0;
            string runTileId = null;

            for (int inner = 0; inner <= innerLimit; inner++)
            {
                string tileId = inner < innerLimit
                    ? GetTile(board, horizontal, outer, inner)
                    : null; // sentinel to flush the final run

                bool continuesRun = tileId != null && tileId == runTileId;

                if (!continuesRun)
                {
                    int runLength = inner - runStart;
                    if (runLength >= MinMatchLength)
                    {
                        var run = new List<BoardCoord>();
                        for (int k = runStart; k < inner; k++)
                            run.Add(ToCoord(horizontal, outer, k));
                        runs.Add(run);
                    }
                    runStart = inner;
                    runTileId = tileId;
                }
            }
        }

        return runs;
    }

    private static string GetTile(BoardModel board, bool horizontal, int outer, int inner)
    {
        return horizontal ? board.GetTile(inner, outer) : board.GetTile(outer, inner);
    }

    private static BoardCoord ToCoord(bool horizontal, int outer, int inner)
    {
        return horizontal ? new BoardCoord(inner, outer) : new BoardCoord(outer, inner);
    }

    
    private static List<List<BoardCoord>> MergeOverlappingMatches(List<List<BoardCoord>> matches)
    {
        var merged = new List<HashSet<BoardCoord>>();

        foreach (var match in matches)
        {
            var asSet = new HashSet<BoardCoord>(match);
            var overlapping = merged.Where(m => m.Overlaps(asSet)).ToList();

            if (overlapping.Count == 0)
            {
                merged.Add(asSet);
            }
            else
            {
                foreach (var group in overlapping)
                {
                    asSet.UnionWith(group);
                    merged.Remove(group);
                }
                merged.Add(asSet);
            }
        }

        return merged.Select(s => s.ToList()).ToList();
    }
}
