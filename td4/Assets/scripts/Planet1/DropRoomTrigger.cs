using UnityEngine;
using System.Collections;

public class DropRoomTrigger : MonoBehaviour
{
    [Header("The Actor")]
    public Animator characterAnimator;
    public string animationTriggerName = "WakeUp";

    [Header("The Audio")]
    public AudioSource roomSound; 

    [Header("The Trapdoor")]
    public GameObject floorToDrop;
    
    [Header("The Lights")]
    public GameObject playerFlashlight; // <-- NEW: Slot for your flashlight!
    
    [Header("Timing")]
    [Tooltip("How long should they watch the animation before falling again?")]
    public float timeBeforeDrop = 5f; 

    [Tooltip("How many seconds does it take the player to hit the ground?")]
    public float timeFalling = 1.5f; // <-- NEW: The fall timer!

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            // 1. Play the sound the exact millisecond they hit the floor!
            if (roomSound != null)
            {
                roomSound.Play();
            }

            StartCoroutine(RoomSequence());
        }
    }

    IEnumerator RoomSequence()
    {
        // 2. Play the creepy animation!
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(animationTriggerName);
        }

        // 3. Wait while the player watches in horror...
        yield return new WaitForSeconds(timeBeforeDrop);

        // 4. Drop the floor to the NEXT level AND kill the light!
        if (floorToDrop != null) floorToDrop.SetActive(false);
        if (playerFlashlight != null) playerFlashlight.SetActive(false);

        // 5. Wait for them to hit the bottom in the pitch black
        yield return new WaitForSeconds(timeFalling);

        // 6. Turn the flashlight back on when they land!
        if (playerFlashlight != null) playerFlashlight.SetActive(true);
    }
}