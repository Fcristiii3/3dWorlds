using UnityEngine;
using UnityEngine.SceneManagement; // <-- CRITICAL: This is Unity's teleporter!

public class LevelLoader : MonoBehaviour
{
    [Header("Next Level")]
    [Tooltip("Type the EXACT name of your 2nd game's scene here!")]
    public string nameOfNextScene;

    // The button will press this function
    public void LoadNextGame()
    {
        Debug.Log("Teleporting to: " + nameOfNextScene);
        SceneManager.LoadScene(nameOfNextScene);
    }
}