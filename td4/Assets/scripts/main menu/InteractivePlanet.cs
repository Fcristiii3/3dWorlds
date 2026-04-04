using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems; // CRITICAL: Allows mouse detection on UI
using UnityEngine.SceneManagement;

public class InteractivePlanet : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Level Loading")]
    [Tooltip("Type the exact name of the scene this planet loads")]
    public string sceneToLoad;

    [Header("Hover Settings")]
    public float hoverScale = 1.2f;      
    public float animationSpeed = 5f;    
    public float bobSpeed = 1f;          
    public float bobHeight = 7f;       

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector2 originalPosition;

    private Coroutine scaleCoroutine;
    private Coroutine bobCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(SmoothScale(originalScale * hoverScale));

        if (bobCoroutine != null) StopCoroutine(bobCoroutine);
        bobCoroutine = StartCoroutine(BobUpDown());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(SmoothScale(originalScale));

        if (bobCoroutine != null)
        {
            StopCoroutine(bobCoroutine);
            rectTransform.anchoredPosition = originalPosition;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneToLoad);
    }


    private IEnumerator SmoothScale(Vector3 targetScale)
    {
        while (Vector3.Distance(rectTransform.localScale, targetScale) > 0.01f)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        rectTransform.localScale = targetScale;
    }

    private IEnumerator BobUpDown()
    {
        float elapsedTime = 0f;
        while (true) 
        {
            float newY = originalPosition.y + (Mathf.Sin(elapsedTime * bobSpeed) * bobHeight);
            rectTransform.anchoredPosition = new Vector2(originalPosition.x, newY);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}