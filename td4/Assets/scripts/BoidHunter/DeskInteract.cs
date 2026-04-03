using UnityEngine;
using UnityEngine.SceneManagement; 

public class DeskInteract : MonoBehaviour
{
    public float interactionDistance = 20.0f; 
    public string sceneToLoad;              
    public Transform player;                 

    void OnMouseDown()
    {
        // Calculate distance between player and desk
        float dist = Vector3.Distance(player.position, transform.position);

        if (dist <= interactionDistance)
        {
            Debug.Log("Loading next scene...");
            //SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.Log("Too far away!");
        }
    }
}