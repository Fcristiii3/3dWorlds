using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ProceduralFlyoverCamera : MonoBehaviour
{
    [Min(0.1f)]
    public float introDuration = 10f;

    [Min(0.1f)]
    public float lookSmoothing = 8f;

    private readonly List<Vector3> waypoints = new List<Vector3>();
    private GameManager gameManager;
    private Vector3 lookTarget;
    private float elapsedTime;
    private bool isPlaying;

    public bool HasWaypoints => waypoints.Count >= 2;

    private void OnDisable()
    {
        isPlaying = false;
    }

    private void LateUpdate()
    {
        if (!isPlaying || waypoints.Count < 2)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsedTime / Mathf.Max(0.1f, introDuration));
        float segmentProgress = normalizedTime * (waypoints.Count - 1);
        int currentSegmentIndex = Mathf.Min(waypoints.Count - 2, Mathf.FloorToInt(segmentProgress));
        float currentSegmentT = Mathf.SmoothStep(0f, 1f, segmentProgress - currentSegmentIndex);

        Vector3 currentWaypoint = waypoints[currentSegmentIndex];
        Vector3 nextWaypoint = waypoints[currentSegmentIndex + 1];
        transform.position = Vector3.Lerp(currentWaypoint, nextWaypoint, currentSegmentT);

        Quaternion targetRotation = Quaternion.LookRotation((lookTarget - transform.position).normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSmoothing * Time.deltaTime);

        if (normalizedTime >= 1f)
        {
            CompleteFlyover();
        }
    }

    public void Configure(IReadOnlyList<Vector3> aerialWaypoints, Vector3 flyoverLookTarget)
    {
        waypoints.Clear();
        lookTarget = flyoverLookTarget;

        if (aerialWaypoints == null)
        {
            return;
        }

        for (int i = 0; i < aerialWaypoints.Count; i++)
        {
            waypoints.Add(aerialWaypoints[i]);
        }
    }

    public bool BeginFlyover(GameManager manager)
    {
        if (!HasWaypoints)
        {
            return false;
        }

        gameManager = manager;
        elapsedTime = 0f;
        isPlaying = true;
        enabled = true;

        transform.position = waypoints[0];
        transform.LookAt(lookTarget);
        return true;
    }

    public void StopFlyover()
    {
        isPlaying = false;
        enabled = false;
    }

    private void CompleteFlyover()
    {
        isPlaying = false;
        enabled = false;

        if (gameManager != null)
        {
            gameManager.StartCountdown();
        }
    }
}
