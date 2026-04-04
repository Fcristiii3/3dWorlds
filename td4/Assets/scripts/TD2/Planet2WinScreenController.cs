using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Planet2WinScreenController : MonoBehaviour
{
    private GameManager gameManager;
    private GameObject canvasRoot;
    private GameObject panel;
    private Text titleText;
    private Text subtitleText;
    private Button restartButton;
    private Button nextPlanetButton;
    private bool isInitialized;
    private bool isShowing;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;

        if (isInitialized)
        {
            return;
        }

        CreateUi();
        isInitialized = true;
    }

    public void HandlePlayerFinished()
    {
        ShowEndScreen(true, null);
    }

    public void ShowEndScreen(bool didWin, DriftScoring driftScoring)
    {
        if (!isInitialized)
        {
            Initialize(gameManager != null ? gameManager : GetComponent<GameManager>());
        }

        if (isShowing)
        {
            return;
        }

        isShowing = true;

        if (titleText != null)
        {
            titleText.text = didWin ? "You Win" : "You Lose";
        }

        if (subtitleText != null)
        {
            subtitleText.text = BuildSubtitle(didWin, driftScoring);
        }

        if (nextPlanetButton != null)
        {
            nextPlanetButton.gameObject.SetActive(didWin);
        }

        if (gameManager != null)
        {
            gameManager.SetRaceFrozen(true);
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (restartButton != null)
        {
            restartButton.Select();
        }
    }

    public void RestartRace()
    {
        isShowing = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextPlanet()
    {
        Debug.Log("Next Planet button clicked. Wire this button to your next scene when ready.");
    }

    private void CreateUi()
    {
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        canvasRoot = new GameObject("Planet2WinCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(transform, false);

        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasRoot.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject title = CreateText("Title", panel.transform, defaultFont, "You Win", 58, FontStyle.Bold);
        titleText = title.GetComponent<Text>();
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(800f, 90f);
        titleRect.anchoredPosition = new Vector2(0f, 150f);

        GameObject subtitle = CreateText("Subtitle", panel.transform, defaultFont, "Choose what happens next.", 26, FontStyle.Normal);
        subtitleText = subtitle.GetComponent<Text>();
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        subtitleRect.sizeDelta = new Vector2(700f, 50f);
        subtitleRect.anchoredPosition = new Vector2(0f, 85f);

        restartButton = CreateButton("Restart", panel.transform, defaultFont, new Vector2(0f, -10f), new Color(0.18f, 0.52f, 0.2f));
        restartButton.onClick.AddListener(RestartRace);

        nextPlanetButton = CreateButton("Next Planet", panel.transform, defaultFont, new Vector2(0f, -110f), new Color(0.18f, 0.32f, 0.55f));
        nextPlanetButton.onClick.AddListener(NextPlanet);

        panel.SetActive(false);
    }

    private static GameObject CreateText(string name, Transform parent, Font font, string value, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return textObject;
    }

    private static Button CreateButton(string label, Transform parent, Font font, Vector2 anchoredPosition, Color color)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(340f, 72f);
        rectTransform.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.9f;
        colors.selectedColor = color * 1.05f;
        colors.disabledColor = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, color.a);
        button.colors = colors;
        button.targetGraphic = image;

        GameObject textObject = CreateText(label + "Label", buttonObject.transform, font, label, 28, FontStyle.Bold);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static string BuildSubtitle(bool didWin, DriftScoring driftScoring)
    {
        if (driftScoring == null)
        {
            return didWin ? "Target score reached. Choose what happens next." : "Target score missed. Try again.";
        }

        int totalScore = Mathf.RoundToInt(driftScoring.totalScore);
        int targetScore = Mathf.RoundToInt(driftScoring.targetScore);

        return didWin
            ? $"Target reached! Score: {totalScore} / {targetScore}"
            : $"Not enough points. Score: {totalScore} / {targetScore}";
    }
}
