using UnityEngine;
using System.Collections;

public class DropRoomTrigger : MonoBehaviour
{
    [Header("The Actor")]
    public Animator characterAnimator;
    public string animationTriggerName = "WakeUp";

    [Header("The Audio")]
    public AudioSource roomSound; // <-- New slot for your speaker!

    [Header("The Next Trapdoor")]
    public GameObject floorToDrop;
    
    [Tooltip("How long should they watch the animation before falling again?")]
    public float timeBeforeDrop = 5f; 

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            // 0. Play the sound the exact millisecond they hit the floor!
            if (roomSound != null)
            {
                roomSound.Play();
            }

            StartCoroutine(RoomSequence());
        }
    }

    IEnumerator RoomSequence()
    {
        // 1. Play the creepy animation!
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(animationTriggerName);
        }

        // 2. Wait while the player watches in horror...
        yield return new WaitForSeconds(timeBeforeDrop);

        // 3. Drop the floor to the NEXT level!
        if (floorToDrop != null)
        {
            floorToDrop.SetActive(false);
        }
    }
}