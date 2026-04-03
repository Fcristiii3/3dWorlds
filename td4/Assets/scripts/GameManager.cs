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
    public GameObject introSkipUI;

    public AudioSource audioSource;
    public AudioClip lowBeep;
    public AudioClip highBeep;

    private ProceduralFlyoverCamera proceduralFlyoverCamera;
    private bool introActive;
    private bool countdownStarted;

    void Awake()
    {
        Planet2RaceBootstrap planet2RaceBootstrap = GetComponent<Planet2RaceBootstrap>();
        if (planet2RaceBootstrap == null)
        {
            planet2RaceBootstrap = FindFirstObjectByType<Planet2RaceBootstrap>();
        }

        if (planet2RaceBootstrap != null)
        {
            planet2RaceBootstrap.TryPrepare(this);
        }

        // Start with the intro cinematic
        StartIntro();
    }

    void Update()
    {
        if (!introActive || countdownStarted)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SkipIntro();
        }
    }

    public void StartIntro()
    {
        introActive = true;
        countdownStarted = false;

        if (introSkipUI != null)
        {
            introSkipUI.SetActive(true);
        }

        // 1. Disable player following so the animator can move the camera
        if (followPlayerCamera != null)
        {
            followPlayerCamera.enabled = false;
        }

        proceduralFlyoverCamera = ResolveProceduralFlyoverCamera();

        // 2. Use the procedural flyover when available
        if (proceduralFlyoverCamera != null && proceduralFlyoverCamera.BeginFlyover(this))
        {
            if (cameraIntroAnimator != null)
            {
                cameraIntroAnimator.enabled = false;
            }

            FreezePlayers(true);
            return;
        }

        // 3. Fall back to the legacy animator if no procedural path is available
        if (cameraIntroAnimator != null)
        {
            cameraIntroAnimator.enabled = true;
        }

        // 4. Ensure cars are frozen during the cinematic
        FreezePlayers(true);

        if (cameraIntroAnimator == null)
        {
            StartCountdown();
        }
    }

    public void SkipIntro()
    {
        if (!introActive || countdownStarted)
        {
            return;
        }

        StartCountdown();
    }

    // This method will be triggered by the Animation Event
    public void StartCountdown()
    {
        if (countdownStarted)
        {
            return;
        }

        countdownStarted = true;
        introActive = false;

        if (introSkipUI != null)
        {
            introSkipUI.SetActive(false);
        }

        // 1. Disable the animator so it doesn't fight the follow script
        if (cameraIntroAnimator != null)
        {
            cameraIntroAnimator.enabled = false;
        }

        if (proceduralFlyoverCamera != null)
        {
            proceduralFlyoverCamera.StopFlyover();
        }

        // 2. Re-enable and snap the follow script for racing
        if (followPlayerCamera != null)
        {
            followPlayerCamera.enabled = true;

            followPlayer followCamera = followPlayerCamera as followPlayer;
            if (followCamera != null)
            {
                followCamera.SnapToTarget();
            }
        }

        // 3. Begin the beep countdown
        StartCoroutine("Countdown");
    }

    private ProceduralFlyoverCamera ResolveProceduralFlyoverCamera()
    {
        if (proceduralFlyoverCamera != null)
        {
            return proceduralFlyoverCamera;
        }

        proceduralFlyoverCamera = FindFirstObjectByType<ProceduralFlyoverCamera>();
        if (proceduralFlyoverCamera != null)
        {
            return proceduralFlyoverCamera;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            proceduralFlyoverCamera = mainCamera.GetComponent<ProceduralFlyoverCamera>();
        }

        return proceduralFlyoverCamera;
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(1);
        Debug.Log("3");
        if (tricolorLights != null) tricolorLights.SetProgress(1);
        if (audioSource != null && lowBeep != null) audioSource.PlayOneShot(lowBeep);
        yield return new WaitForSeconds(1);
        Debug.Log("2");
        if (tricolorLights != null) tricolorLights.SetProgress(2);
        if (audioSource != null && lowBeep != null) audioSource.PlayOneShot(lowBeep);
        yield return new WaitForSeconds(1);
        Debug.Log("1");
        if (tricolorLights != null) tricolorLights.SetProgress(3);
        if (audioSource != null && lowBeep != null) audioSource.PlayOneShot(lowBeep);
        yield return new WaitForSeconds(1);
        Debug.Log("GO");
        if (tricolorLights != null) tricolorLights.SetProgress(4);
        if (audioSource != null && highBeep != null) audioSource.PlayOneShot(highBeep);

        StartRacing();

        yield return new WaitForSeconds(2f);
        if (tricolorLights != null) tricolorLights.SetAllLightsOff();
    }

    public void StartRacing()
    {
        FreezePlayers(false);
    }

    public void SetRaceFrozen(bool freeze)
    {
        FreezePlayers(freeze);
    }

    void FreezePlayers(bool freeze)
    {
        bool isEnabled = !freeze;

        if (playerControls != null)
        {
            playerControls.enabled = isEnabled;
            if (freeze) StopCar(playerControls.GetComponent<Rigidbody>());
        }

        if (aiControls == null)
        {
            return;
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
