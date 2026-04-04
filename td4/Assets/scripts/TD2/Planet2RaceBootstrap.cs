using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Planet2RaceBootstrap : MonoBehaviour
{
    private const string Planet2SceneName = "Planet_02";
    private const string GeneratedTrackRootName = "Planet2GeneratedTrack";
    private const string LegacyTrackRootName = "TrackTD2Resource";

    [Header("Track Prefabs")]
    [SerializeField] private GameObject straightPrefab;
    [SerializeField] private GameObject cornerPrefab;
    [SerializeField] private GameObject startFinishDecorationPrefab;

    [Header("Props")]
    [SerializeField] private GameObject streetLampPrefab;
    [Min(1)]
    [SerializeField] private int lampSpawnInterval = 3;
    [SerializeField] private float lampVerticalOffset = 0.5f;
    [SerializeField] private float lampDepthOffset = 0.35f;
    [SerializeField] private float lampRotationCorrection = 90f;

    [Header("Track Layout")]
    [Min(12)]
    [SerializeField] private int targetTrackLength = 32;
    [Range(0f, 1f)]
    [SerializeField] private float turnProbability = 0.75f;
    [Min(0)]
    [SerializeField] private int maxConsecutiveStraights = 1;
    [Range(0f, 1f)]
    [SerializeField] private float alternateTurnProbability = 0.8f;
    [Min(0)]
    [SerializeField] private int minStraightsBetweenTurns = 0;

    [Header("Track Scaling")]
    [Min(0.1f)]
    [SerializeField] private float tileSize = 10f;
    [Min(0.5f)]
    [SerializeField] private float trackScale = 2.5f;

    [Header("Flyover")]
    [Min(1f)]
    [SerializeField] private float flyoverHeight = 40f;
    [Min(0f)]
    [SerializeField] private float flyoverPadding = 20f;

    [Header("Race Setup")]
    [Min(0f)]
    [SerializeField] private float startHeight = 0.85f;
    [Min(3)]
    [SerializeField] private int checkpointCount = 4;
    [Header("AI Setup")]
    [Min(1f)]
    [SerializeField] private float aiStartRowSpacing = 6f;
    [Min(0f)]
    [SerializeField] private float aiStartSideSpacing = 3f;
    [Tooltip("Optional quarter-turn offset for straight prefabs. Use 90 degree increments if their default forward axis is different.")]
    [SerializeField] private float straightRotationOffsetDegrees;
    [Tooltip("Use 90 degree increments if the corner prefab faces the wrong way after you assign it.")]
    [SerializeField] private float cornerRotationOffsetDegrees;

    public void TryPrepare(GameManager gameManager)
    {
        if (gameManager == null || SceneManager.GetActiveScene().name != Planet2SceneName)
        {
            return;
        }

        PlayerControl playerControl = FindPlayerControl();
        if (playerControl == null)
        {
            Debug.LogError("Planet2RaceBootstrap could not find a PlayerControl in Planet_02.");
            return;
        }

        if (!HasRequiredPrefabs())
        {
            Debug.LogError("Planet2RaceBootstrap needs both Straight Prefab and Corner Prefab assigned before Planet_02 can generate the runtime track.");
            return;
        }

        DisableLegacyTrack();
        ClearLegacyCheckpoints(gameManager.lapTracker);
        DestroyExistingGeneratedTrack();

        var loopBuilder = new Planet2LoopBuilder(
            new Planet2LoopBuilder.Settings
            {
                targetTrackLength = targetTrackLength,
                turnProbability = turnProbability,
                maxConsecutiveStraights = maxConsecutiveStraights,
                alternateTurnProbability = alternateTurnProbability,
                minStraightsBetweenTurns = minStraightsBetweenTurns
            });

        Planet2LoopBuilder.Result layout = loopBuilder.Generate();

        var generator = new Planet2ProceduralTrackGenerator(
            layout,
            new Planet2ProceduralTrackGenerator.Settings
            {
                straightPrefab = straightPrefab,
                cornerPrefab = cornerPrefab,
                startFinishDecorationPrefab = startFinishDecorationPrefab,
                streetLampPrefab = streetLampPrefab,
                tileSize = tileSize,
                trackScale = trackScale,
                lampSpawnInterval = lampSpawnInterval,
                lampVerticalOffset = lampVerticalOffset,
                lampDepthOffset = lampDepthOffset,
                lampRotationCorrection = lampRotationCorrection,
                startHeight = startHeight,
                checkpointCount = checkpointCount,
                straightRotationOffsetDegrees = straightRotationOffsetDegrees,
                cornerRotationOffsetDegrees = cornerRotationOffsetDegrees,
                flyoverHeight = flyoverHeight,
                flyoverPadding = flyoverPadding
            });

        Planet2ProceduralTrackGenerator.BuildResult buildResult = generator.Generate();

        if (buildResult.Root != null)
        {
            buildResult.Root.name = GeneratedTrackRootName;
        }

        if (gameManager.lapTracker != null)
        {
            gameManager.lapTracker.checkpoints = buildResult.Checkpoints;
        }

        PositionPlayer(playerControl, buildResult.StartPosition, buildResult.StartRotation);
        PrepareAiCars(gameManager, buildResult);
        Physics.SyncTransforms();
        WireSceneReferences(gameManager, playerControl);
        bool flyoverConfigured = ConfigureProceduralFlyover(gameManager, buildResult);
        if (!flyoverConfigured)
        {
            SnapCameraToPlayer(gameManager, playerControl);
        }

        EnsureWinScreenController(gameManager);
    }

    private bool HasRequiredPrefabs()
    {
        return straightPrefab != null && cornerPrefab != null;
    }

    private static PlayerControl FindPlayerControl()
    {
        PlayerControl[] playerControls = UnityEngine.Object.FindObjectsByType<PlayerControl>(FindObjectsSortMode.None);
        return playerControls.Length > 0 ? playerControls[0] : null;
    }

    private static void DisableLegacyTrack()
    {
        GameObject legacyTrackRoot = GameObject.Find(LegacyTrackRootName);
        if (legacyTrackRoot != null)
        {
            legacyTrackRoot.SetActive(false);
        }
    }

    private void PrepareAiCars(GameManager gameManager, Planet2ProceduralTrackGenerator.BuildResult buildResult)
    {
        AIControls[] aiCars = UnityEngine.Object.FindObjectsByType<AIControls>(FindObjectsSortMode.None);
        if (aiCars == null || aiCars.Length == 0)
        {
            gameManager.aiControls = Array.Empty<AIControls>();
            return;
        }

        var configuredAiCars = new List<AIControls>(aiCars.Length);

        for (int i = 0; i < aiCars.Length; i++)
        {
            AIControls aiCar = aiCars[i];
            if (aiCar == null)
            {
                continue;
            }

            aiCar.gameObject.SetActive(true);
            aiCar.SetWaypointsHolder(buildResult.AIWaypointsHolder);
            PositionAiCar(aiCar, buildResult.StartPosition, buildResult.StartRotation, i, aiCars.Length);
            aiCar.enabled = false;
            configuredAiCars.Add(aiCar);
        }

        gameManager.aiControls = configuredAiCars.ToArray();
    }

    private void PositionAiCar(AIControls aiCar, Vector3 startPosition, Quaternion startRotation, int aiIndex, int totalAiCars)
    {
        Transform aiTransform = aiCar.transform;
        Vector3 startForward = startRotation * Vector3.forward;
        Vector3 startRight = startRotation * Vector3.right;

        int rowIndex = (aiIndex / 2) + 1;
        bool isSoloCar = totalAiCars <= 1;
        float sideDirection = aiIndex % 2 == 0 ? -1f : 1f;
        float sideOffset = isSoloCar ? 0f : sideDirection * aiStartSideSpacing;
        Vector3 aiPosition = startPosition - (startForward * (rowIndex * aiStartRowSpacing)) + (startRight * sideOffset);
        aiPosition.y = startPosition.y;

        aiTransform.SetPositionAndRotation(aiPosition, startRotation);

        Rigidbody rigidbody = aiCar.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.position = aiPosition;
            rigidbody.rotation = startRotation;
            rigidbody.Sleep();
        }

        if (aiTransform.CompareTag("Player"))
        {
            aiTransform.tag = "ai";
        }
    }

    private static void DisableAiCars(GameManager gameManager)
    {
        AIControls[] aiCars = UnityEngine.Object.FindObjectsByType<AIControls>(FindObjectsSortMode.None);
        foreach (AIControls aiCar in aiCars)
        {
            if (aiCar != null)
            {
                aiCar.gameObject.SetActive(false);
            }
        }

        gameManager.aiControls = Array.Empty<AIControls>();
    }

    private static void ClearLegacyCheckpoints(LapManager lapManager)
    {
        if (lapManager == null || lapManager.checkpoints == null)
        {
            return;
        }

        var preservedCheckpoints = new List<Checkpoint>();

        foreach (Checkpoint checkpoint in lapManager.checkpoints)
        {
            if (checkpoint != null)
            {
                if (checkpoint.transform.root != null && checkpoint.transform.root.name == GeneratedTrackRootName)
                {
                    preservedCheckpoints.Add(checkpoint);
                    continue;
                }

                UnityEngine.Object.Destroy(checkpoint.gameObject);
            }
        }

        lapManager.checkpoints = preservedCheckpoints;
    }

    private static void DestroyExistingGeneratedTrack()
    {
        GameObject generatedTrack = GameObject.Find(GeneratedTrackRootName);
        if (generatedTrack != null)
        {
            UnityEngine.Object.Destroy(generatedTrack);
        }
    }

    private static void PositionPlayer(PlayerControl playerControl, Vector3 startPosition, Quaternion startRotation)
    {
        playerControl.gameObject.tag = "Player";
        playerControl.transform.SetPositionAndRotation(startPosition, startRotation);

        Rigidbody rigidbody = playerControl.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.position = startPosition;
            rigidbody.rotation = startRotation;
            rigidbody.Sleep();
        }
    }

    private static void WireSceneReferences(GameManager gameManager, PlayerControl playerControl)
    {
        gameManager.playerControls = playerControl;

        followPlayer followCamera = gameManager.followPlayerCamera as followPlayer;
        if (followCamera == null)
        {
            followPlayer[] followCameras = UnityEngine.Object.FindObjectsByType<followPlayer>(FindObjectsSortMode.None);
            if (followCameras.Length > 0)
            {
                followCamera = followCameras[0];
                gameManager.followPlayerCamera = followCamera;
            }
        }

        if (followCamera != null)
        {
            followCamera.player = playerControl.transform;
        }
    }

    private static void SnapCameraToPlayer(GameManager gameManager, PlayerControl playerControl)
    {
        followPlayer followCamera = gameManager.followPlayerCamera as followPlayer;
        if (followCamera == null)
        {
            followPlayer[] followCameras = UnityEngine.Object.FindObjectsByType<followPlayer>(FindObjectsSortMode.None);
            if (followCameras.Length > 0)
            {
                followCamera = followCameras[0];
                gameManager.followPlayerCamera = followCamera;
            }
        }

        if (followCamera != null)
        {
            followCamera.player = playerControl.transform;
            followCamera.SnapToTarget();
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, playerControl.transform.eulerAngles.y, 0f);
            mainCamera.transform.SetPositionAndRotation(
                playerControl.transform.position + (yawRotation * new Vector3(0f, 8f, -12f)),
                yawRotation * Quaternion.Euler(35f, 0f, 0f));
        }
    }

    private static bool ConfigureProceduralFlyover(GameManager gameManager, Planet2ProceduralTrackGenerator.BuildResult buildResult)
    {
        if (buildResult == null || buildResult.FlyoverWaypoints == null || buildResult.FlyoverWaypoints.Count < 2)
        {
            return false;
        }

        ProceduralFlyoverCamera flyoverCamera = FindFirstObjectByType<ProceduralFlyoverCamera>();
        if (flyoverCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }

            flyoverCamera = mainCamera.GetComponent<ProceduralFlyoverCamera>();
            if (flyoverCamera == null)
            {
                flyoverCamera = mainCamera.gameObject.AddComponent<ProceduralFlyoverCamera>();
            }
        }

        flyoverCamera.Configure(buildResult.FlyoverWaypoints, buildResult.FlyoverLookTarget);
        return true;
    }

    private static void EnsureWinScreenController(GameManager gameManager)
    {
        Planet2WinScreenController winScreenController = gameManager.GetComponent<Planet2WinScreenController>();
        if (winScreenController == null)
        {
            winScreenController = gameManager.gameObject.AddComponent<Planet2WinScreenController>();
        }

        winScreenController.Initialize(gameManager);
    }
}
