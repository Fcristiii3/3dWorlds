using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class DriftScoring : MonoBehaviour
{
    [Header("References")]
    public Rigidbody targetRigidbody;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI currentDriftText;
    public TextMeshProUGUI targetScoreText;
    public TextMeshProUGUI lapText;

    [Header("Drift Detection")]
    [Min(0f)]
    public float minDriftAngle = 18f;
    [Min(0f)]
    public float minSpeed = 7f;

    [Header("Scoring")]
    [Min(0.1f)]
    public float pointsPerSecond = 25f;
    [Min(0.1f)]
    public float maxDriftAngleForMaxScore = 60f;
    [Min(1f)]
    public float maxAngleScoreMultiplier = 3f;
    [Min(0f)]
    public float targetScore = 5000f;

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

    public float CurrentScore => totalScore;
    public float CurrentDriftScore => currentDriftScore;

    private void Reset()
    {
        targetRigidbody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody>();
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
        bool wasDrifting = isDrifting;
        UpdateDriftState();

        if (isDrifting)
        {
            float angleLerp = Mathf.InverseLerp(
                minDriftAngle,
                Mathf.Max(minDriftAngle + 0.01f, maxDriftAngleForMaxScore),
                currentDriftAngle
            );

            float angleMultiplier = Mathf.Lerp(1f, maxAngleScoreMultiplier, angleLerp);
            currentDriftScore += pointsPerSecond * angleMultiplier * Time.deltaTime;
        }
        else if (wasDrifting && currentDriftScore > 0f)
        {
            BankCurrentDriftScore();
        }

        UpdateScoreUI();
    }

    private void UpdateDriftState()
    {
        if (targetRigidbody == null)
        {
            isDrifting = false;
            currentDriftAngle = 0f;
            return;
        }

        // Work only in the XZ plane so bumps or jumps do not fake a drift.
        Vector3 planarVelocity = targetRigidbody.linearVelocity;
        planarVelocity.y = 0f;

        float planarSpeed = planarVelocity.magnitude;
        if (planarSpeed < minSpeed)
        {
            isDrifting = false;
            currentDriftAngle = 0f;
            return;
        }

        // Compare where the car points versus where it is actually travelling.
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f || planarVelocity.sqrMagnitude <= 0.0001f)
        {
            isDrifting = false;
            currentDriftAngle = 0f;
            return;
        }

        forward.Normalize();
        planarVelocity.Normalize();

        currentDriftAngle = Vector3.Angle(forward, planarVelocity);
        isDrifting = currentDriftAngle >= minDriftAngle;
    }

    private void BankCurrentDriftScore()
    {
        totalScore += currentDriftScore;
        currentDriftScore = 0f;
    }

    private void BustCurrentDrift()
    {
        currentDriftScore = 0f;
        isDrifting = false;
        currentDriftAngle = 0f;
        Debug.Log("Drift Bust!");
        UpdateScoreUI();
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
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        GameObject hitObject = collision.collider.gameObject;
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
