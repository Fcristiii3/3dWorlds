using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    [Header("Next Level")]
    [Tooltip("Type the EXACT name of your 2nd game's scene here!")]
    public string nameOfNextScene;

    public void LoadNextGame()
    {
        Debug.Log("Teleporting to: " + nameOfNextScene);
        SceneManager.LoadScene(nameOfNextScene);
    }
}