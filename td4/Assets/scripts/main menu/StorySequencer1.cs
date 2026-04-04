using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorySequencer2 : MonoBehaviour
{
    [Header("Story Elements")]
    public AudioSource storytellerAudio;

    [Header("Your 3 Scenes (Drag Canvas Groups here)")]
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
        yield return new WaitForSeconds(3f); 

        yield return StartCoroutine(FadeCanvas(scene2_PrincessWalle, 0f, 1f));
        yield return new WaitForSeconds(4f); 

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}