using UnityEngine;

public class CarMovement : MonoBehaviour
{
    public Rigidbody rg;
    public float forwardMoveSpeed;
    public float backwardMoveSpeed;
    public float steerSpeed;

    public Vector2 input;

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

        // Accelerate (using input.y)
        float speed = input.y > 0 ? forwardMoveSpeed : backwardMoveSpeed;
        if (input.y == 0) speed = 0;

        rg.AddForce(this.transform.forward * speed, ForceMode.Acceleration);

        // Clear collision-driven spin so seams and tiny collider hits cannot twist the car.
        rg.angularVelocity = Vector3.zero;

        // Steer (using input.x) using the original direct rotate for the snappy arcade feel.
        float rotation = input.x * steerSpeed * Time.fixedDeltaTime;
        transform.Rotate(0f, rotation, 0f, Space.World);
    }

    public float GetSpeed()
    {
        return rg.linearVelocity.magnitude;

    }

}
