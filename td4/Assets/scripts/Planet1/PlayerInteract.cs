using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("How close do you need to be to press a button?")]
    public float interactDistance = 3f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                GameButton button = hit.collider.GetComponent<GameButton>();
                
                if (button != null)
                {
                    button.Press();
                }
            }
        }
    }
}