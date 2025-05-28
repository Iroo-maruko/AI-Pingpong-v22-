using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerPaddleHit2 : MonoBehaviour
{
    [Header("References")]
    public Transform ball;
    public Rigidbody ballRb;
    public Transform aiCourtCenter;

    [Header("Hit Settings")]
    public float hitDistance = 12f;
    public float baseHitForce = 10f;
    public float baseUpwardForce = 6f;
    public float hitCooldown = 0.5f;
    public float collisionDisableTime = 0.2f;

    [Header("Angle Tuning")]
    public float maxAngleAdjustment = 0.4f;
    public float minForceScale = 0.75f;
    public float spinTorque = 2f;

    [Header("Force Clamping")]
    public float minUpwardY = 2.5f;
    public float minTotalForce = 10f;

    [Header("Target Randomness")]
    public float targetRangeX = 1.0f;
    public float targetRangeZ = 1.0f;

    private Collider paddleCollider;
    private Collider ballCollider;
    private float lastHitTime = -1f;

    private void Start()
    {
        if (!ball || !ballRb || !aiCourtCenter)
        {
            Debug.LogError("❌ PlayerPaddleHit2: Reference not assigned properly!");
            enabled = false;
            return;
        }

        paddleCollider = GetComponent<Collider>();
        ballCollider = ball.GetComponent<Collider>();
    }

    private void Update()
    {
        if (!ball || !ballRb || !aiCourtCenter) return;

        float distance = Vector3.Distance(transform.position, ball.position);
        bool canHit = distance < hitDistance && (Time.time - lastHitTime > hitCooldown);

        if (!canHit) return;

        lastHitTime = Time.time;

        if (paddleCollider && ballCollider)
            Physics.IgnoreCollision(paddleCollider, ballCollider, true);

        BallController ballController = ball.GetComponent<BallController>();
        if (ballController != null)
        {
            ballController.RegisterHit("Player");
            if (ballController.IsServe()) ballController.ForceEndServe();
        }

        // 🎯 랜덤 target 위치 계산
        Vector3 randomizedTarget = aiCourtCenter.position;
        randomizedTarget.x += Random.Range(-targetRangeX, targetRangeX);
        randomizedTarget.z += Random.Range(-targetRangeZ, targetRangeZ);

        // 🧭 방향 계산
        Vector3 direction = randomizedTarget - ball.position;
        float angleAdjust = Mathf.Clamp(ball.position.x - transform.position.x, -1f, 1f);
        direction.x += angleAdjust * maxAngleAdjustment;
        direction.z = Mathf.Max(direction.z, 0.5f);
        direction.y = 0.7f;
        direction = direction.normalized;

        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        if (ball.position.y < 0.2f)
            ball.position += Vector3.up * 0.1f;

        float forceScale = Mathf.Clamp01((hitDistance - distance) / hitDistance);
        float scaledHitForce = baseHitForce * (minForceScale + forceScale * (1f - minForceScale));
        Vector3 force = direction * scaledHitForce;
        force.y = Mathf.Max(force.y + baseUpwardForce, minUpwardY);

        if (force.magnitude < minTotalForce)
            force = force.normalized * minTotalForce;

        ballRb.AddForce(force, ForceMode.Impulse);
        ballRb.AddTorque(Vector3.right * Random.Range(-spinTorque, spinTorque), ForceMode.Impulse);

        Invoke(nameof(ResetCollision), collisionDisableTime);
    }

    private void ResetCollision()
    {
        if (paddleCollider && ballCollider)
            Physics.IgnoreCollision(paddleCollider, ballCollider, false);
    }
}
