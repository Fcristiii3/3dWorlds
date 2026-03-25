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

            // 1. Wake up Wall-E
            if (walleAnimator != null) walleAnimator.SetTrigger(animationTriggerName);
            
            // 2. Wake up Eve
            if (eveAnimator != null) eveAnimator.SetTrigger(animationTriggerName);

            if (ballAnimator != null) ballAnimator.SetTrigger(animationTriggerName);

            // 3. Play the creepy laughing sound
            if (creepySound != null) creepySound.Play();
        }
    }
}