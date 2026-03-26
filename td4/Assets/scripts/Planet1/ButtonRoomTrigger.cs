using UnityEngine;
using System.Collections;

public class ButtonRoomTrigger : MonoBehaviour
{
    [Header("The Trap")]
    public GameObject invisibleBlockerWall; // <-- NEW: The door blocks behind them
    public GameObject floorToDrop;
    
    [Header("The Actor (Flower)")]
    public GameObject flowerModel; 
    public Animator characterAnimator;
    public string animationTriggerName = "WakeUp";

    [Header("The Lights")]
    public GameObject playerFlashlight; 
    public float timeInDarkness = 2f; 

    [Header("The Audio")]
    public AudioSource roomSound; 

    [Header("Timing")]
    public float timeBeforeDrop = 3f; 
    
    [Tooltip("How many seconds does it take the player to hit the ground?")]
    public float timeFalling = 1.5f; // <-- NEW: The fall timer

    private bool hasTriggered = false;

    public void StartRoomEvent()
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            
            // 1. Trap them immediately!
            if (invisibleBlockerWall != null) invisibleBlockerWall.SetActive(true);

            // 2. Kill the flashlight!
            if (playerFlashlight != null) playerFlashlight.SetActive(false);

            // 3. Play the room sound
            if (roomSound != null) roomSound.Play();

            // 4. Start the timeline
            StartCoroutine(RoomSequence());
        }
    }

    IEnumerator RoomSequence()
    {
        // 5. Reveal the flower in the dark
        if (flowerModel != null) flowerModel.SetActive(true);

        // 6. Start the creepy flower animation
        if (characterAnimator != null) characterAnimator.SetTrigger(animationTriggerName);

        // 7. Wait in the pitch black
        yield return new WaitForSeconds(timeInDarkness);

        // 8. SNAP THE FLASHLIGHT BACK ON!
        if (playerFlashlight != null) playerFlashlight.SetActive(true);

        // 9. Wait while the player watches in horror
        yield return new WaitForSeconds(timeBeforeDrop);

        // 10. Drop the floor AND kill the light again!
        if (floorToDrop != null) floorToDrop.SetActive(false);
        if (playerFlashlight != null) playerFlashlight.SetActive(false);

        // 11. Wait for them to hit the bottom in the dark
        yield return new WaitForSeconds(timeFalling);

        // 12. Turn the flashlight back on for the next room!
        if (playerFlashlight != null) playerFlashlight.SetActive(true);
    }
}