using UnityEngine;

public class HunterController2D : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float eatRadius = 1f;
    public Color hunterColor = new Color(1f, 0.45f, 0.3f, 1f);
    public bool useAutoControl;

    [Header("References")]
    public BoidGameManager2D manager;
    private Rigidbody2D hunterRigidbody;

    private void Start()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<BoidGameManager2D>();
        }

        Renderer hunterRenderer = GetComponent<Renderer>();
        if (hunterRenderer != null)
        {
            hunterRenderer.material.color = hunterColor;
        }

        BoxCollider2D hunterCollider = GetComponent<BoxCollider2D>();
        if (hunterCollider == null)
        {
            hunterCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        hunterCollider.isTrigger = true;

        hunterRigidbody = GetComponent<Rigidbody2D>();
        if (hunterRigidbody == null)
        {
            hunterRigidbody = gameObject.AddComponent<Rigidbody2D>();
        }

        hunterRigidbody.gravityScale = 0f;
        hunterRigidbody.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Update()
    {
        if (manager == null)
        {
            return;
        }

        if (!manager.IsGameRunning)
        {
            return;
        }

        Vector2 input = useAutoControl ? GetAutoDirection() : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector2 nextPosition = (Vector2)transform.position + input * moveSpeed * Time.deltaTime;
        Vector2 fakeVelocity = input * moveSpeed;
        manager.ClampAndBounce(ref nextPosition, ref fakeVelocity);
        hunterRigidbody.MovePosition(nextPosition);

        // Extra overlap fallback so eating works reliably even when trigger events are missed.
        manager.EatBoidsWithin(nextPosition, eatRadius);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        BoidAgent2D boid = other.GetComponent<BoidAgent2D>();
        if (boid == null || manager == null)
        {
            return;
        }

        boid.CaughtByHunter();
        manager.RemoveBoid(boid);
    }

    private Vector2 GetAutoDirection()
    {
        if (manager == null || manager.Boids.Count == 0)
        {
            return Vector2.zero;
        }

        Vector2 current = transform.position;
        BoidAgent2D nearest = null;
        float bestDistanceSqr = float.MaxValue;
        for (int i = 0; i < manager.Boids.Count; i++)
        {
            BoidAgent2D boid = manager.Boids[i];
            if (boid == null)
            {
                continue;
            }

            float distSqr = ((Vector2)boid.transform.position - current).sqrMagnitude;
            if (distSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distSqr;
                nearest = boid;
            }
        }

        if (nearest == null)
        {
            return Vector2.zero;
        }

        return ((Vector2)nearest.transform.position - current).normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, eatRadius);
    }
}
