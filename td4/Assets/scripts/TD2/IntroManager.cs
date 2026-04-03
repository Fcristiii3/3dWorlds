using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class IntroManager : MonoBehaviour
{
    public GameObject introCamera;
    public GameObject mainPlayerCamera;
    public MonoBehaviour[] scriptsToDisableDuringIntro;
    public GameObject introUI;

    public bool destroyIntroObjectsOnStart = false;
    public UnityEvent onRaceStarted;

    private bool hasRaceStarted;

    private void Start()
    {
        if (introCamera != null)
        {
            introCamera.SetActive(true);
        }

        if (introUI != null)
        {
            introUI.SetActive(true);
        }

        if (mainPlayerCamera != null)
        {
            mainPlayerCamera.SetActive(false);
        }

        SetControlledScriptsEnabled(false);
        Physics.SyncTransforms();
    }

    private void Update()
    {
        if (hasRaceStarted)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartRace();
        }
    }

    public void StartRace()
    {
        if (hasRaceStarted)
        {
            return;
        }

        hasRaceStarted = true;

        if (introCamera != null)
        {
            introCamera.SetActive(false);

            if (destroyIntroObjectsOnStart)
            {
                Destroy(introCamera);
            }
        }

        if (introUI != null)
        {
            introUI.SetActive(false);

            if (destroyIntroObjectsOnStart)
            {
                Destroy(introUI);
            }
        }

        if (mainPlayerCamera != null)
        {
            mainPlayerCamera.SetActive(true);

            followPlayer followCamera = mainPlayerCamera.GetComponent<followPlayer>();
            if (followCamera != null)
            {
                followCamera.SnapToTarget();
            }
        }

        SetControlledScriptsEnabled(true);
        Physics.SyncTransforms();
        onRaceStarted?.Invoke();
    }

    private void SetControlledScriptsEnabled(bool enabledState)
    {
        if (scriptsToDisableDuringIntro == null)
        {
            return;
        }

        for (int i = 0; i < scriptsToDisableDuringIntro.Length; i++)
        {
            MonoBehaviour script = scriptsToDisableDuringIntro[i];
            if (script == null)
            {
                continue;
            }

            script.enabled = enabledState;

            Rigidbody rb = script.GetComponent<Rigidbody>();
            if (rb == null)
            {
                continue;
            }

            if (!enabledState)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
            else
            {
                rb.WakeUp();
            }
        }
    }
}
