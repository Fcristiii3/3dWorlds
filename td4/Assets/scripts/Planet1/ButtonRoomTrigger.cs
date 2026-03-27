using UnityEngine;
using System.Collections;

public class ButtonRoomTrigger : MonoBehaviour
{
    [Header("The Trap")]
    public GameObject invisibleBlockerWall;
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
    public float timeFalling = 1.5f;

    private bool hasTriggered = false;

    public void StartRoomEvent()
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            
            if (invisibleBlockerWall != null) invisibleBlockerWall.SetActive(true);

            if (playerFlashlight != null) playerFlashlight.SetActive(false);

            if (roomSound != null) roomSound.Play();

            StartCoroutine(RoomSequence());
        }
    }

    IEnumerator RoomSequence()
    {
        if (flowerModel != null) flowerModel.SetActive(true);

        if (characterAnimator != null) characterAnimator.SetTrigger(animationTriggerName);

        yield return new WaitForSeconds(timeInDarkness);

        if (playerFlashlight != null) playerFlashlight.SetActive(true);

        yield return new WaitForSeconds(timeBeforeDrop);

        if (floorToDrop != null) floorToDrop.SetActive(false);
        if (playerFlashlight != null) playerFlashlight.SetActive(false);

        yield return new WaitForSeconds(timeFalling);

        if (playerFlashlight != null) playerFlashlight.SetActive(true);
    }
}