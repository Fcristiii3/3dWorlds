


using UnityEngine.Events; // needed to use UnityEvent
using UnityEngine; // as usual
public class SimpleCheckpoint : MonoBehaviour
{
    public UnityEvent<GameObject, SimpleCheckpoint> onCheckpointEnter;
    void OnTriggerEnter(Collider collider)
    {
        // if entering object is tagged as the Player
        if (collider.gameObject.tag == "Player")
        {
            // fire an event giving the entering gameObject and this checkpoint
            onCheckpointEnter.Invoke(collider.gameObject, this);
            Debug.Log("reached checkpoint");
        }
    }
}