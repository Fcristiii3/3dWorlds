using UnityEngine;
using System.Collections; 

public class HorrorEventTrigger : MonoBehaviour
{
    [Header("The Trap")]
    public GameObject invisibleBlockerWall; 
    public GameObject floorToDestroy;       

    [Header("The Scare")]
    public GameObject flowerModel;
    public Light flowerLight;               
    public AudioSource scareSound;          
    public Animator flowerAnimator;         
    
    [Tooltip("How many seconds to wait before the floor drops?")]
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
        // turn on invisible wall
        if (invisibleBlockerWall != null) invisibleBlockerWall.SetActive(true);

        // playsound
        if (scareSound != null) scareSound.Play();

        // turn on light
        if (flowerLight != null) flowerLight.enabled = true;

        // trigger flower animation
        if (flowerAnimator != null) flowerAnimator.SetTrigger("PlayScare");

        // wait for ani to finish
        yield return new WaitForSeconds(fallDelay);

        // DROP!
        if (floorToDestroy != null) floorToDestroy.SetActive(false);
    }
}