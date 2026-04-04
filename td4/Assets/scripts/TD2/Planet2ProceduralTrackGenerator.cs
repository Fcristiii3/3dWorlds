using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class Planet2ProceduralTrackGenerator
{
    private enum TrackPieceKind
    {
        Straight,
        Corner
    }

    private sealed class TrackPieceData
    {
        private readonly List<Vector2Int> connections = new List<Vector2Int>(2);

        public TrackPieceData(Vector2Int coordinate)
        {
            Coordinate = coordinate;
        }

        public Vector2Int Coordinate { get; }
        public IReadOnlyList<Vector2Int> Connections => connections;
        public TrackPieceKind PieceKind { get; set; }
        public float Yaw { get; set; }

        public void AddConnection(Vector2Int direction)
        {
            if (!connections.Contains(direction))
            {
                connections.Add(direction);
            }
        }
    }

    public sealed class Settings
    {
        public GameObject straightPrefab;
        public GameObject cornerPrefab;
        public GameObject startFinishDecorationPrefab;
        public GameObject streetLampPrefab;
        public float tileSize = 10f;
        public float trackScale = 2.5f;
        public int lampSpawnInterval = 3;
        public float lampVerticalOffset = 0.5f;
        public float lampDepthOffset = 0.35f;
        public float lampRotationCorrection = 90f;
        public float startHeight = 0.85f;
        public int checkpointCount = 4;
        public float straightRotationOffsetDegrees;
        public float cornerRotationOffsetDegrees;
        public float checkpointHeight = 10f;
        public float checkpointThickness = 2f;
        public Vector3 worldCenterOffset = Vector3.zero;
        public float flyoverHeight = 40f;
        public float flyoverPadding = 20f;
    }

    public sealed class BuildResult
    {
        public BuildResult(
            GameObject root,
            List<Checkpoint> checkpoints,
            Transform aiWaypointsHolder,
            List<Vector3> flyoverWaypoints,
            Vector3 flyoverLookTarget,
            Vector3 startPosition,
            Quaternion startRotation)
        {
            Root = root;
            Checkpoints = checkpoints;
            AIWaypointsHolder = aiWaypointsHolder;
            FlyoverWaypoints = flyoverWaypoints;
            FlyoverLookTarget = flyoverLookTarget;
            StartPosition = startPosition;
            StartRotation = startRotation;
        }

        public GameObject Root { get; }
        public List<Checkpoint> Checkpoints { get; }
        public Transform AIWaypointsHolder { get; }
        public List<Vector3> FlyoverWaypoints { get; }
        public Vector3 FlyoverLookTarget { get; }
        public Vector3 StartPosition { get; }
        public Quaternion StartRotation { get; }
    }

    private readonly Planet2LoopBuilder.Result layout;
    private readonly Settings settings;
    private readonly float cellSize;
    private readonly float appliedTrackScale;

    public Planet2ProceduralTrackGenerator(Planet2LoopBuilder.Result layout, Settings settings)
    {
        this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
        this.settings = settings ?? new Settings();

        if (this.settings.straightPrefab == null)
        {
            throw new ArgumentNullException(nameof(this.settings.straightPrefab), "A straight prefab is required to generate the Planet 2 track.");
        }

        if (this.settings.cornerPrefab == null)
        {
            throw new ArgumentNullException(nameof(this.settings.cornerPrefab), "A corner prefab is required to generate the Planet 2 track.");
        }

        cellSize = Mathf.Max(0.1f, this.settings.tileSize);
        appliedTrackScale = Mathf.Max(0.5f, this.settings.trackScale);
    }

    public BuildResult Generate()
    {
        GameObject root = new GameObject("Planet2GeneratedTrack");
        GameObject pieceRoot = new GameObject("Pieces");
        GameObject triggerRoot = new GameObject("Checkpoints");
        GameObject aiWaypointRoot = new GameObject("AIWaypoints");
        GameObject decorationRoot = new GameObject("Decorations");

        pieceRoot.transform.SetParent(root.transform, false);
        triggerRoot.transform.SetParent(root.transform, false);
        aiWaypointRoot.transform.SetParent(root.transform, false);
        decorationRoot.transform.SetParent(root.transform, false);

        Dictionary<Vector2Int, TrackPieceData> trackMap = BuildTrackMap();
        List<TrackPieceData> orderedPieces = CreateOrderedPieces(trackMap);
        var spawnedPiecePositions = new List<Vector3>(orderedPieces.Count);
        int lampInterval = Mathf.Max(1, settings.lampSpawnInterval);
        bool spawnLampOnLeftSide = true;

        for (int i = 0; i < orderedPieces.Count; i++)
        {
            TrackPieceData pieceData = orderedPieces[i];
            GameObject spawnedPiece = SpawnMappedPiece(pieceRoot.transform, pieceData, i);
            spawnedPiecePositions.Add(spawnedPiece.transform.position);

            if ((i + 1) % lampInterval == 0 && pieceData.PieceKind == TrackPieceKind.Straight)
            {
                if (SpawnStreetLamp(spawnedPiece.transform, spawnLampOnLeftSide))
                {
                    spawnLampOnLeftSide = !spawnLampOnLeftSide;
                }
            }
        }

        List<Checkpoint> checkpoints = CreateCheckpoints(triggerRoot.transform);
        Transform aiWaypointsHolder = CreateAIWaypoints(aiWaypointRoot.transform);
        List<Vector3> flyoverWaypoints = CreateFlyoverWaypoints(spawnedPiecePositions, out Vector3 flyoverLookTarget);

        Vector3 startForward = GridToWorldDirection(GetForwardDirection(0));
        SpawnStartFinishDecoration(decorationRoot.transform, layout.Cells[0], startForward);
        Vector3 startPosition = GridToWorld(layout.Cells[0]) - startForward * (cellSize * 0.35f);
        startPosition.y = settings.startHeight;
        Quaternion startRotation = Quaternion.LookRotation(startForward, Vector3.up);

        return new BuildResult(root, checkpoints, aiWaypointsHolder, flyoverWaypoints, flyoverLookTarget, startPosition, startRotation);
    }

    private List<TrackPieceData> CreateOrderedPieces(Dictionary<Vector2Int, TrackPieceData> trackMap)
    {
        var orderedPieces = new List<TrackPieceData>(layout.Cells.Count);

        for (int i = 0; i < layout.Cells.Count; i++)
        {
            Vector2Int cell = layout.Cells[i];
            if (!trackMap.TryGetValue(cell, out TrackPieceData pieceData))
            {
                throw new InvalidOperationException($"Planet2ProceduralTrackGenerator could not resolve mapped data for loop cell {cell}.");
            }

            orderedPieces.Add(pieceData);
        }

        return orderedPieces;
    }

    private Dictionary<Vector2Int, TrackPieceData> BuildTrackMap()
    {
        var trackMap = new Dictionary<Vector2Int, TrackPieceData>(layout.Cells.Count);

        for (int i = 0; i < layout.Cells.Count; i++)
        {
            Vector2Int currentCell = layout.Cells[i];
            Vector2Int nextCell = layout.Cells[(i + 1) % layout.Cells.Count];
            Vector2Int direction = nextCell - currentCell;

            if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
            {
                throw new InvalidOperationException($"Planet2ProceduralTrackGenerator received a non-adjacent loop segment between {currentCell} and {nextCell}.");
            }

            AddConnection(trackMap, currentCell, direction);
            AddConnection(trackMap, nextCell, -direction);
        }

        ResolveTrackPieces(trackMap);
        return trackMap;
    }

    private static void AddConnection(Dictionary<Vector2Int, TrackPieceData> trackMap, Vector2Int coordinate, Vector2Int direction)
    {
        if (!trackMap.TryGetValue(coordinate, out TrackPieceData pieceData))
        {
            pieceData = new TrackPieceData(coordinate);
            trackMap.Add(coordinate, pieceData);
        }

        pieceData.AddConnection(direction);
    }

    private void ResolveTrackPieces(Dictionary<Vector2Int, TrackPieceData> trackMap)
    {
        foreach (TrackPieceData pieceData in trackMap.Values)
        {
            if (pieceData.Connections.Count != 2)
            {
                throw new InvalidOperationException(
                    $"Planet2ProceduralTrackGenerator expected exactly 2 connections at {pieceData.Coordinate}, but found {pieceData.Connections.Count}.");
            }

            Vector2Int firstDirection = pieceData.Connections[0];
            Vector2Int secondDirection = pieceData.Connections[1];

            if (AreOpposite(firstDirection, secondDirection))
            {
                pieceData.PieceKind = TrackPieceKind.Straight;
                pieceData.Yaw = GetStraightYaw(firstDirection, secondDirection);
                continue;
            }

            pieceData.PieceKind = TrackPieceKind.Corner;
            pieceData.Yaw = GetCornerYaw(firstDirection, secondDirection);
        }
    }

    private static bool AreOpposite(Vector2Int firstDirection, Vector2Int secondDirection)
    {
        return firstDirection == -secondDirection;
    }

    private GameObject SpawnMappedPiece(Transform parent, TrackPieceData pieceData, int index)
    {
        GameObject prefab = pieceData.PieceKind == TrackPieceKind.Corner ? settings.cornerPrefab : settings.straightPrefab;
        string prefix = pieceData.PieceKind == TrackPieceKind.Corner ? "Corner" : "Straight";
        return SpawnTrackPiece(prefab, parent, $"{prefix}_{index}_{pieceData.Coordinate.x}_{pieceData.Coordinate.y}", pieceData.Coordinate, pieceData.Yaw);
    }

    private GameObject SpawnTrackPiece(GameObject prefab, Transform parent, string name, Vector2Int cell, float yaw)
    {
        GameObject piece = Object.Instantiate(prefab, parent);
        piece.name = name;

        Transform pieceTransform = piece.transform;
        pieceTransform.localScale = Vector3.Scale(prefab.transform.localScale, Vector3.one * appliedTrackScale);
        pieceTransform.SetPositionAndRotation(GridToWorld(cell), Quaternion.Euler(0f, yaw, 0f));

        EnsureColliders(piece);
        return piece;
    }

    private void SpawnStartFinishDecoration(Transform parent, Vector2Int startCell, Vector3 startForward)
    {
        if (settings.startFinishDecorationPrefab == null)
        {
            return;
        }

        GameObject decoration = Object.Instantiate(settings.startFinishDecorationPrefab, parent);
        decoration.name = "StartFinishDecoration";
        decoration.transform.SetPositionAndRotation(
            GridToWorld(startCell),
            Quaternion.LookRotation(startForward, Vector3.up));

        if (decoration.GetComponent<StartFinishLightAutoWire>() == null)
        {
            decoration.AddComponent<StartFinishLightAutoWire>();
        }
    }

    private bool SpawnStreetLamp(Transform trackPieceTransform, bool placeOnLeftSide)
    {
        if (settings.streetLampPrefab == null || trackPieceTransform == null)
        {
            return false;
        }

        string wallName = placeOnLeftSide ? "Wall_Left" : "Wall_Right";
        Transform wallTransform = FindChildRecursive(trackPieceTransform, wallName);
        if (wallTransform == null)
        {
            Debug.LogWarning($"Planet2ProceduralTrackGenerator could not find '{wallName}' under '{trackPieceTransform.name}'.");
            return false;
        }

        GameObject lamp = Object.Instantiate(settings.streetLampPrefab, trackPieceTransform, false);
        lamp.name = placeOnLeftSide ? "StreetLamp_Left" : "StreetLamp_Right";
        Transform lampTransform = lamp.transform;
        lampTransform.localScale = GetUnscaledChildLocalScale(settings.streetLampPrefab.transform.localScale, trackPieceTransform.lossyScale);

        Vector3 wallLocalPosition = trackPieceTransform.InverseTransformPoint(wallTransform.position);
        Vector3 towardTrackCenterLocal = new Vector3(-wallLocalPosition.x, 0f, -wallLocalPosition.z);
        if (towardTrackCenterLocal.sqrMagnitude < 0.0001f)
        {
            towardTrackCenterLocal = placeOnLeftSide ? Vector3.right : Vector3.left;
        }

        Vector3 outwardLocal = -towardTrackCenterLocal.normalized;
        lampTransform.localPosition = wallLocalPosition
            + (Vector3.up * settings.lampVerticalOffset)
            + (outwardLocal * settings.lampDepthOffset);

        Quaternion lookAtCenterRotation = Quaternion.LookRotation(towardTrackCenterLocal.normalized, Vector3.up);
        lampTransform.localRotation = lookAtCenterRotation * Quaternion.Euler(0f, settings.lampRotationCorrection, 0f);
        return true;
    }

    private List<Checkpoint> CreateCheckpoints(Transform parent)
    {
        int loopCellCount = layout.Cells.Count;
        var checkpoints = new List<Checkpoint>(loopCellCount);
        float checkpointHeight = Mathf.Max(1f, settings.checkpointHeight);
        float checkpointThickness = Mathf.Max(1f, settings.checkpointThickness);

        for (int sequenceIndex = 1; sequenceIndex <= loopCellCount; sequenceIndex++)
        {
            int currentCellIndex = sequenceIndex % loopCellCount;
            int previousCellIndex = (currentCellIndex - 1 + loopCellCount) % loopCellCount;
            Vector2Int previousCell = layout.Cells[previousCellIndex];
            Vector2Int currentCell = layout.Cells[currentCellIndex];
            Vector2Int travelDirection = currentCell - previousCell;
            Vector3 forward = GridToWorldDirection(travelDirection);
            bool isStartFinish = currentCellIndex == 0;
            Vector3 triggerPosition = isStartFinish
                ? GridToWorld(currentCell)
                : (GridToWorld(previousCell) + GridToWorld(currentCell)) * 0.5f;
            triggerPosition.y = checkpointHeight * 0.5f;

            GameObject checkpointObject = new GameObject(isStartFinish ? "StartFinishCheckpoint" : $"Checkpoint_{sequenceIndex}");
            checkpointObject.transform.SetParent(parent, false);
            checkpointObject.transform.position = triggerPosition;
            checkpointObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            BoxCollider collider = checkpointObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(cellSize, checkpointHeight, checkpointThickness);
            if (isStartFinish)
            {
                // Place the trigger so the approach-side face sits on the visual checkerboard line.
                collider.center = Vector3.forward * (checkpointThickness * 0.5f);
            }

            Checkpoint checkpoint = checkpointObject.AddComponent<Checkpoint>();
            checkpoints.Add(checkpoint);
        }

        return checkpoints;
    }

    private Transform CreateAIWaypoints(Transform parent)
    {
        int loopCellCount = layout.Cells.Count;
        float waypointHeight = settings.startHeight;

        for (int i = 1; i <= loopCellCount; i++)
        {
            int cellIndex = i % loopCellCount;
            Vector2Int cell = layout.Cells[cellIndex];
            Vector3 waypointPosition = GridToWorld(cell);
            waypointPosition.y = waypointHeight;

            GameObject waypoint = new GameObject($"Waypoint_{i:000}");
            waypoint.transform.SetParent(parent, false);
            waypoint.transform.position = waypointPosition;
        }

        return parent;
    }

    private List<Vector3> CreateFlyoverWaypoints(IReadOnlyList<Vector3> piecePositions, out Vector3 trackCenter)
    {
        float flyoverHeight = Mathf.Max(1f, settings.flyoverHeight);
        float flyoverPadding = Mathf.Max(0f, settings.flyoverPadding);
        float halfTile = cellSize * 0.5f;
        Vector3 fallbackCenter = GridToWorld(layout.Cells[0]);

        if (piecePositions == null || piecePositions.Count == 0)
        {
            trackCenter = fallbackCenter;
            return BuildSquareWaypoints(
                fallbackCenter.x - halfTile - flyoverPadding,
                fallbackCenter.x + halfTile + flyoverPadding,
                fallbackCenter.z - halfTile - flyoverPadding,
                fallbackCenter.z + halfTile + flyoverPadding,
                flyoverHeight,
                fallbackCenter);
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        for (int i = 0; i < piecePositions.Count; i++)
        {
            Vector3 position = piecePositions[i];
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minZ = Mathf.Min(minZ, position.z);
            maxZ = Mathf.Max(maxZ, position.z);
        }

        minX -= halfTile + flyoverPadding;
        maxX += halfTile + flyoverPadding;
        minZ -= halfTile + flyoverPadding;
        maxZ += halfTile + flyoverPadding;

        trackCenter = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
        return BuildSquareWaypoints(minX, maxX, minZ, maxZ, flyoverHeight, GridToWorld(layout.Cells[0]));
    }

    private static List<Vector3> BuildSquareWaypoints(float minX, float maxX, float minZ, float maxZ, float height, Vector3 startReference)
    {
        var corners = new List<Vector3>
        {
            new Vector3(minX, height, minZ),
            new Vector3(maxX, height, minZ),
            new Vector3(maxX, height, maxZ),
            new Vector3(minX, height, maxZ)
        };

        int closestCornerIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < corners.Count; i++)
        {
            float distance = (corners[i] - new Vector3(startReference.x, height, startReference.z)).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCornerIndex = i;
            }
        }

        int startIndex = (closestCornerIndex + 1) % corners.Count;
        var orderedWaypoints = new List<Vector3>(corners.Count);
        for (int i = 0; i < corners.Count; i++)
        {
            orderedWaypoints.Add(corners[(startIndex + i) % corners.Count]);
        }

        return orderedWaypoints;
    }

    private Vector2Int GetForwardDirection(int cellIndex)
    {
        Vector2Int current = layout.Cells[cellIndex];
        Vector2Int next = layout.Cells[(cellIndex + 1) % layout.Cells.Count];
        return next - current;
    }

    private Vector3 GridToWorld(Vector2Int cell)
    {
        return settings.worldCenterOffset + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
    }

    private Vector3 GridToWorldDirection(Vector2Int direction)
    {
        return new Vector3(direction.x, 0f, direction.y).normalized;
    }

    private float GetStraightYaw(Vector2Int firstDirection, Vector2Int secondDirection)
    {
        float baseYaw = ContainsDirections(firstDirection, secondDirection, Vector2Int.up, Vector2Int.down)
            ? 0f
            : 90f;

        return ApplyQuarterTurnOffset(baseYaw, settings.straightRotationOffsetDegrees);
    }

    private float GetCornerYaw(Vector2Int firstDirection, Vector2Int secondDirection)
    {
        float baseYaw;

        if (ContainsDirections(firstDirection, secondDirection, Vector2Int.up, Vector2Int.right))
        {
            baseYaw = 0f;
        }
        else if (ContainsDirections(firstDirection, secondDirection, Vector2Int.right, Vector2Int.down))
        {
            baseYaw = 90f;
        }
        else if (ContainsDirections(firstDirection, secondDirection, Vector2Int.down, Vector2Int.left))
        {
            baseYaw = 180f;
        }
        else
        {
            baseYaw = 270f;
        }

        return ApplyQuarterTurnOffset(baseYaw, settings.cornerRotationOffsetDegrees);
    }

    private static bool ContainsDirections(Vector2Int first, Vector2Int second, Vector2Int expectedA, Vector2Int expectedB)
    {
        return (first == expectedA && second == expectedB) || (first == expectedB && second == expectedA);
    }

    private static float ApplyQuarterTurnOffset(float baseYaw, float offsetDegrees)
    {
        int quarterTurnOffset = Mathf.RoundToInt(offsetDegrees / 90f);
        return Mathf.Repeat(baseYaw + (quarterTurnOffset * 90f), 360f);
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        Transform directMatch = parent.Find(childName);
        if (directMatch != null)
        {
            return directMatch;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform nestedMatch = FindChildRecursive(child, childName);
            if (nestedMatch != null)
            {
                return nestedMatch;
            }
        }

        return null;
    }

    private static float DivideByScale(float value, float scale)
    {
        return Mathf.Approximately(scale, 0f) ? value : value / scale;
    }

    private static Vector3 GetUnscaledChildLocalScale(Vector3 prefabLocalScale, Vector3 parentLossyScale)
    {
        return new Vector3(
            DivideByScale(prefabLocalScale.x, parentLossyScale.x),
            DivideByScale(prefabLocalScale.y, parentLossyScale.y),
            DivideByScale(prefabLocalScale.z, parentLossyScale.z));
    }

    private static void EnsureColliders(GameObject piece)
    {
        if (piece.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        MeshFilter[] meshFilters = piece.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
        }
    }
}
