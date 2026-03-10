using UnityEngine;
using UnityEngine.Events;

public class PlayerControl : MonoBehaviour
{
    public Rigidbody rg;
    public float forwardMoveSpeed;
    public float backwardMoveSpeed;
    public float steerSpeed;

    private float inputX;
    private float inputY;

    public UnityEvent<Vector2> onInput;
    void Update() // Get keyboard inputs
    {
        inputY = Input.GetAxis("Vertical");
        inputX = Input.GetAxis("Horizontal");

        Vector2 input = new Vector2(inputX, inputY).normalized;

        onInput.Invoke(input);

    }

}