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
    public float viewRadius = 15.8f;
    public Color hunterColor = new Color(1f, 0.45f, 0.3f, 1f);
    
    [Header("Controls")]
    public bool useAutoControl;
    public bool isPlayerAgent;

    [Header("Testing & Debugging")]
    public bool useManualNormalization = true;
    public bool showDiagnostics = false;



    public BoidGameManager2D manager;
    private Rigidbody2D hunterRigidbody;
    private BufferSensorComponent boidSensor;

    protected override void Awake()
    {
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
        boidSensor.MaxNumObservables = 60; 

        base.Awake();
    }

    public override void Initialize()
    {
        if (manager == null) manager = GetComponentInParent<BoidGameManager2D>();

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
        else if (isPlayerAgent)
        {
            if (behavior != null) behavior.BehaviorType = BehaviorType.HeuristicOnly;
        }
        else
        {
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
                if (behavior != null) behavior.BehaviorType = BehaviorType.HeuristicOnly;
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        if (manager != null)
        {
            transform.position = manager.transform.position;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (manager == null || !manager.IsGameRunning)
        {
            sensor.AddObservation(Vector2.zero);
            return;
        }

        Vector2 rawVel = hunterRigidbody.linearVelocity;
        Vector2 n_vel = useManualNormalization ? (rawVel / moveSpeed) : rawVel;
        sensor.AddObservation(n_vel);

        foreach (var boid in manager.Boids)
        {
            if (boid == null || boid.isDead) continue; 
            Vector2 offset = (Vector2)boid.transform.position - (Vector2)transform.position;

            if (offset.sqrMagnitude < (viewRadius * viewRadius)) // viewing distance
            {
                Rigidbody2D boidRb = boid.GetComponent<Rigidbody2D>();
                
                Vector2 n_off = useManualNormalization ? (offset / 50f) : offset;
                Vector2 n_bvel = useManualNormalization ? (boidRb.linearVelocity / 6f) : boidRb.linearVelocity;

                boidSensor.AppendObservation(new float[] {
                    n_off.x, n_off.y, 
                    n_bvel.x, n_bvel.y
                });
            }
        }

    }

    private float cachedInputX;
    private float cachedInputY;

    private void Update()
    {
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
        
        Vector2 pos = hunterRigidbody.position;


        float globalMinX = manager.transform.position.x + manager.worldMin.x;
        float globalMaxX = manager.transform.position.x + manager.worldMax.x;
        float globalMinY = manager.transform.position.y + manager.worldMin.y;
        float globalMaxY = manager.transform.position.y + manager.worldMax.y;

        // FIX: Compare against the global boundaries
        if (pos.x < globalMinX && fakeVelocity.x < 0) fakeVelocity.x = Mathf.Abs(fakeVelocity.x);
        else if (pos.x > globalMaxX && fakeVelocity.x > 0) fakeVelocity.x = -Mathf.Abs(fakeVelocity.x);

        if (pos.y < globalMinY && fakeVelocity.y < 0) fakeVelocity.y = Mathf.Abs(fakeVelocity.y);
        else if (pos.y > globalMaxY && fakeVelocity.y > 0) fakeVelocity.y = -Mathf.Abs(fakeVelocity.y);

        hunterRigidbody.linearVelocity = fakeVelocity;

        int eatenCount = manager.EatBoidsWithin(transform.position, eatRadius);
        if (eatenCount > 0)
        {
            AddReward(eatenCount * 1.0f); 
        }


        float maxProximityReward = 0f;
        foreach (var boid in manager.Boids)
        {
            if (boid == null || boid.isDead) continue;
            float dist = Vector2.Distance(transform.position, boid.transform.position);
            if (dist < 8f) 
            {
                float currentShaping = 0.01f * (1.0f - (dist / 8f));
                if (currentShaping > maxProximityReward) maxProximityReward = currentShaping;
            }
        }
        AddReward(maxProximityReward);

        AddReward(-0.001f);
    }

    private void LateUpdate()
    {
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

        boid.CaughtByHunter(); 
        manager.RemoveBoid(boid);
        
        AddReward(1.0f);
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
