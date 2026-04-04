using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BoidHunterSceneCreator
{
    [MenuItem("Tools/Boid Hunter/Create New 2D Scene")]
    public static void CreateBoidHunterScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera mainCamera = CreateMainCamera();
        BoidGameManager2D manager = CreateManager();
        CreateHunter(manager);
        CreateUI(manager);

        string scenePath = "Assets/Scenes/BoidHunter2D.unity";
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = mainCamera.gameObject;
        Debug.Log("BoidHunter2D scene created at " + scenePath);
    }

    private static Camera CreateMainCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera cameraComponent = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraComponent.orthographic = true;
        cameraComponent.orthographicSize = 14f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        return cameraComponent;
    }

    private static BoidGameManager2D CreateManager()
    {
        GameObject managerObject = new GameObject("BoidGameManager2D");
        BoidGameManager2D manager = managerObject.AddComponent<BoidGameManager2D>();
        manager.worldMin = new Vector2(-24f, -13f);
        manager.worldMax = new Vector2(24f, 13f);
        manager.startingBoidCount = 70;
        return manager;
    }

    private static void CreateHunter(BoidGameManager2D manager)
    {
        GameObject hunterObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hunterObject.name = "Hunter";
        hunterObject.transform.position = Vector3.zero;
        hunterObject.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        Collider hunterCollider = hunterObject.GetComponent<Collider>();
        if (hunterCollider != null)
        {
            Object.DestroyImmediate(hunterCollider);
        }

        HunterController2D hunterController = hunterObject.AddComponent<HunterController2D>();
        hunterController.manager = manager;
        manager.hunter = hunterController;
    }

    private static void CreateUI(BoidGameManager2D manager)
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();

        GameObject hudPanel = new GameObject("HudPanel");
        hudPanel.transform.SetParent(canvasObject.transform, false);
        RectTransform hudRect = hudPanel.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(20f, -20f);
        hudRect.sizeDelta = new Vector2(450f, 60f);
        hudPanel.SetActive(true);

        Text scoreText = CreateText("ScoreText", hudPanel.transform, "Eaten: 0   Remaining: 0", 28);
        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = Vector2.zero;
        scoreRect.sizeDelta = new Vector2(450f, 60f);
        scoreText.alignment = TextAnchor.UpperLeft;

        manager.scoreText = scoreText;
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
