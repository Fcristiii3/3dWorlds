using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float lookSensitivity = 2f;
    public float gravity = -9.81f;
    
    private CharacterController controller;
    private Camera playerCamera;
    private float verticalRotation = 0f;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        Cursor.lockState = CursorLockMode.Locked; 
    }

    void Update()
    {
        // mouse look
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // MOVEMENT
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        
        // --- THE NEW FIX ---
        // This stops the vector from ever growing longer than 1!
        move = Vector3.ClampMagnitude(move, 1f); 
        // -------------------

        controller.Move(move * walkSpeed * Time.deltaTime);

        // 3. Gravity (The Trapdoor Mechanic)
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // stick to the floor
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime); // falling math
    }
}