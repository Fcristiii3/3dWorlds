using UnityEngine;

public class carController : MonoBehaviour
{
    public Rigidbody rg;
    public float forwardMoveSpeed;
    public float backwardMoveSpeed;
    public float steerSpeed;

    private float inputX;
    private float inputY;
    void Update() // Get keyboard inputs
    {
        inputY = Input.GetAxis("Vertical");
        inputX = Input.GetAxis("Horizontal");
    }

    void FixedUpdate() // Apply physics here
    {
        // Accelerate
        float speed = inputY > 0 ? forwardMoveSpeed : backwardMoveSpeed;
        if (inputY == 0) speed = 0;
        rg.AddForce(this.transform.forward * speed, ForceMode.Acceleration);
        // Steer
        float rotation = inputX * steerSpeed * Time.fixedDeltaTime;
        transform.Rotate(0, rotation, 0, Space.World);
        //Debug.Log("moving with:" + speed + " kmh and rotating with:" + rotation);
    }
}