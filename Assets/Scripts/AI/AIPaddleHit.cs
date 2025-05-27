using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AIPaddleHit : MonoBehaviour
{
    public Transform ball;
    public Rigidbody ballRb;
    public AIAgent agent;

    [Header("Hit Settings")]
    public float baseHitForce = 10f;
    public float baseUpwardForce = 6f;
    public float hitDistance = 9f;
    public float hitCooldown = 0.5f;

    [Header("Advanced Tuning")]
    public float minForceScale = 0.75f;
    public float spinTorque = 2f;

    private float lastHitTime = -1f;

    private void Update()
    {
        if (ball == null || ballRb == null || agent == null)
        {
            Debug.LogWarning("❌ Missing references: ball or ballRb or agent");
            return;
        }

        BallController ballController = ball.GetComponent<BallController>();
        if (ballController == null)
        {
            Debug.LogWarning("❌ Missing BallController on ball");
            return;
        }

        // ✅ 중복 타격 방지
        if (ballController.AIHasHit())
        {
            // Debug.Log("⚠️ AI already hit this rally.");
            return;
        }

        float distance = Vector3.Distance(transform.position, ball.position);
        if (distance > hitDistance)
        {
            // Debug.Log($"⛔ Too far to hit: Distance = {distance:F2} (Threshold = {hitDistance})");
            return;
        }

        float timeSinceLastHit = Time.time - lastHitTime;
        bool wantToHit = agent.currentHitAction[0] > 0.5f;
        if (!wantToHit)
        {
            // Debug.Log("⛔ Agent does not want to hit (hitAction[0] too low)");
            return;
        }

        if (timeSinceLastHit < hitCooldown)
        {
            // Debug.Log($"⏳ Cooldown not ready: {timeSinceLastHit:F2}s < {hitCooldown}s");
            return;
        }

        lastHitTime = Time.time;

        Vector3 direction = new Vector3(agent.currentHitAction[2], 0.6f, agent.currentHitAction[3]);
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
            Debug.Log("⚠️ Using fallback forward direction due to low magnitude");
        }
        direction = direction.normalized;

        float forceScale = Mathf.Clamp01((hitDistance - distance) / hitDistance);
        float dynamicHitForce = baseHitForce * (minForceScale + forceScale * (1f - minForceScale));
        float hitPower = Mathf.Clamp01(agent.currentHitAction[1]);
        Vector3 finalForce = direction * hitPower * dynamicHitForce;
        finalForce.y += baseUpwardForce;

        if (finalForce.magnitude < 5f)
        {
            Debug.Log($"⚠️ Force too weak ({finalForce.magnitude:F2}), boosting to minimum");
            finalForce = finalForce.normalized * 5f;
        }

        Debug.Log($"✅ [AI Hit] Force = {finalForce}, Power = {hitPower:F2}, Distance = {distance:F2}");

        ballController.RegisterHit("AI");
        if (ballController.IsServe()) ballController.ForceEndServe();

        if (ball.position.y < 0.2f)
            ball.position += Vector3.up * 0.1f;

        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        ballRb.AddForce(finalForce, ForceMode.Impulse);
        ballRb.AddTorque(Vector3.right * Random.Range(-spinTorque, spinTorque), ForceMode.Impulse);
    }
}
