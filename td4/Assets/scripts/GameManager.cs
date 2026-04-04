using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public PlayerControl playerControls;
    public AIControls[] aiControls;
    public LapManager lapTracker;
    public TricolorLights tricolorLights;
    public Light[] racingLights;

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
    private Color[] originalRacingLightColors;
    private Light[] cachedRacingLightReferences;

    void Awake()
    {
        ResetRacingLights();

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
        ResetRacingLights();

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
        ResetRacingLights();
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
        SetCountdownLights(1);
        if (audioSource != null && lowBeep != null) audioSource.PlayOneShot(lowBeep);
        yield return new WaitForSeconds(1);
        Debug.Log("2");
        if (tricolorLights != null) tricolorLights.SetProgress(2);
        SetCountdownLights(2);
        if (audioSource != null && lowBeep != null) audioSource.PlayOneShot(lowBeep);
        yield return new WaitForSeconds(1);
        Debug.Log("1");
        if (tricolorLights != null) tricolorLights.SetProgress(3);
        SetCountdownLights(3);
        if (audioSource != null && lowBeep != null) audioSource.PlayOneShot(lowBeep);
        yield return new WaitForSeconds(1);
        Debug.Log("GO");
        if (tricolorLights != null) tricolorLights.SetProgress(4);
        SetCountdownLights(4);
        if (audioSource != null && highBeep != null) audioSource.PlayOneShot(highBeep);

        StartRacing();

        yield return new WaitForSeconds(2f);
        if (tricolorLights != null) tricolorLights.SetAllLightsOff();
        ResetRacingLights();
    }

    public void StartRacing()
    {
        Planet2RaceBootstrap planet2RaceBootstrap = GetComponent<Planet2RaceBootstrap>();
        if (planet2RaceBootstrap == null)
        {
            planet2RaceBootstrap = FindFirstObjectByType<Planet2RaceBootstrap>();
        }

        if (planet2RaceBootstrap != null)
        {
            planet2RaceBootstrap.ForceGameplayCameraHandoff(this);
        }

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

    private void ResetRacingLights()
    {
        if (racingLights == null)
        {
            return;
        }

        CacheRacingLightColorsIfNeeded();

        for (int i = 0; i < racingLights.Length; i++)
        {
            if (racingLights[i] != null)
            {
                if (originalRacingLightColors != null && i < originalRacingLightColors.Length)
                {
                    racingLights[i].color = originalRacingLightColors[i];
                }

                racingLights[i].enabled = false;
            }
        }
    }

    private void SetCountdownLights(int stage)
    {
        if (racingLights == null || racingLights.Length == 0)
        {
            return;
        }

        CacheRacingLightColorsIfNeeded();

        if (stage <= 0)
        {
            return;
        }

        if (stage == 1)
        {
            EnableRacingLight(0);
            return;
        }

        if (stage == 2)
        {
            EnableRacingLight(0);
            EnableRacingLight(1);
            return;
        }

        if (stage == 3)
        {
            EnableRacingLight(0);
            EnableRacingLight(1);
            EnableRacingLight(2);
            return;
        }

        if (stage == 4)
        {
            SetGoLightState();
        }
    }

    private void EnableRacingLight(int index)
    {
        if (racingLights == null || index < 0 || index >= racingLights.Length)
        {
            return;
        }

        Light racingLight = racingLights[index];
        if (racingLight == null)
        {
            return;
        }

        racingLight.enabled = true;
    }

    private void SetGoLightState()
    {
        if (racingLights == null)
        {
            return;
        }

        EnableRacingLight(0);
        EnableRacingLight(1);
        EnableRacingLight(2);

        for (int i = 0; i < 3 && i < racingLights.Length; i++)
        {
            if (racingLights[i] != null)
            {
                racingLights[i].color = Color.green;
            }
        }
    }

    private void CacheRacingLightColorsIfNeeded()
    {
        if (racingLights == null)
        {
            originalRacingLightColors = null;
            cachedRacingLightReferences = null;
            return;
        }

        if (HaveSameRacingLightReferences(cachedRacingLightReferences, racingLights))
        {
            return;
        }

        cachedRacingLightReferences = new Light[racingLights.Length];
        originalRacingLightColors = new Color[racingLights.Length];

        for (int i = 0; i < racingLights.Length; i++)
        {
            cachedRacingLightReferences[i] = racingLights[i];
            originalRacingLightColors[i] = racingLights[i] != null ? racingLights[i].color : Color.white;
        }
    }

    private static bool HaveSameRacingLightReferences(Light[] cachedLights, Light[] currentLights)
    {
        if (cachedLights == null || currentLights == null)
        {
            return false;
        }

        if (cachedLights.Length != currentLights.Length)
        {
            return false;
        }

        for (int i = 0; i < cachedLights.Length; i++)
        {
            if (cachedLights[i] != currentLights[i])
            {
                return false;
            }
        }

        return true;
    }

}
