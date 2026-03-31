using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(BufferSensorComponent))]
public class BoidAgent2D : Agent
{
    public enum BoidControlMode
    {
        ClassicHeuristic,
        MlAgent
    }

    public float moveSpeed = 4.5f; 
    public float maxSteerForce = 8f;
    public float rotationSpeed = 220f;
    public float maxSpeed = 6f;

    [Header("Boid Rules")]
    public float neighborRadius = 3.5f;
    public float separationRadius = 1.2f;
    public float separationWeight = 2.2f;
    public float alignmentWeight = 1f;
    public float cohesionWeight = 1.1f;

    [Header("Flee")]
    public float hunterDetectRadius = 6f;
    public float fleeWeight = 3.5f;

    [Header("Rewards")]
    public float flockRewardDistance = 2.5f;
    public float hunterDangerDistance = 1.5f;
    public BoidControlMode controlMode = BoidControlMode.ClassicHeuristic;


    [Header("ML-Agents")]
    public string behaviorName = "BoidAgent";
    public int bufferObservationSize = 5;

    [HideInInspector] public bool isDead;
    public int bufferMaxObservations = 32;

    private BoidGameManager2D manager;
    private Rigidbody2D rb;
    private BufferSensorComponent neighborBufferSensor;
    private Vector2 velocity;

    protected override void Awake()
    {
        BehaviorParameters behavior = GetComponent<BehaviorParameters>();
        if (behavior == null) { behavior = gameObject.AddComponent<BehaviorParameters>(); }

        behavior.BehaviorName = behaviorName;
        behavior.BrainParameters.VectorObservationSize = 5;
        behavior.BrainParameters.NumStackedVectorObservations = 1;
        behavior.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(3);

        DecisionRequester requester = GetComponent<DecisionRequester>();
        if (requester == null) { requester = gameObject.AddComponent<DecisionRequester>(); }

        requester.DecisionPeriod = 5; 
        requester.TakeActionsBetweenDecisions = true;

        base.Awake();
    }

    public override void Initialize()
    {
        neighborBufferSensor = GetComponent<BufferSensorComponent>();
        if (neighborBufferSensor != null)
        {
            neighborBufferSensor.ObservableSize = bufferObservationSize;
            neighborBufferSensor.MaxNumObservables = bufferMaxObservations;
        }
    }

