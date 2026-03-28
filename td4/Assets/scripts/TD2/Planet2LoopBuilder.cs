using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Planet2LoopBuilder
{
    public sealed class Settings
    {
        public int gridWidth = 18;
        public int gridHeight = 18;
        public int minRectangleWidth = 5;
        public int maxRectangleWidth = 8;
        public int minRectangleHeight = 5;
        public int maxRectangleHeight = 8;
        public int expansionPasses = 18;
        public int maxOffset = 3;
        public int minStraightEdgeCount = 3;
        public int layoutAttempts = 8;
        public int targetTrackLength = 32;
        public float turnProbability = 0.75f;
        public bool useRandomSeed = true;
        public int seed;
    }

    public sealed class Result
    {
        public Result(List<Vector2Int> cells)
        {
            Cells = cells;
        }

        public List<Vector2Int> Cells { get; }
    }

    private readonly Settings settings;
    private readonly System.Random random;

    private readonly struct StraightRun
    {
        public StraightRun(int startCellIndex, int endCellIndex, Vector2Int direction)
        {
            StartCellIndex = startCellIndex;
            EndCellIndex = endCellIndex;
            Direction = direction;
        }

        public int StartCellIndex { get; }
        public int EndCellIndex { get; }
        public Vector2Int Direction { get; }
    }

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public Planet2LoopBuilder(Settings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        int seed = settings.useRandomSeed ? Environment.TickCount : settings.seed;
        random = new System.Random(seed);
    }

    public Result Generate()
    {
        int targetTrackLength = GetTargetTrackLength();
        float turnBias = Mathf.Clamp01(settings.turnProbability);
        List<Vector2Int> bestLoop = null;
        float bestScore = float.MinValue;

        for (int attempt = 0; attempt < Mathf.Max(1, settings.layoutAttempts); attempt++)
        {
            List<Vector2Int> candidate = CreateSeedLoop(targetTrackLength, turnBias);

            for (int pass = 0; pass < Mathf.Max(0, settings.expansionPasses) && candidate.Count < targetTrackLength; pass++)
            {
                if (random.NextDouble() <= turnBias)
                {
                    TryExpandLoop(candidate, targetTrackLength, turnBias);
                }
            }

            float candidateScore = ScoreLoop(candidate, targetTrackLength, turnBias);
            if (bestLoop == null || candidateScore > bestScore)
            {
                bestLoop = new List<Vector2Int>(candidate);
                bestScore = candidateScore;
            }
        }

        if (bestLoop == null || bestLoop.Count < 8)
        {
            bestLoop = CreateFallbackLoop(targetTrackLength);
        }

        bestLoop = CenterLoop(bestLoop);
        bestLoop = ApplyRandomTransform(bestLoop);
        bestLoop = RotateToBestStartingStraight(bestLoop);

        return new Result(bestLoop);
    }

    private int GetTargetTrackLength()
    {
        return Mathf.Max(12, settings.targetTrackLength);
    }

    private List<Vector2Int> CreateSeedLoop(int targetTrackLength, float turnBias)
    {
        int clampedMaxWidth = Mathf.Clamp(settings.maxRectangleWidth, 4, settings.gridWidth - 4);
        int clampedMaxHeight = Mathf.Clamp(settings.maxRectangleHeight, 4, settings.gridHeight - 4);
        int clampedMinWidth = Mathf.Clamp(settings.minRectangleWidth, 4, clampedMaxWidth);
        int clampedMinHeight = Mathf.Clamp(settings.minRectangleHeight, 4, clampedMaxHeight);

        int seedLengthCap = Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Lerp(targetTrackLength, targetTrackLength * 0.6f, turnBias)),
            12,
            targetTrackLength);

        List<Vector2Int> validSizes = BuildValidRectangleSizes(clampedMinWidth, clampedMaxWidth, clampedMinHeight, clampedMaxHeight, seedLengthCap);
        if (validSizes.Count == 0)
        {
            validSizes = BuildValidRectangleSizes(clampedMinWidth, clampedMaxWidth, clampedMinHeight, clampedMaxHeight, targetTrackLength);
        }

        Vector2Int selectedSize = validSizes.Count > 0 ? validSizes[random.Next(validSizes.Count)] : new Vector2Int(4, 4);
        int width = selectedSize.x;
        int height = selectedSize.y;

        int margin = Mathf.Max(2, GetMaximumOffset(turnBias) + 1);
        int minOriginX = margin;
        int maxOriginX = Mathf.Max(minOriginX, settings.gridWidth - width - margin);
        int minOriginY = margin;
        int maxOriginY = Mathf.Max(minOriginY, settings.gridHeight - height - margin);

        int originX = random.Next(minOriginX, maxOriginX + 1);
        int originY = random.Next(minOriginY, maxOriginY + 1);

        return BuildRectangleLoop(originX, originY, width, height);
    }

    private List<Vector2Int> CreateFallbackLoop(int targetTrackLength)
    {
        int clampedMaxWidth = Mathf.Clamp(settings.maxRectangleWidth, 4, settings.gridWidth - 4);
        int clampedMaxHeight = Mathf.Clamp(settings.maxRectangleHeight, 4, settings.gridHeight - 4);
        List<Vector2Int> validSizes = BuildValidRectangleSizes(4, clampedMaxWidth, 4, clampedMaxHeight, targetTrackLength);
        Vector2Int selectedSize = validSizes.Count > 0 ? validSizes[0] : new Vector2Int(4, 4);

        int width = selectedSize.x;
        int height = selectedSize.y;
        int originX = Mathf.Max(1, (settings.gridWidth - width) / 2);
        int originY = Mathf.Max(1, (settings.gridHeight - height) / 2);
        return BuildRectangleLoop(originX, originY, width, height);
    }

    private static List<Vector2Int> BuildValidRectangleSizes(int minWidth, int maxWidth, int minHeight, int maxHeight, int maximumLoopLength)
    {
        var validSizes = new List<Vector2Int>();

        for (int width = minWidth; width <= maxWidth; width++)
        {
            for (int height = minHeight; height <= maxHeight; height++)
            {
                int loopLength = CalculateRectangleLoopLength(width, height);
                if (loopLength <= maximumLoopLength)
                {
                    validSizes.Add(new Vector2Int(width, height));
                }
            }
        }

        validSizes.Sort((first, second) => CalculateRectangleLoopLength(second.x, second.y).CompareTo(CalculateRectangleLoopLength(first.x, first.y)));
        return validSizes;
    }

    private static int CalculateRectangleLoopLength(int width, int height)
    {
        return (width + height) * 2 - 4;
    }

    private static List<Vector2Int> BuildRectangleLoop(int originX, int originY, int width, int height)
    {
        var loop = new List<Vector2Int>((width + height) * 2 - 4);

        for (int x = originX; x < originX + width; x++)
        {
            loop.Add(new Vector2Int(x, originY));
        }

        for (int y = originY + 1; y < originY + height; y++)
        {
            loop.Add(new Vector2Int(originX + width - 1, y));
        }

        for (int x = originX + width - 2; x >= originX; x--)
        {
            loop.Add(new Vector2Int(x, originY + height - 1));
        }

        for (int y = originY + height - 2; y > originY; y--)
        {
            loop.Add(new Vector2Int(originX, y));
        }

        return loop;
    }

    private bool TryExpandLoop(List<Vector2Int> sourceLoop, int targetTrackLength, float turnBias)
    {
        List<Vector2Int> workingLoop = RotateLoop(sourceLoop, random.Next(sourceLoop.Count));
        List<StraightRun> runs = GetStraightRuns(workingLoop, GetMinimumStraightEdgeCount(turnBias));
        Shuffle(runs);

        foreach (StraightRun run in runs)
        {
            var perpendiculars = new List<Vector2Int>
            {
                PerpendicularLeft(run.Direction),
                PerpendicularRight(run.Direction)
            };

            Shuffle(perpendiculars);

            List<int> offsets = new List<int>();
            for (int offset = 1; offset <= GetMaximumOffset(turnBias); offset++)
            {
                offsets.Add(offset);
            }

            if (turnBias >= 0.5f)
            {
                offsets.Sort();
            }
            else
            {
                offsets.Sort((first, second) => second.CompareTo(first));
            }

            foreach (Vector2Int perpendicular in perpendiculars)
            {
                foreach (int offset in offsets)
                {
                    if (TryBuildExpandedLoop(workingLoop, run, perpendicular, offset, targetTrackLength, out List<Vector2Int> expandedLoop))
                    {
                        sourceLoop.Clear();
                        sourceLoop.AddRange(expandedLoop);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool TryBuildExpandedLoop(
        List<Vector2Int> sourceLoop,
        StraightRun run,
        Vector2Int perpendicular,
        int offset,
        int targetTrackLength,
        out List<Vector2Int> expandedLoop)
    {
        expandedLoop = null;

        List<Vector2Int> expandedSegment = BuildExpandedSegment(sourceLoop, run, perpendicular, offset);
        List<Vector2Int> candidate = ReplaceSpan(sourceLoop, run.StartCellIndex, run.EndCellIndex, expandedSegment);

        if (candidate.Count > targetTrackLength || !IsWithinBounds(candidate) || !IsSimpleLoop(candidate))
        {
            return false;
        }

        expandedLoop = candidate;
        return true;
    }

    private static List<Vector2Int> BuildExpandedSegment(
        List<Vector2Int> sourceLoop,
        StraightRun run,
        Vector2Int perpendicular,
        int offset)
    {
        Vector2Int start = sourceLoop[run.StartCellIndex];
        Vector2Int end = sourceLoop[run.EndCellIndex];
        int cellCount = (run.EndCellIndex - run.StartCellIndex) + 1;
        Vector2Int startShift = start + perpendicular * offset;

        var segment = new List<Vector2Int>();
        segment.Add(start);

        for (int step = 1; step <= offset; step++)
        {
            segment.Add(start + perpendicular * step);
        }

        for (int travel = 1; travel < cellCount; travel++)
        {
            segment.Add(startShift + run.Direction * travel);
        }

        for (int step = offset - 1; step >= 1; step--)
        {
            segment.Add(end + perpendicular * step);
        }

        segment.Add(end);
        return segment;
    }

    private static List<Vector2Int> ReplaceSpan(
        List<Vector2Int> sourceLoop,
        int startCellIndex,
        int endCellIndex,
        List<Vector2Int> replacement)
    {
        var result = new List<Vector2Int>(sourceLoop.Count - (endCellIndex - startCellIndex + 1) + replacement.Count);

        for (int i = 0; i < startCellIndex; i++)
        {
            result.Add(sourceLoop[i]);
        }

        result.AddRange(replacement);

        for (int i = endCellIndex + 1; i < sourceLoop.Count; i++)
        {
            result.Add(sourceLoop[i]);
        }

        return result;
    }

    private int GetMaximumOffset(float turnBias)
    {
        return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(settings.maxOffset, 1f, turnBias)));
    }

    private int GetMinimumStraightEdgeCount(float turnBias)
    {
        return Mathf.Max(2, Mathf.RoundToInt(Mathf.Lerp(settings.minStraightEdgeCount + 1, 2f, turnBias)));
    }

    private float ScoreLoop(List<Vector2Int> loop, int targetTrackLength, float turnBias)
    {
        int lengthError = Mathf.Abs(targetTrackLength - loop.Count);
        int cornerCount = CountCorners(loop);
        GetBounds(loop, out int minX, out int maxX, out int minY, out int maxY);
        int footprintArea = (maxX - minX + 1) * (maxY - minY + 1);

        float lengthScore = -lengthError * 5f;
        float turnScore = cornerCount * Mathf.Lerp(1.5f, 4f, turnBias);
        float footprintScore = -footprintArea * 0.15f;
        return lengthScore + turnScore + footprintScore;
    }

    private static int CountCorners(List<Vector2Int> loop)
    {
        int corners = 0;

        for (int i = 0; i < loop.Count; i++)
        {
            Vector2Int previous = loop[(i - 1 + loop.Count) % loop.Count];
            Vector2Int current = loop[i];
            Vector2Int next = loop[(i + 1) % loop.Count];

            Vector2Int incoming = current - previous;
            Vector2Int outgoing = next - current;

            if (incoming != outgoing)
            {
                corners++;
            }
        }

        return corners;
    }

    private List<StraightRun> GetStraightRuns(List<Vector2Int> loop, int minimumStraightEdgeCount)
    {
        var runs = new List<StraightRun>();

        if (loop.Count < 4)
        {
            return runs;
        }

        int edgeIndex = 0;
        while (edgeIndex < loop.Count - 1)
        {
            Vector2Int direction = loop[edgeIndex + 1] - loop[edgeIndex];
            int startCellIndex = edgeIndex;
            int straightEdgeCount = 1;

            while (edgeIndex + straightEdgeCount < loop.Count - 1)
            {
                Vector2Int nextDirection = loop[edgeIndex + straightEdgeCount + 1] - loop[edgeIndex + straightEdgeCount];
                if (nextDirection != direction)
                {
                    break;
                }

                straightEdgeCount++;
            }

            if (straightEdgeCount >= Mathf.Max(2, minimumStraightEdgeCount))
            {
                runs.Add(new StraightRun(startCellIndex, startCellIndex + straightEdgeCount, direction));
            }

            edgeIndex += straightEdgeCount;
        }

        return runs;
    }

    private bool IsWithinBounds(List<Vector2Int> loop)
    {
        for (int i = 0; i < loop.Count; i++)
        {
            Vector2Int cell = loop[i];
            if (cell.x < 0 || cell.x >= settings.gridWidth || cell.y < 0 || cell.y >= settings.gridHeight)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSimpleLoop(List<Vector2Int> loop)
    {
        if (loop.Count < 8)
        {
            return false;
        }

        var occupied = new HashSet<Vector2Int>();
        for (int i = 0; i < loop.Count; i++)
        {
            if (!occupied.Add(loop[i]))
            {
                return false;
            }
        }

        for (int i = 0; i < loop.Count; i++)
        {
            Vector2Int current = loop[i];
            Vector2Int next = loop[(i + 1) % loop.Count];
            if (Mathf.Abs(current.x - next.x) + Mathf.Abs(current.y - next.y) != 1)
            {
                return false;
            }
        }

        foreach (Vector2Int cell in loop)
        {
            int neighbouringTrackCells = 0;
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                if (occupied.Contains(cell + CardinalDirections[i]))
                {
                    neighbouringTrackCells++;
                }
            }

            if (neighbouringTrackCells != 2)
            {
                return false;
            }
        }

        return true;
    }

    private List<Vector2Int> CenterLoop(List<Vector2Int> loop)
    {
        GetBounds(loop, out int minX, out int maxX, out int minY, out int maxY);
        int offsetX = Mathf.RoundToInt((minX + maxX) * 0.5f);
        int offsetY = Mathf.RoundToInt((minY + maxY) * 0.5f);

        var centered = new List<Vector2Int>(loop.Count);
        for (int i = 0; i < loop.Count; i++)
        {
            centered.Add(new Vector2Int(loop[i].x - offsetX, loop[i].y - offsetY));
        }

        return centered;
    }

    private List<Vector2Int> ApplyRandomTransform(List<Vector2Int> loop)
    {
        bool mirror = random.NextDouble() > 0.5d;
        int quarterTurns = random.Next(0, 4);

        var transformed = new List<Vector2Int>(loop.Count);
        for (int i = 0; i < loop.Count; i++)
        {
            Vector2Int cell = loop[i];

            if (mirror)
            {
                cell = new Vector2Int(-cell.x, cell.y);
            }

            for (int turn = 0; turn < quarterTurns; turn++)
            {
                cell = new Vector2Int(-cell.y, cell.x);
            }

            transformed.Add(cell);
        }

        return transformed;
    }

    private static List<Vector2Int> RotateToBestStartingStraight(List<Vector2Int> loop)
    {
        int bestIndex = 0;
        int bestRunLength = -1;

        for (int startEdge = 0; startEdge < loop.Count; startEdge++)
        {
            Vector2Int direction = loop[(startEdge + 1) % loop.Count] - loop[startEdge];
            int runLength = 1;

            while (runLength < loop.Count - 1)
            {
                int currentIndex = (startEdge + runLength) % loop.Count;
                int nextIndex = (startEdge + runLength + 1) % loop.Count;
                Vector2Int nextDirection = loop[nextIndex] - loop[currentIndex];
                if (nextDirection != direction)
                {
                    break;
                }

                runLength++;
            }

            if (runLength > bestRunLength)
            {
                bestRunLength = runLength;
                bestIndex = (startEdge + (runLength / 2)) % loop.Count;
            }
        }

        return RotateLoop(loop, bestIndex);
    }

    private static List<Vector2Int> RotateLoop(List<Vector2Int> loop, int startIndex)
    {
        var rotated = new List<Vector2Int>(loop.Count);
        for (int i = 0; i < loop.Count; i++)
        {
            rotated.Add(loop[(startIndex + i) % loop.Count]);
        }

        return rotated;
    }

    private static void GetBounds(List<Vector2Int> loop, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = int.MaxValue;
        maxX = int.MinValue;
        minY = int.MaxValue;
        maxY = int.MinValue;

        for (int i = 0; i < loop.Count; i++)
        {
            Vector2Int cell = loop[i];
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);
        }
    }

    private void Shuffle<T>(List<T> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
        }
    }

    private static Vector2Int PerpendicularLeft(Vector2Int direction)
    {
        return new Vector2Int(-direction.y, direction.x);
    }

    private static Vector2Int PerpendicularRight(Vector2Int direction)
    {
        return new Vector2Int(direction.y, -direction.x);
    }
}
