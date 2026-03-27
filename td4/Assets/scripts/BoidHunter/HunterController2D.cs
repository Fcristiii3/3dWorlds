using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(Rigidbody2D))]
public class HunterController2D : Agent
{
    [Header("Hunter Settings")]
    public float moveSpeed = 10f;
    public float eatRadius = 1f;
    public Color hunterColor = new Color(1f, 0.45f, 0.3f, 1f);
    
    [Header("Controls")]
    public bool useAutoControl;

    [Header("References")]
    public BoidGameManager2D manager;
    private Rigidbody2D hunterRigidbody;
    private BufferSensorComponent boidSensor;

    protected override void Awake()
    {
        // 1. Force Inspector settings BEFORE ML-Agents boots
        BehaviorParameters behavior = GetComponent<BehaviorParameters>();
        if (behavior == null) behavior = gameObject.AddComponent<BehaviorParameters>();
        behavior.BehaviorName = "HunterAgent";
        behavior.BrainParameters.VectorObservationSize = 2; 
        behavior.BrainParameters.NumStackedVectorObservations = 1;
        behavior.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(2);

        DecisionRequester requester = GetComponent<DecisionRequester>();
        if (requester == null) requester = gameObject.AddComponent<DecisionRequester>();
        requester.DecisionPeriod = 5;
        requester.TakeActionsBetweenDecisions = true;

        boidSensor = GetComponent<BufferSensorComponent>();
        if (boidSensor == null) boidSensor = gameObject.AddComponent<BufferSensorComponent>();
        boidSensor.ObservableSize = 4;
        boidSensor.MaxNumObservables = 20;

        // 2. Let ML-Agents boot safely
        base.Awake();
    }

    public override void Initialize()
    {
        if (manager == null) manager = FindObjectOfType<BoidGameManager2D>();

        Renderer hunterRenderer = GetComponent<Renderer>();
        if (hunterRenderer != null) hunterRenderer.material.color = hunterColor;

        BoxCollider2D hunterCollider = GetComponent<BoxCollider2D>();
        if (hunterCollider == null) hunterCollider = gameObject.AddComponent<BoxCollider2D>();
        hunterCollider.isTrigger = true;

        hunterRigidbody = GetComponent<Rigidbody2D>();
        if (hunterRigidbody == null) hunterRigidbody = gameObject.AddComponent<Rigidbody2D>();
        hunterRigidbody.gravityScale = 0f;
        hunterRigidbody.bodyType = RigidbodyType2D.Kinematic;

        BehaviorParameters behavior = GetComponent<BehaviorParameters>();
        if (manager != null && manager.trainingMode)
        {
            if (behavior != null) behavior.BehaviorType = BehaviorType.Default;
        }
        else
        {
            // If a trained brain is assigned on the GameManager, use it for inference
            if (manager != null && manager.hunterBrain != null)
            {
                if (behavior != null)
                {
                    behavior.Model = manager.hunterBrain;
                    behavior.BehaviorType = BehaviorType.InferenceOnly;
                }
            }
            else
            {
                // Fall back to manual WASD control if no brain is assigned
                if (behavior != null) behavior.BehaviorType = BehaviorType.HeuristicOnly;
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        if (manager != null)
        {
            // Reset position to center whenever the 60-second timer rolls over!
            transform.position = Vector3.zero;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (manager == null || !manager.IsGameRunning)
        {
            sensor.AddObservation(Vector2.zero);
            return;
        }

        // 1. My Own Velocity (2 floats)
        sensor.AddObservation(hunterRigidbody.linearVelocity);

        // 2. Buffer sensor looks at all boids to find nearest targets
        foreach (var boid in manager.Boids)
        {
            if (boid == null) continue;
            Vector2 offset = (Vector2)boid.transform.position - (Vector2)transform.position;
            
            // Only supply boids within a certain logical hunt viewing distance
            if (offset.sqrMagnitude < 250f) 
            {
                Rigidbody2D boidRb = boid.GetComponent<Rigidbody2D>();
                boidSensor.AppendObservation(new float[] {
                    offset.x, offset.y, 
                    boidRb.linearVelocity.x, 
                    boidRb.linearVelocity.y
                });
            }
        }
    }

    private float cachedInputX;
    private float cachedInputY;

    private void Update()
    {
        // Cache input strictly in Update so we never miss a keystroke
        float rawX = Input.GetAxisRaw("Horizontal");
        float rawY = Input.GetAxisRaw("Vertical");
        
        if (Mathf.Abs(rawX) > 0.1f) cachedInputX = rawX;
        else cachedInputX = Mathf.Lerp(cachedInputX, 0, Time.deltaTime * 10f);

        if (Mathf.Abs(rawY) > 0.1f) cachedInputY = rawY;
        else cachedInputY = Mathf.Lerp(cachedInputY, 0, Time.deltaTime * 10f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (manager == null || !manager.IsGameRunning) return;

        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveY = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        
        Vector2 input = new Vector2(moveX, moveY);
        Vector2 fakeVelocity = input * moveSpeed;
        
        // Simply apply the velocity. Unity physics engine handles the rest perfectly!
        hunterRigidbody.linearVelocity = fakeVelocity;

        // Try to eat any boids within radius
        int eatenCount = manager.EatBoidsWithin(transform.position, eatRadius);
        if (eatenCount > 0)
        {
            AddReward(eatenCount * 1.0f); // Massive reward for eating boids!
        }
        else
        {
            AddReward(-0.002f); // Tiny penalty for time passing so he learns to hunt FAST
        }
    }

    private void LateUpdate()
    {
        if (manager == null || !manager.IsGameRunning || hunterRigidbody == null) return;

        Vector2 pos = hunterRigidbody.position;
        bool outOfBounds = false;

        if (pos.x < manager.worldMin.x) { pos.x = manager.worldMin.x; outOfBounds = true; }
        else if (pos.x > manager.worldMax.x) { pos.x = manager.worldMax.x; outOfBounds = true; }

        if (pos.y < manager.worldMin.y) { pos.y = manager.worldMin.y; outOfBounds = true; }
        else if (pos.y > manager.worldMax.y) { pos.y = manager.worldMax.y; outOfBounds = true; }

        if (outOfBounds)
        {
            hunterRigidbody.position = pos;
            if (manager.trainingMode) AddReward(-0.05f); // Learn to not hit walls!
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        Vector2 input = useAutoControl ? GetAutoDirection() : new Vector2(cachedInputX, cachedInputY);
        
        if (input.sqrMagnitude > 1f) input.Normalize();

        continuous[0] = input.x;
        continuous[1] = input.y;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        BoidAgent2D boid = other.GetComponent<BoidAgent2D>();
        if (boid == null || manager == null) return;

        boid.CaughtByHunter(); // Tells the boid it lost
        manager.RemoveBoid(boid);
        
        AddReward(1.0f); // Positive reward for hunter
    }

    private Vector2 GetAutoDirection()
    {
        if (manager == null || manager.Boids.Count == 0) return Vector2.zero;

        Vector2 current = transform.position;
        BoidAgent2D nearest = null;
        float bestDistanceSqr = float.MaxValue;
        
        for (int i = 0; i < manager.Boids.Count; i++)
        {
            BoidAgent2D boid = manager.Boids[i];
            if (boid == null) continue;

            float distSqr = ((Vector2)boid.transform.position - current).sqrMagnitude;
            if (distSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distSqr;
                nearest = boid;
            }
        }

        if (nearest == null) return Vector2.zero;

        return ((Vector2)nearest.transform.position - current).normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, eatRadius);
    }
}
