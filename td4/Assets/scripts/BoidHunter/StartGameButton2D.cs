using UnityEngine;
using UnityEngine.UI;

public class StartGameButton2D : MonoBehaviour
{
    public BoidGameManager2D manager;
    public GameObject startPanel;
    public GameObject hudPanel;

    private Button startButton;

    private void Awake()
    {
        startButton = GetComponent<Button>();
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
            startButton.onClick.AddListener(HandleStartClicked);
        }

        if (manager == null)
        {
            manager = FindObjectOfType<BoidGameManager2D>();
        }

        if (startPanel == null)
        {
            GameObject foundStartPanel = GameObject.Find("StartPanel");
            if (foundStartPanel != null)
            {
                startPanel = foundStartPanel;
            }
        }

        if (hudPanel == null)
        {
            GameObject foundHudPanel = GameObject.Find("HudPanel");
            if (foundHudPanel != null)
            {
                hudPanel = foundHudPanel;
            }
        }
    }

    private void HandleStartClicked()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<BoidGameManager2D>();
        }

        if (manager != null)
        {
            manager.StartGame();
        }

        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }

        if (hudPanel != null)
        {
            hudPanel.SetActive(true);
        }
    }
}
