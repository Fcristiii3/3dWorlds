using UnityEngine;
using UnityEngine.SceneManagement; 

public class DeskInteract : MonoBehaviour
{
    public float interactionDistance = 20.0f; 
    public string sceneToLoad;              
    public Transform player;                 

    void OnMouseDown()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        if (dist <= interactionDistance)
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.Log("Too far away!");
        }
    }
}