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
        // 1. Trap them
        if (invisibleBlockerWall != null) invisibleBlockerWall.SetActive(true);
        
        // 2. KILL THE FLASHLIGHT!
        if (playerFlashlight != null) playerFlashlight.SetActive(false);
        
        // 3. Tell the sound to play in 0.5 seconds (This does NOT pause the script!)
        if (scareSound != null) scareSound.PlayDelayed(soundDelay);

        // 4. Wait in the darkness on our original timeline
        yield return new WaitForSeconds(pauseBeforeScare);

        // 5. Wake the flower up IN THE PITCH BLACK
        if (flowerModel != null) flowerModel.SetActive(true);
        if (flowerAnimator != null) flowerAnimator.SetTrigger("PlayScare");

        // 6. WAIT while the flower animates in the dark...
        yield return new WaitForSeconds(lightDelay);

        // 7. SNAP THE FLASHLIGHT BACK ON to reveal the monster!
        if (playerFlashlight != null) playerFlashlight.SetActive(true);

        // 8. Wait for the scare to finish
        yield return new WaitForSeconds(fallDelay);

        // 9. DROP THE FLOOR!
        if (floorToDestroy != null) floorToDestroy.SetActive(false);
    }
}