using UnityEngine;

public class FinalRoomTrigger : MonoBehaviour
{
    [Header("The Actors")]
    public Animator walleAnimator;
    public Animator eveAnimator;
    public Animator ballAnimator;
    public string animationTriggerName = "WakeUp";

    [Header("The Audio")]
    public AudioSource creepySound;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (walleAnimator != null) walleAnimator.SetTrigger(animationTriggerName);
            
            if (eveAnimator != null) eveAnimator.SetTrigger(animationTriggerName);

            if (ballAnimator != null) ballAnimator.SetTrigger(animationTriggerName);

            if (creepySound != null) creepySound.Play();
        }
    }
}