    public void Initialize(BoidGameManager2D boidManager, Vector2 startDirection)
    {
        manager = boidManager;
        rb = GetComponent<Rigidbody2D>();
        neighborBufferSensor = GetComponent<BufferSensorComponent>();
        velocity = startDirection.normalized * moveSpeed;
        rb.linearVelocity = velocity;

        if (controlMode == BoidControlMode.MlAgent)
        {
            BehaviorParameters behavior = GetComponent<BehaviorParameters>();
            if (behavior != null)
            {
                if(manager.trainingMode != false)behavior.BehaviorType=BehaviorType.Default;
                else behavior.BehaviorType=BehaviorType.HeuristicOnly;
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        isDead = false;
        Renderer r = GetComponent<Renderer>();
        if (r != null) r.enabled = true;

        if (manager == null) return;

        Vector2 spawnPos = new Vector2(
            Random.Range(manager.worldMin.x, manager.worldMax.x),
            Random.Range(manager.worldMin.y, manager.worldMax.y)
        );
        transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
        velocity = Random.insideUnitCircle.normalized * moveSpeed;
        rb.linearVelocity = velocity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (isDead || manager == null)
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(Vector2.zero);
            return;
        }

        Vector2 currentPosition = transform.position;
        Vector3 relativeHunterPos = (Vector2)manager.HunterPosition - currentPosition;
        float hDist = relativeHunterPos.magnitude;
        Vector3 flockCenterRel = GetFlockCenter(currentPosition) - currentPosition;

        float n_hDist =  hDist;
        Vector2 n_vel = rb != null ? rb.linearVelocity : velocity;
        Vector2 n_flock = flockCenterRel;

        sensor.AddObservation(n_vel);
        sensor.AddObservation(n_hDist); 
        sensor.AddObservation(n_flock);


        if (neighborBufferSensor != null)
        {
            List<BoidAgent2D> allBoids = manager.Boids;
            for (int i = 0; i < allBoids.Count; i++)
            {
                BoidAgent2D other = allBoids[i];
                if (other == null || other == this) continue;

                Vector2 offset = (Vector2)other.transform.position - currentPosition;
                float distance = offset.magnitude;
                if (distance <= neighborRadius)
                {
                    neighborBufferSensor.AppendObservation(new float[]
                    {
                        offset.x / 50f, offset.y / 50f, distance / 50f,
                        other.velocity.x / maxSpeed, other.velocity.y / maxSpeed
                    });
                }
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isDead || manager == null || !manager.IsGameRunning || controlMode != BoidControlMode.MlAgent)
            return;

        float x = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float y = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float rotate = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        ApplySteeringAndRewards(new Vector2(x, y) * maxSteerForce, rotate);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuous = actionsOut.ContinuousActions;
        Vector2 currentPosition = transform.position;
        Vector2 steering = ComputeSteering(currentPosition);

        continuous[0] = Mathf.Clamp(steering.x / maxSteerForce, -1f, 1f);
        continuous[1] = Mathf.Clamp(steering.y / maxSteerForce, -1f, 1f);

        float desiredAngle = Mathf.Atan2(steering.y, steering.x) * Mathf.Rad2Deg;
        float currentAngle = rb != null ? rb.rotation : transform.eulerAngles.z;
        float delta = Mathf.DeltaAngle(currentAngle, desiredAngle);
        continuous[2] = Mathf.Clamp(delta / 45f, -1f, 1f);
    }

    private void FixedUpdate()
    {
        if (manager == null || !manager.IsGameRunning || rb == null)
        {
            return;
        }

        if (controlMode == BoidControlMode.ClassicHeuristic)
        {
            Vector2 steering = ComputeSteering(transform.position);
            float desiredAngle = Mathf.Atan2(steering.y, steering.x) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(rb.rotation, desiredAngle);
            float rotateInput = Mathf.Clamp(delta / 45f, -1f, 1f);
            ApplySteeringAndRewards(steering, rotateInput);
        }
    }

    private void LateUpdate()
    {
        if (manager == null || !manager.IsGameRunning || rb == null) return;

        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;
        bool outOfBounds = false;

        if (pos.x < manager.worldMin.x) { 
            pos.x = manager.worldMin.x; 
            vel.x = Mathf.Abs(vel.x); 
            outOfBounds = true; 
        }
        else if (pos.x > manager.worldMax.x) { 
            pos.x = manager.worldMax.x; 
            vel.x = -Mathf.Abs(vel.x); 
            outOfBounds = true; 
        }

        if (pos.y < manager.worldMin.y) {
            pos.y = manager.worldMin.y; 
            vel.y = Mathf.Abs(vel.y); 
            outOfBounds = true; 
        }
        else if (pos.y > manager.worldMax.y) { pos.y = manager.worldMax.y; 
            vel.y = -Mathf.Abs(vel.y); 
            outOfBounds = true; }

        if (outOfBounds)
        {
            rb.position = pos;
            rb.linearVelocity = vel;
            velocity = vel;
            
            if (controlMode == BoidControlMode.MlAgent) 
            {
                AddReward(-0.05f); 
            }
        }
    }

    public void CaughtByHunter()
    {
        isDead = true;
        AddReward(-1f);

        Renderer r = GetComponent<Renderer>();
        if (r != null) r.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private Vector2 GetFlockCenter(Vector2 currentPosition)
    {
        if (manager == null || manager.Boids.Count <= 1)
        {
            return currentPosition;
        }

        Vector2 center = Vector2.zero;
        int count = 0;
        List<BoidAgent2D> allBoids = manager.Boids;
        for (int i = 0; i < allBoids.Count; i++)
        {
            BoidAgent2D other = allBoids[i];
            if (other == null || other == this)
            {
                continue;
            }

            center += (Vector2)other.transform.position;
            count++;
        }

        if (count == 0)
        {
            return currentPosition;
        }

        return center / count;
    }

    private Vector2 ComputeSteering(Vector2 currentPosition)
    {
        Vector2 toHunter = (Vector2)manager.HunterPosition - currentPosition;
        float distanceToHunter = toHunter.magnitude;
        if (distanceToHunter <= hunterDetectRadius)
        {
            Vector2 fleeDirection = (-toHunter).normalized * moveSpeed;
            return LimitForce(fleeDirection - velocity) * fleeWeight;
        }

        List<BoidAgent2D> allBoids = manager.Boids;
        Vector2 separation = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 cohesionCenter = Vector2.zero;
        int neighborCount = 0;

        for (int i = 0; i < allBoids.Count; i++)
        {
            BoidAgent2D other = allBoids[i];
            if (other == this || other == null)
            {
                continue;
            }

            Vector2 offset = (Vector2)other.transform.position - currentPosition;
            float distance = offset.magnitude;
            if (distance > neighborRadius || distance <= 0.0001f)
            {
                continue;
            }

            neighborCount++;
            alignment += other.velocity.sqrMagnitude > 0.01f ? other.velocity.normalized : Vector2.zero;
            cohesionCenter += (Vector2)other.transform.position;

            if (distance < separationRadius)
            {
                separation -= offset / Mathf.Max(distance, 0.05f);
            }
        }

        if (neighborCount == 0)
        {
            return Vector2.zero;
        }

        alignment /= neighborCount;
        cohesionCenter /= neighborCount;

        Vector2 separationForce = LimitForce(separation.normalized * moveSpeed - velocity) * separationWeight;
        Vector2 alignmentForce = LimitForce(alignment.normalized * moveSpeed - velocity) * alignmentWeight;
        Vector2 cohesionDirection = (cohesionCenter - currentPosition).normalized;
        Vector2 cohesionForce = LimitForce(cohesionDirection * moveSpeed - velocity) * cohesionWeight;

        return separationForce + alignmentForce + cohesionForce;
    }

    private Vector2 LimitForce(Vector2 force)
    {
        if (force.magnitude > maxSteerForce)
        {
            return force.normalized * maxSteerForce;
        }

        return force;
    }

    private void ApplySteeringAndRewards(Vector2 steeringForce, float rotateInput)
    {
        rb.AddForce(steeringForce, ForceMode2D.Force);
        rb.MoveRotation(rb.rotation + rotateInput * rotationSpeed * Time.fixedDeltaTime);

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        if (rb.linearVelocity.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Random.insideUnitCircle.normalized * moveSpeed;
        }

        velocity = rb.linearVelocity;

        if (controlMode == BoidControlMode.ClassicHeuristic && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            rb.MoveRotation(angle);
        }

        EvaluateRewards(velocity);
    }

    private void EvaluateRewards(Vector2 currentVelocity)
    {
        Vector2 currentPosition = transform.position;
        Vector2 toHunter = (Vector2)manager.HunterPosition - currentPosition;
        float distanceToHunter = toHunter.magnitude;

        if (distanceToHunter < hunterDangerDistance)
        {
            float penalty = 1f - (distanceToHunter / hunterDangerDistance);
            AddReward(-penalty * 0.05f);
        }
        else 
        {
            AddReward(0.005f);
        }

        List<BoidAgent2D> allBoids = manager.Boids;
        Vector2 separation = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        int neighborCount = 0;

        for (int i = 0; i < allBoids.Count; i++)
        {
            BoidAgent2D other = allBoids[i];
            if (other == this || other == null) continue;

            Vector2 offset = (Vector2)other.transform.position - currentPosition;
            float distance = offset.magnitude;
            
            if (distance > neighborRadius || distance <= 0.0001f) continue;
            
            neighborCount++;
            alignment += other.velocity.sqrMagnitude > 0.01f ? other.velocity.normalized : Vector2.zero;

            if (distance < separationRadius)
            {
                separation -= offset / Mathf.Max(distance, 0.05f);
            }
        }

        if (neighborCount > 0)
        {
            alignment /= neighborCount;

            float alignmentMatch = Vector2.Dot(currentVelocity.normalized, alignment.normalized);
            if (alignmentMatch > 0.5f) AddReward(alignmentMatch * 0.01f);

            if (separation.sqrMagnitude > 0.01f)
            {
                float separationMatch = Vector2.Dot(currentVelocity.normalized, separation.normalized);
                if (separationMatch > 0.5f) AddReward(separationMatch * 0.01f);
            }

            float flockDistance = Vector2.Distance(currentPosition, GetFlockCenter(currentPosition));
            if (flockDistance <= flockRewardDistance)
            {
                AddReward(0.01f);
            }
        }
    }


}
