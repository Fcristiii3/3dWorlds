using UnityEngine;
using UnityEngine.Events;

public class Checkpoint : MonoBehaviour
{
    // The LapManager listens for this specific event
    [HideInInspector]
    public UnityEvent<CarIdentity, Checkpoint> onCheckpointEnter = new UnityEvent<CarIdentity, Checkpoint>();

    private void OnTriggerEnter(Collider other)
    {
        // Try to find the CarIdentity on the object that entered the trigger
        CarIdentity car = other.GetComponentInParent<CarIdentity>();
        if (car != null)
        {
            onCheckpointEnter.Invoke(car, this);
        }
    }
}