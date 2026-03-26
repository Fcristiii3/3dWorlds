using UnityEngine;
using UnityEngine.Events; // <-- 1. We added this to use Unity Events!
using System.Collections;

public class GameButton : MonoBehaviour
{
    [Header("Settings")]
    public AudioSource clickSound;
    public float pushDepth = 0.1f; 

    [Header("What happens when pressed?")]
    public UnityEvent onButtonPressed; // <-- 2. This creates the magic list in your Inspector!

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

        // Push in
        transform.localPosition = originalPosition - new Vector3(pushDepth, 0f, 0f);
        yield return new WaitForSeconds(0.2f);

        // Use this if it pops OUT towards you!
        transform.localPosition = originalPosition + new Vector3(pushDepth, 0f, 0f);

        // --- 3. THIS FIRES WHATEVER YOU PUT IN THE INSPECTOR LIST! ---
        onButtonPressed.Invoke();
        // -------------------------------------------------------------

        isPushing = false;
    }
}