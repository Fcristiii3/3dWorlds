using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("How close do you need to be to press a button?")]
    public float interactDistance = 3f;

    void Update()
    {
        // 0 = Left Click, 1 = Right Click, 2 = Middle Click
        if (Input.GetMouseButtonDown(0))
        {
            // 1. Create an invisible laser shooting straight out of the camera
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            // 2. Shoot the laser! Did it hit anything within 3 meters?
            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                // 3. Check if the object we hit has the 'GameButton' script on it
                GameButton button = hit.collider.GetComponent<GameButton>();
                
                if (button != null)
                {
                    // WE HIT A BUTTON! Press it!
                    button.Press();
                }
            }
        }
    }
}