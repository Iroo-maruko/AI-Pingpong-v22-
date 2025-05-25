using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AIPaddleHit : MonoBehaviour
{
    public Transform ball;
    public Rigidbody ballRb;
    public Transform targetCourtPosition;

    [Header("Hit Settings")]
    public float baseHitForce = 10f;
    public float baseUpwardForce = 6f;
    public float hitDistance = 9f;
    public float hitCooldown = 0.5f;
    public float collisionDisableTime = 0.2f;

    [Header("Advanced Tuning")]
    public float minForceScale = 0.75f;
    public float maxAngleAdjustment = 0.4f;
    public float spinTorque = 2f;

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
        if (ball == null || ballRb == null || targetCourtPosition == null) return;

        float distance = Vector3.Distance(transform.position, ball.position);
        float timeSinceLastHit = Time.time - lastHitTime;         
        bool canHit = distance < hitDistance && timeSinceLastHit > hitCooldown;

        if (canHit)
        {
            lastHitTime = Time.time;

            BallController ballController = ball.GetComponent<BallController>();
            if (ballController != null)
            {
                ballController.RegisterHit("AI");
                if (ballController.IsServe())
                {
                    ballController.ForceEndServe();
                    Debug.Log("🟢 [AIPaddleHit] 서브 상태 종료 → 랠리 시작");
                }
            }

            if (paddleCollider != null && ballCollider != null)
                Physics.IgnoreCollision(paddleCollider, ballCollider, true);

            Vector3 direction = (targetCourtPosition.position - ball.position);
            direction.z = Mathf.Min(direction.z, -0.5f);  // ⬅️ Player 방향 보장
            direction.y = 0.65f;

            float angleAdjust = Mathf.Clamp(ball.position.x - transform.position.x, -1f, 1f);
            direction.x += angleAdjust * maxAngleAdjustment;
            direction = direction.normalized;

            ballRb.velocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;

            if (ball.position.y < 0.2f)
                ball.position += Vector3.up * 0.1f;

            float forceScale = Mathf.Clamp01((hitDistance - distance) / hitDistance);
            float dynamicHitForce = baseHitForce * (minForceScale + forceScale * (1f - minForceScale));
            float finalHitForce = dynamicHitForce;
            float finalUpwardForce = baseUpwardForce;

            Vector3 finalForce = direction * finalHitForce;
            finalForce.y += finalUpwardForce;

            ballRb.AddForce(finalForce, ForceMode.Impulse);
            ballRb.AddTorque(Vector3.right * Random.Range(-spinTorque, spinTorque), ForceMode.Impulse);

            Debug.Log($"[AI Hit] Force={finalForce}, BallPos={ball.position}");

            Invoke(nameof(ResetCollision), collisionDisableTime);
        }
    }

    private void ResetCollision()
    {
        if (paddleCollider != null && ballCollider != null)
            Physics.IgnoreCollision(paddleCollider, ballCollider, false);
    }
}
