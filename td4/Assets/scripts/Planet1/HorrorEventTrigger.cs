using UnityEngine;
using System.Collections; 

public class HorrorEventTrigger : MonoBehaviour
{
    [Header("The Trap")]
    public GameObject invisibleBlockerWall; 
    public GameObject floorToDestroy;       
    public GameObject playerFlashlight;     

    [Header("The Scare")]
    public GameObject flowerModel;          
    public AudioSource scareSound;          
    public Animator flowerAnimator;         
    
    [Tooltip("Seconds to wait BEFORE the sound starts (Does NOT delay the light)")]
    public float soundDelay = 0.5f;

    [Tooltip("Seconds of silence AFTER the flashlight dies before the monster wakes up")]
    public float pauseBeforeScare = 1.5f;

    [Tooltip("Seconds to wait while the monster animates in the dark before turning on the light")]
    public float lightDelay = 0.5f;

    [Tooltip("Seconds to wait before the floor drops")]
    public float fallDelay = 4f;            

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(ExecuteTrapSequence());
        }
    }

    IEnumerator ExecuteTrapSequence()
    {
        if (invisibleBlockerWall != null) invisibleBlockerWall.SetActive(true);
        
        if (playerFlashlight != null) playerFlashlight.SetActive(false);
        
        if (scareSound != null) scareSound.PlayDelayed(soundDelay);

        yield return new WaitForSeconds(pauseBeforeScare);

        if (flowerModel != null) flowerModel.SetActive(true);
        if (flowerAnimator != null) flowerAnimator.SetTrigger("PlayScare");

        yield return new WaitForSeconds(lightDelay);

        if (playerFlashlight != null) playerFlashlight.SetActive(true);

        yield return new WaitForSeconds(fallDelay);

        if (floorToDestroy != null) floorToDestroy.SetActive(false);
    }
}