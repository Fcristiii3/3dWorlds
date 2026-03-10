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

    void FixedUpdate()
    {
        // Accelerate (using input.y)
        float speed = input.y > 0 ? forwardMoveSpeed : backwardMoveSpeed;
        if (input.y == 0) speed = 0;

        rg.AddForce(this.transform.forward * speed, ForceMode.Acceleration);

        // Steer (using input.x)
        float rotation = input.x * steerSpeed * Time.fixedDeltaTime;
        transform.Rotate(0, rotation, 0, Space.World);
    }

    public float GetSpeed()
    {
        return rg.linearVelocity.magnitude;

    }

}