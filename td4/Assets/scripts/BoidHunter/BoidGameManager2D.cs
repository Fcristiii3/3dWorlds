using System.Collections.Generic;
using System.IO;
using Unity.InferenceEngine;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public class BoidGameManager2D : MonoBehaviour
{
    [Header("World Bounds")]
    public Vector2 worldMin = new Vector2(-24f, -13f);
    public Vector2 worldMax = new Vector2(24f, 13f);

    [Header("Boid Spawn")]
    public int startingBoidCount = 60;
    public GameObject boidPrefab;
    public BoidAgent2D.BoidControlMode boidControlMode = BoidAgent2D.BoidControlMode.ClassicHeuristic;

    [Header("Training")]
    public bool trainingMode;
    public float matchDuration = 60f;
    public float timeRemaining;

    [Header("Inference")]
    public ModelAsset boidBrain;
    public ModelAsset hunterBrain;

    [Header("Hunter")]
    public HunterController2D hunter;
    public bool hunterAutoControl;

    [Header("Game Modes")]
    public static bool applyHunterBuffs = false;

    [Header("UI")]
    public string teamName;
    public Text scoreText;
    public Text timerText;
    public GameObject gameOverPanel;

    private readonly List<BoidAgent2D> boids = new List<BoidAgent2D>();
    private readonly List<BoidAgent2D> graveyard = new List<BoidAgent2D>();
    public List<BoidAgent2D> Boids => boids;
    public Vector3 HunterPosition => hunter != null ? hunter.transform.position : Vector3.zero;
    public bool IsGameRunning { get; private set; }
    public int EatenCount => eatenCount;
    public int RemainingCount => boids.Count;

    private int eatenCount;

    private void Start()
    {
        Application.runInBackground = true;

        if (trainingMode)
        {
            hunterAutoControl = false;
        }

        if (hunter != null)
        {
            hunter.manager = this;
            hunter.useAutoControl = hunterAutoControl;
            hunter.tag = "Hunter";

            if (applyHunterBuffs)
            {
                hunter.eatRadius = 2.2f;      
                hunter.viewRadius = 20f;      
            }
        }

        if (scoreText == null)
        {
            EnsureUIIsReady();
        }

        DrawArenaBorder();

        StartGame();
    }

    public void StartGame()
    {
        if (IsGameRunning) return;

        IsGameRunning = true;
        eatenCount = 0;
        timeRemaining = matchDuration;

        for (int i = graveyard.Count - 1; i >= 0; i--)
        {
            BoidAgent2D ghost = graveyard[i];
            graveyard.RemoveAt(i);
            if (ghost != null)
            {
                ghost.gameObject.SetActive(true);
                ghost.isDead = false;
                Renderer[] rs = ghost.GetComponentsInChildren<Renderer>();
                foreach (var r in rs) r.enabled = true;

                boids.Add(ghost);
            }
        }

        int targetCount = Mathf.Max(2, startingBoidCount);
        int needed = targetCount - boids.Count;
        for (int i = 0; i < needed; i++)
        {
            SpawnBoid();
        }


        foreach (var boid in boids)
        {
            if (boid == null) continue;

            boid.transform.position = transform.position + new Vector3(
                Random.Range(worldMin.x, worldMax.x),
                Random.Range(worldMin.y, worldMax.y),
                0f
            );

            Rigidbody2D boidRb = boid.GetComponent<Rigidbody2D>();
            if (boidRb != null)
            {
                boidRb.linearVelocity = Random.insideUnitCircle.normalized * boid.moveSpeed;
            }
        }
        RefreshScoreUI();
    }

    public void ClampAndBounce(ref Vector2 position, ref Vector2 velocity)
    {
        Vector2 localPos = position - (Vector2)transform.position;
        if (localPos.x < worldMin.x)
        {
            position.x = transform.position.x + worldMin.x;
            velocity.x = Mathf.Abs(velocity.x);
        }
        else if (localPos.x > worldMax.x)
        {
            position.x = transform.position.x + worldMax.x;
            velocity.x = -Mathf.Abs(velocity.x);
        }

        if (localPos.y < worldMin.y)
        {
            position.y = transform.position.y + worldMin.y;
            velocity.y = Mathf.Abs(velocity.y);
        }
        else if (localPos.y > worldMax.y)
        {
            position.y = transform.position.y + worldMax.y;
            velocity.y = -Mathf.Abs(velocity.y);
        }
    }

    private void Update()
    {
        if (!IsGameRunning) return;

        timeRemaining -= Time.deltaTime;
        RefreshScoreUI();

        if (timeRemaining <= 0 || (timeRemaining < matchDuration - 1.0f && boids.Count == 0))
        {
            timeRemaining = 0;
            EndRound();
        }
    }

    private void EndRound()
    {
        if (!IsGameRunning) return;
        IsGameRunning = false;

        foreach (BoidAgent2D boid in boids)
        {
            if (boid != null) boid.EndEpisode();
        }

        foreach (BoidAgent2D ghost in graveyard)
        {
            if (ghost != null) ghost.EndEpisode();
        }

        if (hunter != null && hunter.TryGetComponent<Unity.MLAgents.Agent>(out var agent))
        {
            agent.EndEpisode();
        }

        if (matchDuration <= 0) matchDuration = 60f;

        if (trainingMode)
        {
            StartGame();
        }
        else
        {
            ShowGameOverScreen();
        }
    }

    private void ShowGameOverScreen()
    {
        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartLevel()
    {
        applyHunterBuffs = false; 
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartLevelHardMode()
    {
        applyHunterBuffs = true; 
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f; 
        Debug.Log("Go Next Clicked!");
        SceneManager.LoadScene("Story_03");
    }

    public int EatBoidsWithin(Vector2 center, float radius)
    {
        int eatenThisFrame = 0;
        float radiusSqr = radius * radius;
        for (int i = boids.Count - 1; i >= 0; i--)
        {
            BoidAgent2D boid = boids[i];
            if (boid == null)
            {
                boids.RemoveAt(i);
                continue;
            }

            float distanceSqr = ((Vector2)boid.transform.position - center).sqrMagnitude;
            if (distanceSqr <= radiusSqr)
            {
                boids.RemoveAt(i);

                boid.CaughtByHunter();
                graveyard.Add(boid);

                eatenCount++;
                eatenThisFrame++;
            }
        }

        RefreshScoreUI();
        return eatenThisFrame;
    }

    private void SpawnBoid()
    {
        Vector3 spawnPos = transform.position + new Vector3(
            Random.Range(worldMin.x, worldMax.x),
            Random.Range(worldMin.y, worldMax.y),
            0f
        );


        GameObject boidObject = Instantiate(boidPrefab, spawnPos, Quaternion.identity);
        boidObject.name = "Boid";
        boidObject.tag = "Boid";
        boidObject.transform.SetParent(transform);

        BoidAgent2D boid = boidObject.GetComponent<BoidAgent2D>();
        boid.controlMode = boidControlMode;
        boid.Initialize(this, Random.insideUnitCircle.normalized);

        BehaviorParameters bp = boidObject.GetComponent<BehaviorParameters>();
        if (bp != null)
        {
            bp.BehaviorType = (boidControlMode == BoidAgent2D.BoidControlMode.ClassicHeuristic)
                ? BehaviorType.HeuristicOnly : (trainingMode ? BehaviorType.Default : BehaviorType.InferenceOnly);
        }

        boids.Add(boid);
    }

    public void RemoveBoid(BoidAgent2D boid)
    {
        if (boid == null || !boids.Contains(boid)) return;

        boids.Remove(boid);
        boid.CaughtByHunter(); 
        graveyard.Add(boid);

        eatenCount++;
        RefreshScoreUI();
    }

    private void RefreshScoreUI()
    {
        int seconds = Mathf.Max(0, Mathf.FloorToInt(timeRemaining));
        if (timerText != null)
        {
            timerText.text = $"Time: {seconds}s";
        }

        if (scoreText != null)
        {
            string timeStr = timerText == null ? $"   Time: {seconds}s" : "";
            scoreText.text = $"{teamName}\nEaten: {eatenCount}   Remaining: {boids.Count}{timeStr}";
        }
    }

    private void ClearAllBoids()
    {
        for (int i = boids.Count - 1; i >= 0; i--)
        {
            if (boids[i] != null)
            {
                Destroy(boids[i].gameObject);
            }
        }

        boids.Clear();
    }

    private void OnDrawGizmos()
    {
        Vector3 center = transform.position + new Vector3((worldMin.x + worldMax.x) * 0.5f, (worldMin.y + worldMax.y) * 0.5f, 0f);
        Vector3 size = new Vector3(worldMax.x - worldMin.x, worldMax.y - worldMin.y, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }

    private void EnsureUIIsReady()
    {
        Canvas canvas = GetComponentInChildren<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false); 
            canvas = canvasObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = GetComponentInChildren<Camera>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = GetComponentInChildren<Camera>();
            }
        }



        GameObject hudPanel = FindOrCreateHudPanel(canvas.transform);
        Text hudScoreText = FindOrCreateScoreText(hudPanel.transform);

        if (scoreText == null)
        {
            scoreText = hudScoreText;
        }
    }

    private void AddBestAvailableInputModule(GameObject eventSystemObject)
    {
        System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            eventSystemObject.AddComponent(inputSystemModuleType);
        }
        else
        {
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private GameObject FindOrCreateHudPanel(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find("HudPanel");
        GameObject hudPanel = existing != null ? existing.gameObject : new GameObject("HudPanel");
        if (existing == null)
        {
            hudPanel.transform.SetParent(canvasTransform, false);
        }

        RectTransform hudRect = hudPanel.GetComponent<RectTransform>();
        if (hudRect == null)
        {
            hudRect = hudPanel.AddComponent<RectTransform>();
        }

        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(20f, -20f);
        hudRect.sizeDelta = new Vector2(450f, 60f);
        hudPanel.SetActive(true);
        return hudPanel;
    }

    private Text FindOrCreateScoreText(Transform hudPanelTransform)
    {
        Transform existing = hudPanelTransform.Find("ScoreText");
        GameObject scoreObject = existing != null ? existing.gameObject : new GameObject("ScoreText");
        if (existing == null)
        {
            scoreObject.transform.SetParent(hudPanelTransform, false);
        }

        Text text = scoreObject.GetComponent<Text>();
        if (text == null)
        {
            text = scoreObject.AddComponent<Text>();
        }

        text.text = "Eaten: 0   Remaining: 0   Time: 60s";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform scoreRect = text.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = Vector2.zero;
        scoreRect.sizeDelta = new Vector2(450f, 60f);
        return text;
    }

    private void DrawArenaBorder()
    {
        LineRenderer line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 5;
        line.startWidth = 0.5f;
        line.endWidth = 0.5f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.black;
        line.endColor = Color.black;
        line.sortingOrder = -1;

        // ADDED: Padding to push the visual line outside the physics bounds.
        // 0.5f is slightly larger than your boidVisualSize (0.45f), making it perfect.
        float padding = 0.5f;

        Vector3 c = transform.position;

        // Subtract padding from Min, Add padding to Max
        Vector3 bottomLeft = c + new Vector3(worldMin.x - padding, worldMin.y - padding, 0);
        Vector3 topLeft = c + new Vector3(worldMin.x - padding, worldMax.y + padding, 0);
        Vector3 topRight = c + new Vector3(worldMax.x + padding, worldMax.y + padding, 0);
        Vector3 bottomRight = c + new Vector3(worldMax.x + padding, worldMin.y - padding, 0);

        line.SetPosition(0, bottomLeft);
        line.SetPosition(1, topLeft);
        line.SetPosition(2, topRight);
        line.SetPosition(3, bottomRight);
        line.SetPosition(4, bottomLeft);
    }

    private void DebugLog(string runId, string hypothesisId, string location, string message, Dictionary<string, object> data)
    {
        // Performance Fix: Disabled synchronous disk logging to prevent start-up timeouts
    }
}
