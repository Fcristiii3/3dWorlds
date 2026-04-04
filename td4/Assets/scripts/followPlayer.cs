using UnityEngine;

[DisallowMultipleComponent]
public class followPlayer : MonoBehaviour
{
    public Transform player;

    [SerializeField] private float distance = 6f;
    [SerializeField] private float height = 2.5f;
    [SerializeField] private float positionDamping = 10f;
    [SerializeField] private float rotationDamping = 5f;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    private float currentYaw;

    private void OnEnable()
    {
        if (player != null)
        {
            currentYaw = player.eulerAngles.y;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        UpdateCameraPosition(Time.deltaTime, false);
    }

    public void SnapToTarget()
    {
        if (player == null)
        {
            return;
        }

        currentYaw = player.eulerAngles.y;
        UpdateCameraPosition(1f, true);
    }

    private void UpdateCameraPosition(float deltaTime, bool instant)
    {
        float targetYaw = player.eulerAngles.y;
        currentYaw = instant
            ? targetYaw
            : Mathf.LerpAngle(currentYaw, targetYaw, rotationDamping * deltaTime);

        Quaternion smoothedYawRotation = Quaternion.Euler(0f, currentYaw, 0f);
        Vector3 desiredPosition = player.position - (smoothedYawRotation * Vector3.forward * distance) + (player.up * height);

        transform.position = instant
            ? desiredPosition
            : Vector3.Lerp(transform.position, desiredPosition, positionDamping * deltaTime);

        transform.LookAt(player.position + lookAtOffset);
    }
}
