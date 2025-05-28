using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AIPaddleHit : MonoBehaviour
{
    public Transform ball;
    public Rigidbody ballRb;
    public AIAgent agent;

    [Header("Hit Settings")]
    public float baseHitForce = 24f; // 전체 힘 약간 줄임
    public float baseUpwardForce = 4f;
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

        if (ballController.AIHasHit()) return;

        float distance = Vector3.Distance(transform.position, ball.position);
        if (distance > hitDistance) return;

        float timeSinceLastHit = Time.time - lastHitTime;
        bool wantToHit = agent.currentHitAction[0] > 0.5f;
        if (!wantToHit || timeSinceLastHit < hitCooldown) return;

        lastHitTime = Time.time;

        float xComponent = agent.currentHitAction[2] * 1.2f;
        float zComponent = Mathf.Clamp(agent.currentHitAction[3] * 0.5f, -1f, 0.2f); // 뒤로 치지 않도록 제한

        Vector3 direction = new Vector3(xComponent, 0.15f, zComponent);
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
            Debug.Log("⚠️ Using fallback forward direction due to low magnitude");
        }
        direction = direction.normalized;

        float forceScale = Mathf.Clamp01((hitDistance - distance) / hitDistance);
        float dynamicHitForce = baseHitForce * (minForceScale + forceScale * (1f - minForceScale));

        float hitPower = Mathf.Clamp01(agent.currentHitAction[1]);
        hitPower = Mathf.Lerp(1.3f, 2.2f, hitPower); // 힘 범위 약간 축소

        Vector3 finalForce = direction * hitPower * dynamicHitForce;
        finalForce.y = Mathf.Clamp(finalForce.y + baseUpwardForce, 2.5f, 6.5f);

        // 🎯 Z값 과도한 경우 보정
        if (Mathf.Abs(finalForce.z) > 8f)
        {
            float sign = Mathf.Sign(finalForce.z);
            finalForce.z = sign * 8f;
            Debug.Log("⚠️ Adjusted Z to limit over-powerful forward/backward hits");
        }

        // 최소 힘 보장
        if (finalForce.magnitude < 13f)
        {
            Debug.Log($"⚠️ Force too weak ({finalForce.magnitude:F2}), boosting to minimum");
            finalForce = finalForce.normalized * 13f;
        }

        Debug.Log($"✅ [AI Hit] Force = {finalForce}, Power = {hitPower:F2}, Distance = {distance:F2}");

        ballController.RegisterHit("AI");
        if (ballController.IsServe()) ballController.ForceEndServe();

        if (ball.position.y < 0.2f)
            ball.position += Vector3.up * 0.1f;

        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        ballRb.AddForce(finalForce, ForceMode.Impulse);
        // ✅ 좋은 발사면 AI 테이블과 충돌 무시
        if (Mathf.Abs(finalForce.x) >= 7f && Mathf.Abs(finalForce.z) <= 4f)
        {
            BallController ballCtrl = ballRb.GetComponent<BallController>();
            if (ballCtrl != null && ballCtrl.aiTableCollider != null && ballCtrl.ballCollider != null)
            {
                Physics.IgnoreCollision(ballCtrl.ballCollider, ballCtrl.aiTableCollider, true);
                ballCtrl.SetIgnoringAITable(true);
                Debug.Log("✅ AI GOOD HIT: Ignoring collision with AITable temporarily.");
            }
        }
        ballRb.AddTorque(Vector3.right * Random.Range(-spinTorque, spinTorque), ForceMode.Impulse);
    }
}
