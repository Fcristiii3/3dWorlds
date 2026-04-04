using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorySequencer4 : MonoBehaviour
{
    [Header("Story Elements")]
    public AudioSource storytellerAudio;

    [Header("Scenes")]
    public CanvasGroup scene1_PrincePrincess;
    public CanvasGroup scene2_PrincessWalle;
    public CanvasGroup scene3_SoloPrincess;

    [Header("Settings")]
    public float fadeSpeed = 1.5f;
    public string gameSceneName = "YourGameplaySceneName";

    private void Start()
    {
        if (scene1_PrincePrincess != null) scene1_PrincePrincess.alpha = 0f;
        if (scene2_PrincessWalle != null) scene2_PrincessWalle.alpha = 0f;
        if (scene3_SoloPrincess != null) scene3_SoloPrincess.alpha = 0f;

        StartCoroutine(PlayStoryTimeline());
    }

    private IEnumerator PlayStoryTimeline()
    {
        if (storytellerAudio != null) storytellerAudio.Play();

        yield return StartCoroutine(FadeCanvas(scene1_PrincePrincess, 0f, 1f));
        yield return StartCoroutine(ZoomInAndFadeOut(scene1_PrincePrincess, 2f, 6f));
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FadeCanvas(scene2_PrincessWalle, 0f, 1f));

        yield return new WaitForSeconds(10f);
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float startAlpha, float endAlpha)
    {
        if (cg == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < fadeSpeed)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeSpeed);
            yield return null;
        }

        cg.alpha = endAlpha;
    }

    private IEnumerator ZoomInAndFadeOut(CanvasGroup cg, float duration, float targetScale)
    {
        if (cg == null) yield break;

        RectTransform rect = cg.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector3 originalScale = rect.localScale;
        Vector3 finalScale = new Vector3(targetScale, targetScale, 1f);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / duration;

            rect.localScale = Vector3.Lerp(originalScale, finalScale, percent);

            cg.alpha = Mathf.Lerp(1f, 0f, percent);

            yield return null;
        }

        rect.localScale = finalScale;
        cg.alpha = 0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}