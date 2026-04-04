using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorySequencer3 : MonoBehaviour
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
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FadeCanvas(scene2_PrincessWalle, 0f, 1f));

        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(FadeCanvas(scene3_SoloPrincess, 0f, 1f));
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(SnapTiltCanvas(scene3_SoloPrincess, 2, 15f, 1f));
        yield return new WaitForSeconds(1f);
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

    private IEnumerator SnapTiltCanvas(CanvasGroup cg, int tiltCycles, float angle, float delay)
    {
        if (cg == null) yield break;

        RectTransform rect = cg.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Quaternion originalRot = rect.localRotation;

        for (int i = 0; i < tiltCycles; i++)
        {
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            yield return new WaitForSeconds(delay);

            rect.localRotation = Quaternion.Euler(0, 0, -angle);
            yield return new WaitForSeconds(delay);
        }

        rect.localRotation = originalRot;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}