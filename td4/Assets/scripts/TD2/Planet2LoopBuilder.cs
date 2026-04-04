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
        public int maxConsecutiveStraights = 1;
        public float alternateTurnProbability = 0.8f;
        public int minStraightsBetweenTurns = 0;
        public float tileSize = 10f;
        public float targetRadius = 0f;
        public float radiusTolerance = 20f;
        public float exclusionRadius = 25f;
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

    private enum TurnSense
    {
        Left,
        Right
    }

    private readonly struct TurnChoice
    {
        public TurnChoice(Vector2Int perpendicular, TurnSense turnSense)
        {
            Perpendicular = perpendicular;
            TurnSense = turnSense;
        }

        public Vector2Int Perpendicular { get; }
        public TurnSense TurnSense { get; }
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
            TurnSense? lastTurnSense = null;

            for (int pass = 0; pass < Mathf.Max(0, settings.expansionPasses) && candidate.Count < targetTrackLength; pass++)
            {
                if (random.NextDouble() <= turnBias)
                {
                    TryExpandLoop(candidate, targetTrackLength, turnBias, ref lastTurnSense);
                }
            }

            ApplySerpentineRules(candidate, targetTrackLength, turnBias, ref lastTurnSense);

            if (ViolatesExclusionZone(candidate))
            {
                continue;
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

        validSizes = FilterValidSizesByExclusion(validSizes);

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
        validSizes = FilterValidSizesByExclusion(validSizes, false);

        if (validSizes.Count == 0 && HasExclusionZone())
        {
            validSizes = FilterValidSizesByExclusion(
                BuildValidRectangleSizes(4, clampedMaxWidth, 4, clampedMaxHeight, int.MaxValue),
                false);
        }

        Vector2Int selectedSize = validSizes.Count > 0 ? validSizes[0] : new Vector2Int(clampedMaxWidth, clampedMaxHeight);

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

    private bool TryExpandLoop(List<Vector2Int> sourceLoop, int targetTrackLength, float turnBias, ref TurnSense? lastTurnSense)
    {
        List<Vector2Int> workingLoop = RotateLoop(sourceLoop, random.Next(sourceLoop.Count));
        List<StraightRun> runs = GetStraightRuns(workingLoop, GetMinimumStraightEdgeCount(turnBias));
        Shuffle(runs);

        foreach (StraightRun run in runs)
        {
                if (TryExpandRun(workingLoop, run, targetTrackLength, turnBias, ref lastTurnSense, out List<Vector2Int> expandedLoop))
                {
                    sourceLoop.Clear();
                    sourceLoop.AddRange(expandedLoop);
                    return true;
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

        if (candidate.Count > targetTrackLength || !IsWithinBounds(candidate) || ViolatesExclusionZone(candidate) || !IsSimpleLoop(candidate))
        {
            return false;
        }

        expandedLoop = candidate;
        return true;
    }

    private void ApplySerpentineRules(List<Vector2Int> sourceLoop, int targetTrackLength, float turnBias, ref TurnSense? lastTurnSense)
    {
        int safetyPassCount = Mathf.Max(4, settings.expansionPasses * 3);

        for (int pass = 0; pass < safetyPassCount; pass++)
        {
            StraightRun? violatingRun = FindLongestViolatingStraightRun(sourceLoop);
            if (!violatingRun.HasValue)
            {
                break;
            }

            if (!TryBreakStraightRun(sourceLoop, violatingRun.Value, targetTrackLength, turnBias, ref lastTurnSense))
            {
                break;
            }
        }
    }

    private StraightRun? FindLongestViolatingStraightRun(List<Vector2Int> loop)
    {
        List<StraightRun> runs = GetStraightRuns(loop, 2);
        StraightRun? longestRun = null;
        int longestStraightCount = -1;

        foreach (StraightRun run in runs)
        {
            int consecutiveStraightCount = GetConsecutiveStraightCount(run);
            if (consecutiveStraightCount <= GetMaxConsecutiveStraights())
            {
                continue;
            }

            if (consecutiveStraightCount > longestStraightCount)
            {
                longestStraightCount = consecutiveStraightCount;
                longestRun = run;
            }
        }

        return longestRun;
    }

    private bool TryBreakStraightRun(List<Vector2Int> sourceLoop, StraightRun fullRun, int targetTrackLength, float turnBias, ref TurnSense? lastTurnSense)
    {
        int subrunCellCount = Mathf.Max(2, GetMaxConsecutiveStraights() + 2);
        int fullRunCellCount = fullRun.EndCellIndex - fullRun.StartCellIndex + 1;
        if (fullRunCellCount < subrunCellCount)
        {
            return false;
        }

        List<int> candidateStartIndices = BuildSubrunStartIndices(fullRun.StartCellIndex, fullRun.EndCellIndex, subrunCellCount);

        foreach (int startIndex in candidateStartIndices)
        {
            var subrun = new StraightRun(startIndex, startIndex + subrunCellCount - 1, fullRun.Direction);
            if (TryExpandRun(sourceLoop, subrun, targetTrackLength, turnBias, ref lastTurnSense, out List<Vector2Int> expandedLoop))
            {
                sourceLoop.Clear();
                sourceLoop.AddRange(expandedLoop);
                return true;
            }
        }

        return false;
    }

    private bool TryExpandRun(
        List<Vector2Int> sourceLoop,
        StraightRun run,
        int targetTrackLength,
        float turnBias,
        ref TurnSense? lastTurnSense,
        out List<Vector2Int> expandedLoop)
    {
        expandedLoop = null;

        List<TurnChoice> turnChoices = BuildPreferredTurnChoices(sourceLoop, run, lastTurnSense);
        List<int> offsets = BuildOffsetOrder(turnBias);

        foreach (TurnChoice turnChoice in turnChoices)
        {
            foreach (int offset in offsets)
            {
                if (TryBuildExpandedLoop(sourceLoop, run, turnChoice.Perpendicular, offset, targetTrackLength, out expandedLoop))
                {
                    lastTurnSense = turnChoice.TurnSense;
                    return true;
                }
            }
        }

        return false;
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
        int biasedMaximumOffset = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(settings.maxOffset, 1f, turnBias)));
        return Mathf.Max(GetMinimumOffset(), biasedMaximumOffset);
    }

    private int GetMinimumStraightEdgeCount(float turnBias)
    {
        return Mathf.Max(2, Mathf.RoundToInt(Mathf.Lerp(settings.minStraightEdgeCount + 1, 2f, turnBias)));
    }

    private int GetMinimumOffset()
    {
        return Mathf.Max(1, settings.minStraightsBetweenTurns + 1);
    }

    private int GetMaxConsecutiveStraights()
    {
        return Mathf.Max(0, settings.maxConsecutiveStraights);
    }

    private static int GetConsecutiveStraightCount(StraightRun run)
    {
        return Mathf.Max(0, (run.EndCellIndex - run.StartCellIndex + 1) - 2);
    }

    private List<int> BuildOffsetOrder(float turnBias)
    {
        int minimumOffset = GetMinimumOffset();
        int maximumOffset = GetMaximumOffset(turnBias);
        var offsets = new List<int>(maximumOffset - minimumOffset + 1);

        for (int offset = minimumOffset; offset <= maximumOffset; offset++)
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

        return offsets;
    }

    private List<TurnChoice> BuildPreferredTurnChoices(List<Vector2Int> sourceLoop, StraightRun run, TurnSense? lastTurnSense)
    {
        TurnSense preferredTurnSense = ChoosePreferredTurnSense(lastTurnSense);
        if (TryGetOrbitPreferredTurnSense(sourceLoop, run, out TurnSense orbitPreferredTurnSense))
        {
            preferredTurnSense = orbitPreferredTurnSense;
        }

        TurnSense fallbackTurnSense = preferredTurnSense == TurnSense.Left ? TurnSense.Right : TurnSense.Left;

        return new List<TurnChoice>
        {
            CreateTurnChoice(run.Direction, preferredTurnSense),
            CreateTurnChoice(run.Direction, fallbackTurnSense)
        };
    }

    private List<Vector2Int> FilterValidSizesByExclusion(List<Vector2Int> validSizes, bool allowOriginalFallback = true)
    {
        if (!HasExclusionZone() || validSizes == null || validSizes.Count == 0)
        {
            return validSizes;
        }

        var filteredSizes = new List<Vector2Int>(validSizes.Count);
        for (int i = 0; i < validSizes.Count; i++)
        {
            Vector2Int size = validSizes[i];
            List<Vector2Int> rectangleLoop = BuildRectangleLoop(0, 0, size.x, size.y);
            if (!ViolatesExclusionZone(rectangleLoop))
            {
                filteredSizes.Add(size);
            }
        }

        if (filteredSizes.Count > 0)
        {
            return filteredSizes;
        }

        return allowOriginalFallback ? validSizes : filteredSizes;
    }

    private TurnSense ChoosePreferredTurnSense(TurnSense? lastTurnSense)
    {
        if (!lastTurnSense.HasValue)
        {
            return random.NextDouble() < 0.5d ? TurnSense.Left : TurnSense.Right;
        }

        if (random.NextDouble() < Mathf.Clamp01(settings.alternateTurnProbability))
        {
            return lastTurnSense.Value == TurnSense.Left ? TurnSense.Right : TurnSense.Left;
        }

        return lastTurnSense.Value;
    }

    private static TurnChoice CreateTurnChoice(Vector2Int travelDirection, TurnSense turnSense)
    {
        return turnSense == TurnSense.Left
            ? new TurnChoice(PerpendicularLeft(travelDirection), TurnSense.Left)
            : new TurnChoice(PerpendicularRight(travelDirection), TurnSense.Right);
    }

    private static List<int> BuildSubrunStartIndices(int runStartIndex, int runEndIndex, int subrunCellCount)
    {
        int minimumStartIndex = runStartIndex;
        int maximumStartIndex = runEndIndex - subrunCellCount + 1;
        var startIndices = new List<int>();

        if (maximumStartIndex < minimumStartIndex)
        {
            return startIndices;
        }

        int centeredStartIndex = (minimumStartIndex + maximumStartIndex) / 2;
        startIndices.Add(centeredStartIndex);

        for (int offset = 1; startIndices.Count < (maximumStartIndex - minimumStartIndex + 1); offset++)
        {
            int leftIndex = centeredStartIndex - offset;
            if (leftIndex >= minimumStartIndex)
            {
                startIndices.Add(leftIndex);
            }

            int rightIndex = centeredStartIndex + offset;
            if (rightIndex <= maximumStartIndex)
            {
                startIndices.Add(rightIndex);
            }
        }

        return startIndices;
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
        float straightPenalty = -GetExcessStraightPenalty(loop) * Mathf.Lerp(4f, 10f, turnBias);
        float turnAlternationScore = ScoreTurnAlternation(loop) * Mathf.Lerp(0.5f, 2f, Mathf.Clamp01(settings.alternateTurnProbability));
        float orbitScore = ScoreOrbitPreference(loop);
        return lengthScore + turnScore + footprintScore + straightPenalty + turnAlternationScore + orbitScore;
    }

    private float ScoreOrbitPreference(List<Vector2Int> loop)
    {
        if (!HasOrbitPreference() || loop == null || loop.Count == 0)
        {
            return 0f;
        }

        Vector2 loopCenter = GetCurrentLoopCenter(loop);
        float targetRadiusWorld = Mathf.Max(0f, settings.targetRadius);
        float radiusToleranceWorld = Mathf.Max(0f, settings.radiusTolerance);
        float minimumRadius = Mathf.Max(0f, targetRadiusWorld - radiusToleranceWorld);
        float maximumRadius = targetRadiusWorld + radiusToleranceWorld;
        float totalOutsideDistance = 0f;
        float totalTargetDeviation = 0f;
        int cellsInsideBand = 0;

        for (int i = 0; i < loop.Count; i++)
        {
            Vector2 centeredCell = (Vector2)loop[i] - loopCenter;
            float radiusWorld = centeredCell.magnitude * Mathf.Max(0.1f, settings.tileSize);

            if (radiusWorld < minimumRadius)
            {
                totalOutsideDistance += minimumRadius - radiusWorld;
            }
            else if (radiusWorld > maximumRadius)
            {
                totalOutsideDistance += radiusWorld - maximumRadius;
            }
            else
            {
                cellsInsideBand++;
            }

            totalTargetDeviation += Mathf.Abs(radiusWorld - targetRadiusWorld);
        }

        float averageOutsideDistance = totalOutsideDistance / loop.Count;
        float averageTargetDeviation = totalTargetDeviation / loop.Count;
        float insideBandRatio = (float)cellsInsideBand / loop.Count;

        return (insideBandRatio * 25f) - (averageOutsideDistance * 2f) - (averageTargetDeviation * 0.25f);
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

    private float GetExcessStraightPenalty(List<Vector2Int> loop)
    {
        float excessStraightPenalty = 0f;
        List<StraightRun> runs = GetStraightRuns(loop, 2);

        foreach (StraightRun run in runs)
        {
            int excessStraights = Mathf.Max(0, GetConsecutiveStraightCount(run) - GetMaxConsecutiveStraights());
            excessStraightPenalty += excessStraights;
        }

        return excessStraightPenalty;
    }

    private float ScoreTurnAlternation(List<Vector2Int> loop)
    {
        List<TurnSense> turnSequence = GetTurnSequence(loop);
        if (turnSequence.Count < 2)
        {
            return 0f;
        }

        float alternationScore = 0f;
        for (int i = 1; i < turnSequence.Count; i++)
        {
            alternationScore += turnSequence[i] != turnSequence[i - 1] ? 1f : -0.5f;
        }

        return alternationScore;
    }

    private static List<TurnSense> GetTurnSequence(List<Vector2Int> loop)
    {
        var turnSequence = new List<TurnSense>();

        for (int i = 0; i < loop.Count; i++)
        {
            Vector2Int previous = loop[(i - 1 + loop.Count) % loop.Count];
            Vector2Int current = loop[i];
            Vector2Int next = loop[(i + 1) % loop.Count];

            Vector2Int incoming = current - previous;
            Vector2Int outgoing = next - current;
            if (incoming == outgoing)
            {
                continue;
            }

            int crossProduct = (incoming.x * outgoing.y) - (incoming.y * outgoing.x);
            turnSequence.Add(crossProduct > 0 ? TurnSense.Left : TurnSense.Right);
        }

        return turnSequence;
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

    private bool HasExclusionZone()
    {
        return settings.exclusionRadius > 0f && settings.tileSize > 0f;
    }

    private bool ViolatesExclusionZone(List<Vector2Int> loop)
    {
        if (!HasExclusionZone() || loop == null || loop.Count == 0)
        {
            return false;
        }

        GetBounds(loop, out int minX, out int maxX, out int minY, out int maxY);
        int offsetX = Mathf.RoundToInt((minX + maxX) * 0.5f);
        int offsetY = Mathf.RoundToInt((minY + maxY) * 0.5f);
        float tileSize = Mathf.Max(0.1f, settings.tileSize);
        float exclusionRadiusSqr = settings.exclusionRadius * settings.exclusionRadius;

        for (int i = 0; i < loop.Count; i++)
        {
            Vector2 centeredWorldPosition = new Vector2(
                (loop[i].x - offsetX) * tileSize,
                (loop[i].y - offsetY) * tileSize);

            if (centeredWorldPosition.sqrMagnitude < exclusionRadiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasOrbitPreference()
    {
        return settings.targetRadius > 0f && settings.tileSize > 0f;
    }

    private bool TryGetOrbitPreferredTurnSense(List<Vector2Int> loop, StraightRun run, out TurnSense preferredTurnSense)
    {
        preferredTurnSense = TurnSense.Left;

        if (!HasOrbitPreference() || loop == null || loop.Count == 0)
        {
            return false;
        }

        Vector2 loopCenter = GetCurrentLoopCenter(loop);
        Vector2 startCell = loop[run.StartCellIndex];
        Vector2 endCell = loop[run.EndCellIndex];
        Vector2 samplePoint = (startCell + endCell) * 0.5f;
        Vector2 radialFromCenter = samplePoint - loopCenter;

        if (radialFromCenter.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        float sampleRadiusWorld = radialFromCenter.magnitude * Mathf.Max(0.1f, settings.tileSize);
        float minimumRadius = Mathf.Max(0f, settings.targetRadius - Mathf.Max(0f, settings.radiusTolerance));
        float maximumRadius = settings.targetRadius + Mathf.Max(0f, settings.radiusTolerance);

        if (sampleRadiusWorld >= minimumRadius && sampleRadiusWorld <= maximumRadius)
        {
            return false;
        }

        Vector2 desiredDirection = sampleRadiusWorld > maximumRadius
            ? -radialFromCenter.normalized
            : radialFromCenter.normalized;

        Vector2 leftDirection = PerpendicularLeft(run.Direction);
        Vector2 rightDirection = PerpendicularRight(run.Direction);
        float leftAlignment = Vector2.Dot(leftDirection, desiredDirection);
        float rightAlignment = Vector2.Dot(rightDirection, desiredDirection);

        if (Mathf.Approximately(leftAlignment, rightAlignment))
        {
            return false;
        }

        preferredTurnSense = leftAlignment > rightAlignment ? TurnSense.Left : TurnSense.Right;
        return true;
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

    private static Vector2 GetCurrentLoopCenter(List<Vector2Int> loop)
    {
        GetBounds(loop, out int minX, out int maxX, out int minY, out int maxY);
        return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
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
