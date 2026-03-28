using UnityEngine;

public class followPlayer : MonoBehaviour
{
    public Transform player;
    public Vector3 marginFromPlayer;
    public Vector3 rotationOffsetEuler = new Vector3(45f, 0f, 0f);

    void LateUpdate()
    {
        SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (player == null)
        {
            return;
        }

        Quaternion yawRotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
        transform.position = player.position + (yawRotation * marginFromPlayer);
        transform.rotation = yawRotation * Quaternion.Euler(rotationOffsetEuler);
    }
}
