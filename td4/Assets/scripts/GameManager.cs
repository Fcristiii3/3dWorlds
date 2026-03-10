using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public PlayerControl playerControls;
    public AIControls[] aiControls;
    public LapManager lapTracker;
    public TricolorLights tricolorLights;

    // --- New Variables for Camera Animation ---
    public Animator cameraIntroAnimator;
    public MonoBehaviour followPlayerCamera; // Reference to the camera's follow script

    public AudioSource audioSource;
    public AudioClip lowBeep;
    public AudioClip highBeep;

    void Awake()
    {
        // Start with the intro cinematic
        StartIntro();
    }

    public void StartIntro()
    {
        // 1. Disable player following so the animator can move the camera
        followPlayerCamera.enabled = false;
        // 2. Enable the animator to play the clip
        cameraIntroAnimator.enabled = true;
        // 3. Ensure cars are frozen during the cinematic
        FreezePlayers(true);
    }

    // This method will be triggered by the Animation Event
    public void StartCountdown()
    {
        // 1. Re-enable the follow script for racing
        followPlayerCamera.enabled = true;
        // 2. Disable the animator so it doesn't fight the follow script
        cameraIntroAnimator.enabled = false;

        // 3. Begin the beep countdown
        StartCoroutine("Countdown");
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(1);
        Debug.Log("3");
        tricolorLights.SetProgress(1);
        audioSource.PlayOneShot(lowBeep);
        yield return new WaitForSeconds(1);
        Debug.Log("2");
        tricolorLights.SetProgress(2);
        audioSource.PlayOneShot(lowBeep);
        yield return new WaitForSeconds(1);
        Debug.Log("1");
        tricolorLights.SetProgress(3);
        audioSource.PlayOneShot(lowBeep);
        yield return new WaitForSeconds(1);
        Debug.Log("GO");
        tricolorLights.SetProgress(4);
        audioSource.PlayOneShot(highBeep);

        StartRacing();

        yield return new WaitForSeconds(2f);
        tricolorLights.SetAllLightsOff();
    }

    public void StartRacing()
    {
        FreezePlayers(false);
    }

    void FreezePlayers(bool freeze)
    {
        bool isEnabled = !freeze;

        if (playerControls != null)
        {
            playerControls.enabled = isEnabled;
            if (freeze) StopCar(playerControls.GetComponent<Rigidbody>());
        }

        foreach (AIControls ai in aiControls)
        {
            if (ai != null)
            {
                ai.enabled = isEnabled;
                if (freeze) StopCar(ai.GetComponent<Rigidbody>());
            }
        }
    }

    // Add this helper method to your GameManager script
    void StopCar(Rigidbody rb)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Stops forward/backward movement
            rb.angularVelocity = Vector3.zero; // Stops the "spinning" or "burnout" rotation
            rb.Sleep(); // Forces the physics engine to ignore the object until it's "woken up"
        }
    }
}