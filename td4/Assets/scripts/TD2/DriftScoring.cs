using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class DriftScoring : MonoBehaviour
{
    [Header("References")]
    public Rigidbody targetRigidbody;
    public CarMovement carMovement;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI currentDriftText;
    public TextMeshProUGUI targetScoreText;
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI multiplierText;

    [Header("Drift Detection")]
    [Min(0f)]
    public float minDriftAngle = 18f;
    [Min(0f)]
    public float minSpeed = 7f;
    [Min(0f)]
    public float driftGracePeriod = 1.5f;
    [Min(0f)]
    public float minSidewaysSpeed = 2.5f;
    [Min(0f)]
    public float assistedDriftAngle = 10f;

    [Header("Scoring")]
    [Min(0.1f)]
    public float pointsPerSecond = 25f;
    [Min(0.1f)]
    public float maxDriftAngleForMaxScore = 60f;
    [Min(1f)]
    public float maxAngleScoreMultiplier = 3f;
    [Min(0f)]
    public float targetScore = 5000f;
    [Min(1)]
    public int currentMultiplier = 1;
    [Min(1)]
    public int maxMultiplier = 5;
    [Min(0.1f)]
    public float timeToNextMultiplier = 2.5f;

    [Header("UI Labels")]
    public string totalScorePrefix = "Total: ";
    public string currentDriftPrefix = "Drift: ";
    public string targetScorePrefix = "Target: ";
    public string lapPrefix = "Lap ";

    [Header("Runtime")]
    public bool isDrifting;
    public float totalScore;
    public float currentDriftAngle;

    [SerializeField]
    private float currentDriftScore;
    [SerializeField]
    private float driftGraceTimer;
    [SerializeField]
    private float driftTimer;

    public float CurrentScore => totalScore;
    public float CurrentDriftScore => currentDriftScore;

    private void Reset()
    {
        targetRigidbody = GetComponent<Rigidbody>();
        carMovement = GetComponent<CarMovement>();
    }

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }

        if (carMovement == null)
        {
            carMovement = GetComponent<CarMovement>();
        }
    }

    private void Start()
    {
        if (targetScoreText != null)
        {
            targetScoreText.text = $"{targetScorePrefix}{Mathf.RoundToInt(targetScore)}";
        }

        UpdateScoreUI();
    }

    private void Update()
    {
        bool driftQualifiesThisFrame = EvaluateDriftState();

        if (driftQualifiesThisFrame)
        {
            isDrifting = true;
            driftGraceTimer = driftGracePeriod;
            UpdateMultiplierTimer();

            float angleLerp = Mathf.InverseLerp(
                minDriftAngle,
                Mathf.Max(minDriftAngle + 0.01f, maxDriftAngleForMaxScore),
                currentDriftAngle
            );

            float angleMultiplier = Mathf.Lerp(1f, maxAngleScoreMultiplier, angleLerp);
            currentDriftScore += pointsPerSecond * angleMultiplier * currentMultiplier * Time.deltaTime;
        }
        else if (isDrifting)
        {
            driftGraceTimer = Mathf.Max(0f, driftGraceTimer - Time.deltaTime);

            if (driftGraceTimer <= 0f)
            {
                isDrifting = false;

                if (currentDriftScore > 0f)
                {
                    BankCurrentDriftScore();
                }
            }
        }

        UpdateScoreUI();
    }

    private bool EvaluateDriftState()
    {
        if (targetRigidbody == null)
        {
            currentDriftAngle = 0f;
            return false;
        }

        // Work only in the XZ plane so bumps or jumps do not fake a drift.
        Vector3 planarVelocity = targetRigidbody.linearVelocity;
        planarVelocity.y = 0f;
        Vector3 localVelocity = transform.InverseTransformDirection(planarVelocity);
        float sidewaysSpeed = Mathf.Abs(localVelocity.x);

        float planarSpeed = planarVelocity.magnitude;
        if (planarSpeed < minSpeed)
        {
            currentDriftAngle = 0f;
            return false;
        }

        // Compare where the car points versus where it is actually travelling.
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f || planarVelocity.sqrMagnitude <= 0.0001f)
        {
            currentDriftAngle = 0f;
            return false;
        }

        forward.Normalize();
        planarVelocity.Normalize();

        float geometricDriftAngle = Vector3.Angle(forward, planarVelocity);

        // Low-grip drift physics can keep the car in a shallow angle while the body still slides hard sideways.
        float sidewaysAngleEquivalent = Mathf.InverseLerp(
            minSidewaysSpeed,
            minSidewaysSpeed + 8f,
            sidewaysSpeed
        ) * maxDriftAngleForMaxScore;

        currentDriftAngle = Mathf.Max(geometricDriftAngle, sidewaysAngleEquivalent);

        bool meetsPureAngleCheck = currentDriftAngle >= minDriftAngle;
        bool meetsSlipCheck = sidewaysSpeed >= minSidewaysSpeed;
        bool hasDriftIntent = HasDriftIntent();

        return meetsPureAngleCheck || (hasDriftIntent && meetsSlipCheck && currentDriftAngle >= assistedDriftAngle);
    }

    private bool HasDriftIntent()
    {
        if (carMovement == null)
        {
            return false;
        }

        return Mathf.Abs(carMovement.input.y) > 0.01f && Mathf.Abs(carMovement.input.x) >= carMovement.driftTurnThreshold;
    }

    private void BankCurrentDriftScore()
    {
        totalScore += currentDriftScore;
        currentDriftScore = 0f;
        ResetMultiplier();
    }

    private void BustCurrentDrift()
    {
        currentDriftScore = 0f;
        isDrifting = false;
        currentDriftAngle = 0f;
        driftGraceTimer = 0f;
        ResetMultiplier();
        Debug.Log("Drift Bust!");
        UpdateScoreUI();
    }

    private void UpdateMultiplierTimer()
    {
        if (currentMultiplier >= maxMultiplier)
        {
            driftTimer = 0f;
            return;
        }

        driftTimer += Time.deltaTime;
        if (driftTimer >= timeToNextMultiplier)
        {
            currentMultiplier = Mathf.Min(currentMultiplier + 1, maxMultiplier);
            driftTimer = 0f;
        }
    }

    private void ResetMultiplier()
    {
        currentMultiplier = 1;
        driftTimer = 0f;
    }

    private void UpdateScoreUI()
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = $"{totalScorePrefix}{Mathf.RoundToInt(totalScore)}";
        }

        if (currentDriftText != null)
        {
            bool showCurrentDrift = currentDriftScore > 0f;
            currentDriftText.gameObject.SetActive(showCurrentDrift);

            if (showCurrentDrift)
            {
                currentDriftText.text = $"{currentDriftPrefix}{Mathf.RoundToInt(currentDriftScore)}";
            }
        }

        if (multiplierText != null)
        {
            bool showMultiplier = currentMultiplier > 1;
            multiplierText.gameObject.SetActive(showMultiplier);

            if (showMultiplier)
            {
                multiplierText.text = $"x{currentMultiplier}";
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        Debug.LogWarning("I just hit: " + collision.gameObject.name + " on Layer: " + LayerMask.LayerToName(collision.gameObject.layer));

        GameObject hitObject = collision.gameObject;
        bool hitWallLayer = hitObject.layer == LayerMask.NameToLayer("Wall");
        bool hitWallTag = hitObject.CompareTag("Wall");

        if (!hitWallLayer && !hitWallTag)
        {
            return;
        }

        BustCurrentDrift();
    }

    public void UpdateLapUI(int currentLap, int maxLaps)
    {
        if (lapText == null)
        {
            return;
        }

        lapText.text = $"{lapPrefix}{currentLap}/{maxLaps}";
    }

    public void CheckWinCondition()
    {
        if (totalScore >= targetScore)
        {
            Debug.Log("You Win!");
        }
        else
        {
            Debug.Log("You Lose!");
        }
    }
}
