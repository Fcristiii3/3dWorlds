using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AIControls : MonoBehaviour
{
    private struct SensorSnapshot
    {
        public bool centerHit;
        public bool leftHit;
        public bool rightHit;
        public float centerDistance;
        public float leftDistance;
        public float rightDistance;
        public Vector3 origin;
        public Vector3 centerDirection;
        public Vector3 leftDirection;
        public Vector3 rightDirection;
    }

    public UnityEvent<Vector2> onInput;

    public Transform waypointsHolder;
    public Transform raycastOrigin;
    public LayerMask wallLayer;

    public float maxDistanceToTarget = 5f;
    public float maxDistanceToReverse = 10f;
    public float randomJitterOnPosition = 0.5f;

    public float sensorLength = 20f;
    public float frontSensorAngle = 30f;
    public float waypointSteerResponsiveness = 1.5f;
    public float avoidanceSteerStrength = 1.2f;
    public float brakingDistance = 15f;
    public float aggressiveBrakeStrength = 12f;
    [Range(0.05f, 1f)]
    public float minimumForwardSpeedFactor = 0.2f;
    public bool enableDebugLogs = true;

    private readonly List<Transform> waypoints = new List<Transform>();

    private Vector2 input;
    private Transform nextWaypoint;
    private Vector3 nextWaypointPosition;
    private Rigidbody cachedRigidbody;
    private SensorSnapshot latestSensors;
    private float rememberedCruiseSpeed;

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        RebuildWaypoints();
    }

    private void Start()
    {
        if (cachedRigidbody != null)
        {
            cachedRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            cachedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (waypoints.Count > 0)
        {
            SelectWaypoint(waypoints[0]);
        }
    }

    private void Update()
    {
        if (waypoints.Count == 0 || nextWaypoint == null)
        {
            input = Vector2.zero;
            onInput?.Invoke(input);
            return;
        }

        AdvanceWaypointIfNeeded();
        latestSensors = ReadSensors();

        Vector2 desiredInput = BuildWaypointInput();
        desiredInput = ApplySensorAvoidance(desiredInput, latestSensors);

        input = new Vector2(
            Mathf.Clamp(desiredInput.x, -1f, 1f),
            Mathf.Clamp(desiredInput.y, -1f, 1f)
        );

        onInput?.Invoke(input);
    }

    private void FixedUpdate()
    {
        if (cachedRigidbody == null)
        {
            return;
        }

        Vector3 velocity = cachedRigidbody.linearVelocity;
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);

        if (!latestSensors.centerHit || latestSensors.centerDistance >= brakingDistance)
        {
            rememberedCruiseSpeed = Mathf.Max(
                Mathf.Max(rememberedCruiseSpeed * 0.98f, 0f),
                Mathf.Max(0f, forwardSpeed)
            );
            return;
        }

        if (forwardSpeed <= 0f)
        {
            return;
        }

        float safeBrakingDistance = Mathf.Max(0.01f, brakingDistance);
        float brakeT = 1f - Mathf.Clamp01(latestSensors.centerDistance / safeBrakingDistance);
        float cruiseSpeed = Mathf.Max(rememberedCruiseSpeed, forwardSpeed);
        float minimumAllowedSpeed = cruiseSpeed * minimumForwardSpeedFactor;
        float targetForwardSpeed = Mathf.Lerp(cruiseSpeed, minimumAllowedSpeed, brakeT);
        float keptForwardSpeed = Mathf.Min(forwardSpeed, Mathf.Max(targetForwardSpeed, minimumAllowedSpeed));

        Vector3 sideVelocity = velocity - transform.forward * forwardSpeed;
        Vector3 brakedVelocity = sideVelocity + transform.forward * keptForwardSpeed;

        cachedRigidbody.linearVelocity = Vector3.Lerp(
            velocity,
            brakedVelocity,
            aggressiveBrakeStrength * Time.fixedDeltaTime
        );
    }

    public void SetWaypointsHolder(Transform newWaypointsHolder)
    {
        waypointsHolder = newWaypointsHolder;
        RebuildWaypoints();

        if (waypoints.Count > 0)
        {
            SelectWaypoint(waypoints[0]);
        }
    }

    private void AdvanceWaypointIfNeeded()
    {
        float distanceToTarget = Vector3.Distance(transform.position, nextWaypointPosition);
        if (distanceToTarget >= maxDistanceToTarget)
        {
            return;
        }

        int nextIndex = waypoints.IndexOf(nextWaypoint) + 1;
        SelectWaypoint(nextIndex < waypoints.Count ? waypoints[nextIndex] : waypoints[0]);
    }

    private Vector2 BuildWaypointInput()
    {
        Vector3 diff = nextWaypointPosition - transform.position;
        diff.y = 0f;

        float distanceToTarget = diff.magnitude;
        if (distanceToTarget <= 0.01f)
        {
            return new Vector2(0f, 1f);
        }

        Vector3 toTarget = diff.normalized;
        float componentForward = Vector3.Dot(toTarget, transform.forward);
        float componentRight = Vector3.Dot(toTarget, transform.right);

        float throttle = componentForward >= 0f ? 1f : (distanceToTarget > maxDistanceToReverse ? 1f : -1f);
        float steer = Mathf.Clamp(componentRight * waypointSteerResponsiveness, -1f, 1f);

        if (componentForward < 0f && distanceToTarget > maxDistanceToReverse)
        {
            steer = Mathf.Sign(componentRight);
            throttle = 1f;
        }

        return new Vector2(steer, throttle);
    }

    private Vector2 ApplySensorAvoidance(Vector2 desiredInput, SensorSnapshot sensors)
    {
        if (!sensors.centerHit && !sensors.leftHit && !sensors.rightHit)
        {
            return desiredInput;
        }

        float leftClearance = sensors.leftHit ? sensors.leftDistance : sensorLength;
        float rightClearance = sensors.rightHit ? sensors.rightDistance : sensorLength;
        float avoidanceSteer = 0f;

        if (sensors.leftHit && !sensors.rightHit)
        {
            avoidanceSteer = 1f;
        }
        else if (sensors.rightHit && !sensors.leftHit)
        {
            avoidanceSteer = -1f;
        }
        else if (sensors.centerHit || sensors.leftHit || sensors.rightHit)
        {
            float clearanceDifference = Mathf.Clamp((rightClearance - leftClearance) / Mathf.Max(0.01f, sensorLength), -1f, 1f);
            avoidanceSteer = clearanceDifference;

            if (Mathf.Abs(avoidanceSteer) < 0.1f && sensors.centerHit)
            {
                avoidanceSteer = desiredInput.x >= 0f ? 1f : -1f;
            }
        }

        float steer = Mathf.Clamp(
            desiredInput.x * 0.35f + avoidanceSteer * avoidanceSteerStrength,
            -1f,
            1f
        );

        return new Vector2(steer, desiredInput.y);
    }

    private SensorSnapshot ReadSensors()
    {
        SensorSnapshot sensors = new SensorSnapshot();
        sensors.origin = GetRayOrigin();

        Transform basis = raycastOrigin != null ? raycastOrigin : transform;
        Vector3 forward = basis.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        forward.Normalize();
        Vector3 up = basis.up;

        sensors.centerDirection = forward;
        sensors.leftDirection = Quaternion.AngleAxis(-frontSensorAngle, up) * forward;
        sensors.rightDirection = Quaternion.AngleAxis(frontSensorAngle, up) * forward;

        sensors.centerDirection.y = 0f;
        sensors.leftDirection.y = 0f;
        sensors.rightDirection.y = 0f;

        sensors.centerDirection.Normalize();
        sensors.leftDirection.Normalize();
        sensors.rightDirection.Normalize();

        sensors.centerHit = Physics.Raycast(
            sensors.origin,
            sensors.centerDirection,
            out RaycastHit centerHit,
            sensorLength,
            wallLayer,
            QueryTriggerInteraction.Ignore
        );
        sensors.leftHit = Physics.Raycast(
            sensors.origin,
            sensors.leftDirection,
            out RaycastHit leftHit,
            sensorLength,
            wallLayer,
            QueryTriggerInteraction.Ignore
        );
        sensors.rightHit = Physics.Raycast(
            sensors.origin,
            sensors.rightDirection,
            out RaycastHit rightHit,
            sensorLength,
            wallLayer,
            QueryTriggerInteraction.Ignore
        );

        sensors.centerDistance = sensors.centerHit ? centerHit.distance : sensorLength;
        sensors.leftDistance = sensors.leftHit ? leftHit.distance : sensorLength;
        sensors.rightDistance = sensors.rightHit ? rightHit.distance : sensorLength;

        if (enableDebugLogs && sensors.centerHit)
        {
            Debug.Log($"[AIControls] Center ray from '{name}' hit '{centerHit.collider.gameObject.name}' on layer '{LayerMask.LayerToName(centerHit.collider.gameObject.layer)}' at distance {centerHit.distance:F2}.");
        }

        if (enableDebugLogs && sensors.leftHit)
        {
            Debug.Log($"[AIControls] Left ray from '{name}' hit '{leftHit.collider.gameObject.name}' on layer '{LayerMask.LayerToName(leftHit.collider.gameObject.layer)}' at distance {leftHit.distance:F2}.");
        }

        if (enableDebugLogs && sensors.rightHit)
        {
            Debug.Log($"[AIControls] Right ray from '{name}' hit '{rightHit.collider.gameObject.name}' on layer '{LayerMask.LayerToName(rightHit.collider.gameObject.layer)}' at distance {rightHit.distance:F2}.");
        }

        Debug.DrawRay(
            sensors.origin,
            sensors.centerDirection * sensors.centerDistance,
            sensors.centerHit ? Color.red : Color.green
        );
        Debug.DrawRay(
            sensors.origin,
            sensors.leftDirection * sensors.leftDistance,
            sensors.leftHit ? Color.red : Color.green
        );
        Debug.DrawRay(
            sensors.origin,
            sensors.rightDirection * sensors.rightDistance,
            sensors.rightHit ? Color.red : Color.green
        );

        return sensors;
    }

    private Vector3 GetRayOrigin()
    {
        if (raycastOrigin != null)
        {
            return raycastOrigin.position;
        }

        return transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
    }

    private void SelectWaypoint(Transform waypoint)
    {
        if (waypoint == null)
        {
            nextWaypoint = null;
            nextWaypointPosition = transform.position;
            return;
        }

        nextWaypoint = waypoint;
        nextWaypointPosition = nextWaypoint.position + new Vector3(
            Random.Range(-randomJitterOnPosition, randomJitterOnPosition),
            0f,
            Random.Range(-randomJitterOnPosition, randomJitterOnPosition)
        );
    }

    private void RebuildWaypoints()
    {
        waypoints.Clear();

        if (waypointsHolder == null)
        {
            nextWaypoint = null;
            nextWaypointPosition = transform.position;
            return;
        }

        foreach (Transform child in waypointsHolder.GetComponentsInChildren<Transform>())
        {
            if (child != waypointsHolder)
            {
                waypoints.Add(child);
            }
        }

        if (waypoints.Count > 0)
        {
            nextWaypoint = waypoints[0];
            nextWaypointPosition = nextWaypoint.position;
        }
        else
        {
            nextWaypoint = null;
            nextWaypointPosition = transform.position;
        }
    }

    private void OnDrawGizmos()
    {
        SensorSnapshot sensors = Application.isPlaying ? latestSensors : ReadSensors();

        if (sensors.centerDirection == Vector3.zero)
        {
            return;
        }

        DrawGizmoRay(sensors.origin, sensors.centerDirection, sensors.centerHit, sensors.centerDistance);
        DrawGizmoRay(sensors.origin, sensors.leftDirection, sensors.leftHit, sensors.leftDistance);
        DrawGizmoRay(sensors.origin, sensors.rightDirection, sensors.rightHit, sensors.rightDistance);
    }

    private void DrawGizmoRay(Vector3 origin, Vector3 direction, bool hit, float distance)
    {
        Gizmos.color = hit ? Color.red : Color.green;
        Gizmos.DrawLine(origin, origin + direction.normalized * distance);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableDebugLogs || collision == null || collision.collider == null)
        {
            return;
        }

        GameObject hitObject = collision.collider.gameObject;
        Debug.Log($"[AIControls] '{name}' collided with '{hitObject.name}' on layer '{LayerMask.LayerToName(hitObject.layer)}'.");
    }
}
