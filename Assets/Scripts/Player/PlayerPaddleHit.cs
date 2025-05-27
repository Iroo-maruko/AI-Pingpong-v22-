using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerPaddleHit : MonoBehaviour
{
    public Transform ball;
    public Rigidbody ballRb;
    public Transform aiCourtCenter;

    [Header("Hit Settings")]
    public float hitDistance = 12f;
    public float baseHitForce = 10f;
    public float baseUpwardForce = 6f;
    public float hitCooldown = 0.5f;
    public float collisionDisableTime = 0.2f;

    [Header("Advanced Hit Tuning")]
    public float maxAngleAdjustment = 0.4f;
    public float minForceScale = 0.75f;
    public float spinTorque = 2f;
    public float misHitChance = 0.3f;
    public float misHitAngle = 2f;

    [Header("Force Clamping")]
    public float minUpwardY = 2.5f;         // 최소 y 상승값
    public float minTotalForce = 10f;       // 전체 최소 힘 크기

    private Collider paddleCollider;
    private Collider ballCollider;
    private float lastHitTime = -1f;

    private void Start()
    {
        paddleCollider = GetComponent<Collider>();
        ballCollider = ball.GetComponent<Collider>();
    }

    private void Update()
    {
        if (ball == null || ballRb == null || aiCourtCenter == null) return;

        float distance = Vector3.Distance(transform.position, ball.position);
        float timeSinceLastHit = Time.time - lastHitTime;
        bool canHit = distance < hitDistance && timeSinceLastHit > hitCooldown;

        if (canHit)
        {
            lastHitTime = Time.time;

            if (paddleCollider && ballCollider)
                Physics.IgnoreCollision(paddleCollider, ballCollider, true);

            BallController ballController = ball.GetComponent<BallController>();
            if (ballController != null)
            {
                ballController.RegisterHit("Player");
                if (ballController.IsServe())
                    ballController.ForceEndServe();
            }

            bool isRally = ballRb.velocity.z < 0f;

            Vector3 direction = (aiCourtCenter.position - ball.position);
            float angleAdjust = Mathf.Clamp(ball.position.x - transform.position.x, -1f, 1f);
            direction.x += angleAdjust * (isRally ? maxAngleAdjustment * 0.4f : maxAngleAdjustment);
            direction.z = Mathf.Max(direction.z, 0.5f);
            direction.y = isRally ? 0.75f : 0.6f;
            direction = direction.normalized;

            if (Random.value < misHitChance)
            {
                float misX = Random.Range(-misHitAngle, misHitAngle);
                float misZ = Random.Range(-misHitAngle, misHitAngle);
                direction += new Vector3(misX, 0f, misZ).normalized * 0.5f;
                Debug.LogWarning($"❌ [MisHit] Player hit misdirected: XZ offset=({misX:F2}, {misZ:F2})");
            }

            ballRb.velocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;

            if (ball.position.y < 0.2f)
                ball.position += Vector3.up * 0.1f;

            float forceScale = Mathf.Clamp01((hitDistance - distance) / hitDistance);
            float dynamicHitForce = baseHitForce * (minForceScale + forceScale * (1f - minForceScale));
            float finalHitForce = dynamicHitForce;
            float finalUpwardForce = isRally ? baseUpwardForce + 1.5f : baseUpwardForce;

            Vector3 finalForce = direction * finalHitForce;
            finalForce.y = Mathf.Max(finalForce.y + finalUpwardForce, minUpwardY);

            if (finalForce.magnitude < minTotalForce)
                finalForce = finalForce.normalized * minTotalForce;

            ballRb.AddForce(finalForce, ForceMode.Impulse);
            ballRb.AddTorque(Vector3.right * Random.Range(-spinTorque, spinTorque), ForceMode.Impulse);

            Invoke(nameof(ResetCollision), collisionDisableTime);
        }
    }

    private void ResetCollision()
    {
        if (paddleCollider && ballCollider)
            Physics.IgnoreCollision(paddleCollider, ballCollider, false);
    }
}
