using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BoidGameManager2D : MonoBehaviour
{
    [Header("World Bounds")]
    public Vector2 worldMin = new Vector2(-24f, -13f);
    public Vector2 worldMax = new Vector2(24f, 13f);

    [Header("Boid Spawn")]
    public int startingBoidCount = 60;
    public float boidVisualSize = 0.45f;
    public Color boidColor = new Color(0.35f, 0.75f, 1f, 1f);
    public BoidAgent2D.BoidControlMode boidControlMode = BoidAgent2D.BoidControlMode.ClassicHeuristic;

    [Header("Training")]
    public bool trainingMode;
    public float matchDuration = 60f;
    public float timeRemaining;

    [Header("Hunter")]
    public HunterController2D hunter;
    public bool hunterAutoControl;

    [Header("UI")]
    public Text scoreText;

    // References
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
        // Stop Unity from freezing the entire simulation when you click away!
        Application.runInBackground = true;

        ApplyTrainingModeDefaults();

        if (hunter == null)
        {
            hunter = FindObjectOfType<HunterController2D>();
        }

        if (hunter != null)
        {
            hunter.useAutoControl = hunterAutoControl;
        }

        EnsureUIIsReady();
        StartGame();
    }

    private void ApplyTrainingModeDefaults()
    {
        if (trainingMode)
        {
            boidControlMode = BoidAgent2D.BoidControlMode.MlAgent;
            hunterAutoControl = true;
        }
    }

    public void StartGame()
    {
        if (IsGameRunning)
        {
            return;
        }

        IsGameRunning = true;
        eatenCount = 0;
        timeRemaining = matchDuration;

        // Revive Boids from the graveyard!
        for (int i = graveyard.Count - 1; i >= 0; i--)
        {
            BoidAgent2D ghost = graveyard[i];
            graveyard.RemoveAt(i);
            if (ghost != null)
            {
                ghost.gameObject.SetActive(true);
                boids.Add(ghost);
            }
        }

        // Stutter Fix: Only spawn missing Boids instead of destroying and re-rendering 64 complex 3D objects!
        int targetCount = Mathf.Max(2, startingBoidCount);
        int needed = targetCount - boids.Count;
        
        for (int i = 0; i < needed; i++)
        {
            SpawnBoid();
        }

        // Instantly pool and randomize existing boids
        foreach (var boid in boids)
        {
            if (boid == null) continue;
            boid.transform.position = new Vector3(
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

        // Normalize Hunter speed to perfectly match the boids max speed!
        if (hunter != null && boids.Count > 0)
        {
            hunter.moveSpeed = boids[0].maxSpeed;
        }

        RefreshScoreUI();
    }

    public void ClampAndBounce(ref Vector2 position, ref Vector2 velocity)
    {
        if (position.x < worldMin.x)
        {
            position.x = worldMin.x;
            velocity.x = Mathf.Abs(velocity.x);
        }
        else if (position.x > worldMax.x)
        {
            position.x = worldMax.x;
            velocity.x = -Mathf.Abs(velocity.x);
        }

        if (position.y < worldMin.y)
        {
            position.y = worldMin.y;
            velocity.y = Mathf.Abs(velocity.y);
        }
        else if (position.y > worldMax.y)
        {
            position.y = worldMax.y;
            velocity.y = -Mathf.Abs(velocity.y);
        }
    }

    private void Update()
    {
        if (!IsGameRunning) return;

        timeRemaining -= Time.deltaTime;
        RefreshScoreUI();

        // End round completely if the time is up, OR if the hunter successfully caught all boids!
        if (timeRemaining <= 0 || boids.Count == 0)
        {
            timeRemaining = 0;
            EndRound();
        }
    }

    private void EndRound()
    {
        IsGameRunning = false;
        
        if (hunter != null && hunter.TryGetComponent<Unity.MLAgents.Agent>(out var agent))
        {
            // End the hunter's ML-Agents episode explicitly so it learns from this 60s batch
            agent.EndEpisode();
        }

        StartGame();
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
                
                // CRITICAL FIX: Destroying the Boid immediately after calling EndEpisode() severs the 
                // socket connection to Python before Python realizes the Boid died! Python sits there 
                // waiting 60 seconds for the dead Boid's next move, crashing the entire trainer!
                boid.CaughtByHunter(); 
                
                // Instead of teleporting them endlessly, we deactivate them! This formally and elegantly
                // unregisters the PyTorch socket hook, and puts them in our Graveyard pool!
                boid.gameObject.SetActive(false);
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
        GameObject boidObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        boidObject.name = "Boid";
        boidObject.transform.SetParent(transform);
        boidObject.transform.position = new Vector3(
            Random.Range(worldMin.x, worldMax.x),
            Random.Range(worldMin.y, worldMax.y),
            0f
        );
        boidObject.transform.localScale = Vector3.one * boidVisualSize;

        Renderer boidRenderer = boidObject.GetComponent<Renderer>();
        if (boidRenderer != null)
        {
            boidRenderer.material.color = boidColor;
        }

        Collider boidCollider = boidObject.GetComponent<Collider>();
        // #region agent log
        DebugLog("run-2", "H1", "BoidGameManager2D.cs:143", "SpawnBoid primitive collider state", new Dictionary<string, object>
        {
            { "has3dColliderBeforeDestroy", boidCollider != null },
            { "colliderType", boidCollider != null ? boidCollider.GetType().Name : "none" }
        });
        // #endregion
        if (boidCollider != null)
        {
            DestroyImmediate(boidCollider);
        }

        // #region agent log
        DebugLog("run-2", "H1", "BoidGameManager2D.cs:154", "Post-destroy collider check", new Dictionary<string, object>
        {
            { "has3dColliderAfterDestroyCall", boidObject.GetComponent<Collider>() != null }
        });
        // #endregion

        BoxCollider2D boidCollider2D = null;
        try
        {
            boidCollider2D = boidObject.AddComponent<BoxCollider2D>();
            // #region agent log
            DebugLog("run-2", "H1", "BoidGameManager2D.cs:165", "Added BoxCollider2D", new Dictionary<string, object>
            {
                { "boxColliderAdded", boidCollider2D != null }
            });
            // #endregion
        }
        catch (System.Exception ex)
        {
            // #region agent log
            DebugLog("run-2", "H1", "BoidGameManager2D.cs:173", "Failed adding BoxCollider2D", new Dictionary<string, object>
            {
                { "exceptionType", ex.GetType().Name },
                { "exceptionMessage", ex.Message }
            });
            // #endregion
            throw;
        }

        if (boidCollider2D == null)
        {
            // #region agent log
            DebugLog("post-fix", "H1", "BoidGameManager2D.cs:182", "BoxCollider2D is null after AddComponent", new Dictionary<string, object>
            {
                { "has3dColliderNow", boidObject.GetComponent<Collider>() != null }
            });
            // #endregion
            return;
        }

        boidCollider2D.isTrigger = true;

        Rigidbody2D boidRb = boidObject.AddComponent<Rigidbody2D>();
        boidRb.gravityScale = 0f;
        boidRb.linearDamping = 0.2f;
        boidRb.angularDamping = 0.2f;
        boidRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoidAgent2D boid = boidObject.AddComponent<BoidAgent2D>();
        // #region agent log
        DebugLog("run-2", "H2", "BoidGameManager2D.cs:193", "BoidAgent2D add result", new Dictionary<string, object>
        {
            { "boidComponentNull", boid == null },
            { "hasRigidbody2D", boidObject.GetComponent<Rigidbody2D>() != null },
            { "hasCollider2D", boidObject.GetComponent<Collider2D>() != null }
        });
        // #endregion
        boid.controlMode = boidControlMode;
        boid.Initialize(this, Random.insideUnitCircle.normalized);
        boids.Add(boid);
    }

    public void RemoveBoid(BoidAgent2D boid)
    {
        if (boid == null)
        {
            return;
        }

        if (boids.Remove(boid))
        {
            eatenCount++;
            RefreshScoreUI();
        }

        Destroy(boid.gameObject);
    }

    private void RefreshScoreUI()
    {
        if (scoreText != null)
        {
            int seconds = Mathf.Max(0, Mathf.FloorToInt(timeRemaining));
            scoreText.text = $"Eaten: {eatenCount}   Remaining: {boids.Count}   Time: {seconds}s";
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
        Vector3 center = new Vector3((worldMin.x + worldMax.x) * 0.5f, (worldMin.y + worldMax.y) * 0.5f, 0f);
        Vector3 size = new Vector3(worldMax.x - worldMin.x, worldMax.y - worldMin.y, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }

    private void EnsureUIIsReady()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            AddBestAvailableInputModule(eventSystemObject);
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

    private void DebugLog(string runId, string hypothesisId, string location, string message, Dictionary<string, object> data)
    {
        try
        {
            string dataJson = "{}";
            if (data != null && data.Count > 0)
            {
                List<string> kvPairs = new List<string>();
                foreach (KeyValuePair<string, object> kv in data)
                {
                    string value = kv.Value == null ? "null" : kv.Value.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"");
                    kvPairs.Add("\"" + kv.Key + "\":\"" + value + "\"");
                }

                dataJson = "{" + string.Join(",", kvPairs) + "}";
            }

            string json = "{\"sessionId\":\"dddf96\",\"runId\":\"" + runId + "\",\"hypothesisId\":\"" + hypothesisId + "\",\"location\":\"" + location + "\",\"message\":\"" + message + "\",\"data\":" + dataJson + ",\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
            File.AppendAllText("debug-dddf96.log", json + System.Environment.NewLine);
        }
        catch
        {
        }
    }
}
