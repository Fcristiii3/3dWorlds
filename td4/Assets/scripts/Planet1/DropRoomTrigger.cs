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
    public GameObject playerFlashlight;
    
    [Header("Timing")]
    [Tooltip("How long should they watch the animation before falling again?")]
    public float timeBeforeDrop = 5f; 

    [Tooltip("How many seconds does it take the player to hit the ground?")]
    public float timeFalling = 1.5f;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            // play sound when hitting floor
            if (roomSound != null)
            {
                roomSound.Play();
            }

            StartCoroutine(RoomSequence());
        }
    }

    IEnumerator RoomSequence()
    {
        // play animation
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(animationTriggerName);
        }

        yield return new WaitForSeconds(timeBeforeDrop);

        // drop floor + kill light
        if (floorToDrop != null) floorToDrop.SetActive(false);
        if (playerFlashlight != null) playerFlashlight.SetActive(false);

        yield return new WaitForSeconds(timeFalling);

        if (playerFlashlight != null) playerFlashlight.SetActive(true);
    }
}