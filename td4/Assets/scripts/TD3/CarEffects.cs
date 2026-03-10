using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CarEffects : MonoBehaviour
{
    public CarMovement carMovement;
    public Rigidbody rg;

    [Header("Break lights")]
    public MeshRenderer breakLightLeft;
    public MeshRenderer breakLightRight;
    public float breakLightsIntensity = 5f;
    public float normalLightsIntensity = 1f;

    [Header("Tire trails")]
    public TrailRenderer trailLeft;
    public TrailRenderer trailRight;
    public float minTurnForceToShowTrails = 0.4f;

    [Header("Turn wheels")]
    public Transform leftWheel;
    public Transform rightWheel;

    [Header("Sounds and sfx")]
    public AudioClip engineSfx;
    public float engineSfxVelocityPitchFactor = 0.25f;
    public float engineSfxBasePitch = 1f;
    public AudioClip[] collisionSfxs;

    // private vars
    private AudioSource audioSource;
    private Color breakColor;
    private float divide = 5f;
    private float minus = 1f;

    private float leftInitialY;
    private float rightInitialY;

    void Start()
    {
        // Save the rotation you set in the Inspector
        leftInitialY = leftWheel.localEulerAngles.y;
        rightInitialY = rightWheel.localEulerAngles.y;
    }
    void Awake()
    {
        // init audio source for engine sfx
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = engineSfx;
        audioSource.volume = .5f;
        audioSource.loop = true;
        audioSource.spatialBlend = 1;
        audioSource.minDistance = 25;
        audioSource.spread = 360;
        audioSource.Play();

        // init break light color for break light effect
        breakColor = breakLightLeft.material.GetColor("_EmissionColor");
    }

    void Update()
    {
        UpdateBrakeLightEffect();
        UpdateSkidEffect();
        UpdateTurnWheelsEffect();
        UpdateEngineSfx();
}

    private void OnCollisionEnter(Collision collision)
    {
        PlayCollisionSfx(collision);
    }

    private void PlayCollisionSfx(Collision collision)
    {
        if (!rg || !audioSource || collision==null)
        {
            // Can't play collision sfx
            return;
        }
        audioSource.pitch = Random.Range(0.85f, 1f);
        // the more the collisison is big, the more the sound velocity
        float volumeScale = (collision.relativeVelocity.magnitude / divide) - minus;
        audioSource.PlayOneShot(collisionSfxs[Random.Range(0, collisionSfxs.Length - 1)], volumeScale);
    }

    private void UpdateBrakeLightEffect()
    {
        if (!carMovement || !breakLightLeft || !breakLightRight)
        {
            // Can't play brake light effect
            return;
        }

        if (carMovement.input.y < 0)
        {
            if (carMovement.GetSpeed() > 0)
            {
                // breaking lights
                breakLightLeft.material.SetColor("_EmissionColor", breakColor * breakLightsIntensity);
                breakLightRight.material.SetColor("_EmissionColor", breakColor * breakLightsIntensity);
            }
            else
            {
                // reverse light
                breakLightLeft.material.SetColor("_EmissionColor", Color.white * breakLightsIntensity);
                breakLightRight.material.SetColor("_EmissionColor", breakColor * breakLightsIntensity);
            }
        }
        else
        {
            // normal lights
            breakLightLeft.material.SetColor("_EmissionColor", breakColor * normalLightsIntensity);
            breakLightRight.material.SetColor("_EmissionColor", breakColor * normalLightsIntensity);
        }
    }

    private void UpdateSkidEffect()
    {
        if (!carMovement || !trailLeft || !trailRight)
        {
            // Can't play skid effect
            return;
        }

        bool isDrifting = carMovement.GetSpeed() > 5f && Mathf.Abs(carMovement.input.x) >= minTurnForceToShowTrails;
        trailLeft.emitting = isDrifting;
        trailRight.emitting = isDrifting;
    }

    private void UpdateTurnWheelsEffect()
    {
        if (!carMovement || !leftWheel || !rightWheel) return;

        // Calculate the steer offset
        float steerAngle = carMovement.input.x * 30f;

        // Apply it to the INITIAL rotation instead of a hard-coded 90
        leftWheel.localRotation = Quaternion.Euler(
            leftWheel.localRotation.eulerAngles.x,
            leftInitialY + steerAngle,
            leftWheel.localRotation.eulerAngles.z
        );

        rightWheel.localRotation = Quaternion.Euler(
            rightWheel.localRotation.eulerAngles.x,
            rightInitialY + steerAngle,
            rightWheel.localRotation.eulerAngles.z
        );
    }

    private void UpdateEngineSfx()
    {
        if (!rg || !audioSource)
        {
            // Can't play engine sfx
            return;
        }

        audioSource.pitch = engineSfxBasePitch + rg.linearVelocity.magnitude * engineSfxVelocityPitchFactor;
    }
}
