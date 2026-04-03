using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("The Player")]
    public CharacterController controller; 
    public AudioSource audioSource;

    [Header("The Sounds")]
    public AudioClip footstepSound1; 
    public AudioClip footstepSound2; 
    public AudioClip landingSound;

    [Header("Settings")]
    [Tooltip("How far does the player need to walk to trigger a step?")]
    public float stepDistance = 1.5f; 
    
    private float distanceTraveled = 0f;
    private bool wasInAir = false;
    private bool isLeftFoot = true;
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        Vector3 currentFlatPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 lastFlatPos = new Vector3(lastPosition.x, 0, lastPosition.z);
        float distanceMoved = Vector3.Distance(currentFlatPos, lastFlatPos);
        
        lastPosition = transform.position;

        {
            audioSource.pitch = 1f; 
            if (landingSound != null) audioSource.PlayOneShot(landingSound);
            wasInAir = false; 
        }

        if (!controller.isGrounded)
        {
            wasInAir = true; 
        }

        if (controller.isGrounded && distanceMoved > 0.001f)
        {
            distanceTraveled += distanceMoved;
            
            if (distanceTraveled >= stepDistance)
            {
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                
                AudioClip clipToPlay = isLeftFoot ? footstepSound1 : footstepSound2;
                if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay, 0.2f);
                
                isLeftFoot = !isLeftFoot;
                
                distanceTraveled = 0f; 
            }
        }
    }
}