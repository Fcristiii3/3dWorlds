using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class GameButton : MonoBehaviour
{
    [Header("Settings")]
    public AudioSource clickSound;
    public float pushDepth = 0.1f; 

    [Header("What happens when pressed?")]
    public UnityEvent onButtonPressed;

    private Vector3 originalPosition;
    private bool isPushing = false;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    public void Press()
    {
        if (!isPushing)
        {
            StartCoroutine(ButtonPushAnimation());
        }
    }

    IEnumerator ButtonPushAnimation()
    {
        isPushing = true;
        if (clickSound != null) clickSound.Play();

        transform.localPosition = originalPosition - new Vector3(pushDepth, 0f, 0f);
        yield return new WaitForSeconds(0.2f);

        transform.localPosition = originalPosition + new Vector3(pushDepth, 0f, 0f);

        onButtonPressed.Invoke();

        isPushing = false;
    }
}