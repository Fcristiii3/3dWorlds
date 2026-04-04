using UnityEngine;

public enum Planet2TrackPieceType
{
    Straight,
    Curve,
    StartFinish
}

public class Planet2TrackPieceAuthoring : MonoBehaviour
{
    public Planet2TrackPieceType pieceType = Planet2TrackPieceType.Straight;
    public Transform attachIn;
    public Transform attachOut;

    [Min(0f)]
    public float logicalLength;

    [Min(0f)]
    public float logicalWidth;

    public float GetLogicalLength()
    {
        if (logicalLength > 0f)
        {
            return logicalLength;
        }

        Bounds bounds = ComputeLocalBounds();
        return Mathf.Max(bounds.size.x, bounds.size.z);
    }

    public float GetLogicalWidth()
    {
        if (logicalWidth > 0f)
        {
            return logicalWidth;
        }

        Bounds bounds = ComputeLocalBounds();
        return Mathf.Min(bounds.size.x, bounds.size.z);
    }

    public bool HasAttachPoints()
    {
        return attachIn != null && attachOut != null;
    }

    private Bounds ComputeLocalBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
        Bounds localBounds = new Bounds(worldToLocal.MultiplyPoint3x4(renderers[0].bounds.center), Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 center = worldToLocal.MultiplyPoint3x4(worldBounds.center);
            Vector3 extents = worldBounds.extents;

            Vector3[] corners =
            {
                center + new Vector3( extents.x,  extents.y,  extents.z),
                center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z),
                center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z),
                center + new Vector3(-extents.x, -extents.y, -extents.z)
            };

            foreach (Vector3 corner in corners)
            {
                localBounds.Encapsulate(corner);
            }
        }

        return localBounds;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Bounds bounds = ComputeLocalBounds();
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        Gizmos.matrix = previousMatrix;

        DrawAttachPoint(attachIn, Color.green);
        DrawAttachPoint(attachOut, Color.red);
    }

    private static void DrawAttachPoint(Transform attachPoint, Color color)
    {
        if (attachPoint == null)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawSphere(attachPoint.position, 0.15f);
        Gizmos.DrawLine(attachPoint.position, attachPoint.position + attachPoint.forward * 1.2f);
    }
}
