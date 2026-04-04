using UnityEngine;

public class CarMovement : MonoBehaviour
{
    public Rigidbody rg;
    public float forwardMoveSpeed;
    public float backwardMoveSpeed;
    public float steerSpeed;

    [Header("Floaty Drift Assist")]
    [Min(0f)]
    public float driftMinSpeed = 6f;
    [Range(0f, 1f)]
    public float driftTurnThreshold = 0.35f;
    [Min(0f)]
    public float driftSlideForce = 14f;
    [Min(0f)]
    public float driftMomentumCarry = 6f;
    [Range(0.1f, 1f)]
    public float driftSteerMultiplier = 0.6f;
    [Min(0f)]
    public float driftForwardAssist = 4f;
    [Min(0f)]
    public float driftAssistMaxSpeed = 30f;

    [Range(0f, 1f)]
    public float bounceSpeedRetention = 0.8f;

    public Vector2 input;
    private Vector3 lastFrameVelocity;

    public void SetInputs(Vector2 inputVector)
    {
        input = inputVector;
    }

    private void Start()
    {
        if (rg == null)
        {
            rg = GetComponent<Rigidbody>();
        }

        if (rg == null)
        {
            return;
        }

        rg.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rg.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (rg == null)
        {
            return;
        }

        lastFrameVelocity = rg.linearVelocity;

        // Accelerate (using input.y)
        float speed = input.y > 0 ? forwardMoveSpeed : backwardMoveSpeed;
        if (input.y == 0) speed = 0;

        rg.AddForce(this.transform.forward * speed, ForceMode.Acceleration);
        ApplyFloatyDriftAssist();

        // Clear collision-driven spin so seams and tiny collider hits cannot twist the car.
        rg.angularVelocity = Vector3.zero;

        // Steer (using input.x) using the original direct rotate for the snappy arcade feel.
        float steerMultiplier = IsInLowGripDrift(out _) ? driftSteerMultiplier : 1f;
        float rotation = input.x * steerSpeed * steerMultiplier * Time.fixedDeltaTime;
        transform.Rotate(0f, rotation, 0f, Space.World);
    }

    public float GetSpeed()
    {
        return rg.linearVelocity.magnitude;
    }

    private void ApplyFloatyDriftAssist()
    {
        if (!IsInLowGripDrift(out float signedForwardSpeed))
        {
            return;
        }

        float forwardSpeed = Mathf.Abs(signedForwardSpeed);
        float driftStrength = Mathf.Abs(input.x);
        Vector3 localVelocity = transform.InverseTransformDirection(rg.linearVelocity);

        float driveDirection = Mathf.Sign(input.y);
        if (Mathf.Approximately(driveDirection, 0f))
        {
            driveDirection = Mathf.Sign(signedForwardSpeed);
        }

        if (Mathf.Approximately(driveDirection, 0f))
        {
            driveDirection = 1f;
        }

        rg.AddForce(transform.right * (input.x * driftSlideForce * driftStrength), ForceMode.Acceleration);

        float sidewaysDirection = Mathf.Abs(localVelocity.x) > 0.1f ? Mathf.Sign(localVelocity.x) : Mathf.Sign(input.x);
        if (!Mathf.Approximately(sidewaysDirection, 0f))
        {
            rg.AddForce(transform.right * (sidewaysDirection * driftMomentumCarry * driftStrength), ForceMode.Acceleration);
        }

        if (forwardSpeed < driftAssistMaxSpeed)
        {
            rg.AddForce(transform.forward * (driveDirection * driftForwardAssist * driftStrength), ForceMode.Acceleration);
        }
    }

    private bool IsInLowGripDrift(out float signedForwardSpeed)
    {
        signedForwardSpeed = Vector3.Dot(rg.linearVelocity, transform.forward);
        bool isTryingToDrive = Mathf.Abs(input.y) > 0.01f;
        bool isTurningAggressively = Mathf.Abs(input.x) >= driftTurnThreshold;
        float forwardSpeed = Mathf.Abs(signedForwardSpeed);

        return isTryingToDrive && isTurningAggressively && forwardSpeed >= driftMinSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rg == null || collision == null || collision.collider == null || collision.contactCount == 0)
        {
            return;
        }

        Debug.LogWarning("I just hit: " + collision.gameObject.name + " on Layer: " + LayerMask.LayerToName(collision.gameObject.layer));

        GameObject hitObject = collision.gameObject;
        bool hitWallLayer = hitObject.layer == LayerMask.NameToLayer("Wall");
        bool hitWallTag = hitObject.CompareTag("Wall");

        if (!hitWallLayer && !hitWallTag)
        {
            return;
        }

        Vector3 impactVelocity = lastFrameVelocity.sqrMagnitude > 0.01f ? lastFrameVelocity : rg.linearVelocity;
        if (impactVelocity.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Vector3 wallNormal = collision.contacts[0].normal;
        Vector3 reflectedVelocity = Vector3.Reflect(impactVelocity, wallNormal) * bounceSpeedRetention;

        rg.linearVelocity = reflectedVelocity;
        lastFrameVelocity = reflectedVelocity;
    }

}
