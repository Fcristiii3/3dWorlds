using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject planetSelectPanel;

    [Header("Levels")]
    public string planet1Scene = "Story_01";
    public string planet2Scene = "Story_02";
    public string planet3Scene = "Story_03";

    private void Start()
    {
        ShowMainMenu();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Story_01");
    }


    public void ShowPlanetSelect()
    {
        mainMenuPanel.SetActive(false); 
        planetSelectPanel.SetActive(true); 
    }

    public void ShowMainMenu()
    {
        planetSelectPanel.SetActive(false); 
        mainMenuPanel.SetActive(true);     
    }


    public void LoadPlanet1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(planet1Scene);
    }

    public void LoadPlanet2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(planet2Scene);
    }

    public void LoadPlanet3()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(planet3Scene);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}