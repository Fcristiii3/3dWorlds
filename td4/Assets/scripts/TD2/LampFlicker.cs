using UnityEngine;

[DisallowMultipleComponent]
public class LampFlicker : MonoBehaviour
{
    private enum LampPersonality
    {
        Steady,
        Flicker,
        Reactive
    }

    public Light lampLight;
    public Transform player;

    [Range(0f, 1f)]
    public float chanceSteady = 0.7f;

    [Range(0f, 1f)]
    public float chanceFlicker = 0.2f;

    [Range(0f, 1f)]
    public float chanceReactive = 0.1f;

    [Range(0f, 1f)]
    public float flickerChance = 0.35f;

    [Min(0.01f)]
    public float minFlickerDelay = 0.03f;

    [Min(0.01f)]
    public float maxFlickerDelay = 0.12f;

    [Min(0.1f)]
    public float activationDistance = 18f;

    private LampPersonality personality = LampPersonality.Steady;
    private float nextFlickerTime;
    private bool reactivePlayerInRange;

    private void Reset()
    {
        lampLight = GetComponentInChildren<Light>();
        player = FindPlayerTransform();
    }

    private void OnValidate()
    {
        if (maxFlickerDelay < minFlickerDelay)
        {
            maxFlickerDelay = minFlickerDelay;
        }

        activationDistance = Mathf.Max(0.1f, activationDistance);
    }

    private void Start()
    {
        if (lampLight == null)
        {
            lampLight = GetComponentInChildren<Light>();
        }

        if (lampLight == null)
        {
            Debug.LogWarning($"LampFlicker on '{name}' could not find a Light reference.");
            enabled = false;
            return;
        }

        if (player == null)
        {
            player = FindPlayerTransform();
        }

        personality = ChoosePersonality();

        if (personality == LampPersonality.Steady)
        {
            lampLight.enabled = true;
            enabled = false;
            return;
        }

        if (personality == LampPersonality.Flicker)
        {
            lampLight.enabled = true;
            ScheduleNextFlicker();
            return;
        }

        lampLight.enabled = false;
        reactivePlayerInRange = false;
    }

    private void Update()
    {
        switch (personality)
        {
            case LampPersonality.Flicker:
                UpdateAlwaysFlicker();
                break;

            case LampPersonality.Reactive:
                UpdateReactiveFlicker();
                break;
        }
    }

    private void UpdateAlwaysFlicker()
    {
        if (Time.time < nextFlickerTime)
        {
            return;
        }

        lampLight.enabled = Random.value > flickerChance;
        ScheduleNextFlicker();
    }

    private void UpdateReactiveFlicker()
    {
        if (player == null)
        {
            player = FindPlayerTransform();
            if (player == null)
            {
                lampLight.enabled = false;
                return;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > activationDistance)
        {
            reactivePlayerInRange = false;
            lampLight.enabled = false;
            return;
        }

        if (!reactivePlayerInRange)
        {
            reactivePlayerInRange = true;
            nextFlickerTime = Time.time;
        }

        if (Time.time < nextFlickerTime)
        {
            return;
        }

        lampLight.enabled = Random.value > flickerChance;
        ScheduleNextFlicker();
    }

    private void ScheduleNextFlicker()
    {
        nextFlickerTime = Time.time + Random.Range(minFlickerDelay, maxFlickerDelay);
    }

    private LampPersonality ChoosePersonality()
    {
        float steadyWeight = Mathf.Max(0f, chanceSteady);
        float flickerWeight = Mathf.Max(0f, chanceFlicker);
        float reactiveWeight = Mathf.Max(0f, chanceReactive);
        float totalWeight = steadyWeight + flickerWeight + reactiveWeight;

        if (totalWeight <= 0f)
        {
            return LampPersonality.Steady;
        }

        float roll = Random.value * totalWeight;
        if (roll < steadyWeight)
        {
            return LampPersonality.Steady;
        }

        roll -= steadyWeight;
        if (roll < flickerWeight)
        {
            return LampPersonality.Flicker;
        }

        return LampPersonality.Reactive;
    }

    private static Transform FindPlayerTransform()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer.transform;
        }

        PlayerControl playerControl = FindFirstObjectByType<PlayerControl>();
        return playerControl != null ? playerControl.transform : null;
    }
}
