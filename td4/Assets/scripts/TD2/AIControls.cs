using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AIControls : MonoBehaviour
{
    private Vector2 input;
    public UnityEvent<Vector2> onInput;

    public Transform waypointsHolder;
    private List<Transform> waypoints;
    private Transform nextWaypoint;
    private Vector3 nextWaypointPosition;

    public float maxDistanceToTarget = 5f;
    public float maxDistanceToReverse = 10f;

    public float randomJitterOnPosition = .5f;

    void Awake()
    {
        RebuildWaypoints();
    }

    void Start()
    {
        if (waypoints != null && waypoints.Count > 0)
        {
            SelectWaypoint(waypoints[0]);
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count == 0 || nextWaypoint == null)
        {
            input = Vector2.zero;
            onInput?.Invoke(input);
            return;
        }

        // Change to next waypoint if reached current waypoint
        float distanceToTarget = Vector3.Distance(transform.position, nextWaypointPosition);
        if (distanceToTarget < maxDistanceToTarget)
        {
            int nextIndex = waypoints.IndexOf(nextWaypoint) + 1;
            SelectWaypoint(nextIndex < waypoints.Count ? waypoints[nextIndex] : waypoints[0]);
        }

        // Compute Vector2 input based on distances in Right and Forward axis
        Vector3 diff = nextWaypointPosition - transform.position;
        float componentForward = Vector3.Dot(diff, transform.forward.normalized);
        float componentRight = Vector3.Dot(diff, transform.right.normalized);
        input = new Vector2(componentRight, componentForward).normalized;

        // If target behind but too far, turn around
        if (componentForward < 0 && distanceToTarget > maxDistanceToReverse)
        {
            input.y = 1f;
            input.x = Mathf.Sign(componentRight) * 1f;
        }
        onInput?.Invoke(input);
    }

    public void SetWaypointsHolder(Transform newWaypointsHolder)
    {
        waypointsHolder = newWaypointsHolder;
        RebuildWaypoints();

        if (waypoints != null && waypoints.Count > 0)
        {
            SelectWaypoint(waypoints[0]);
        }
    }

    void SelectWaypoint(Transform waypoint)
    {
        if (waypoint == null)
        {
            nextWaypoint = null;
            nextWaypointPosition = transform.position;
            return;
        }

        nextWaypoint = waypoint;
        nextWaypointPosition = nextWaypoint.position + new Vector3(Random.Range(-randomJitterOnPosition, randomJitterOnPosition), 0, Random.Range(-randomJitterOnPosition, randomJitterOnPosition));
    }

    private void RebuildWaypoints()
    {
        if (waypoints == null)
        {
            waypoints = new List<Transform>();
        }
        else
        {
            waypoints.Clear();
        }

        if (waypointsHolder == null)
        {
            nextWaypoint = null;
            nextWaypointPosition = transform.position;
            return;
        }

        foreach (Transform t in waypointsHolder.GetComponentsInChildren<Transform>())
        {
            if (t != waypointsHolder)
            {
                waypoints.Add(t);
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
}
