using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform ball;
    public Rigidbody ballRb;
    public float moveSpeed = 40f;
    public float predictionOffset = 1.5f; // 큰 예측 오차 범위
    public float minY = 0.5f;
    public float maxY = 7f;

    [Header("Imprecision Settings")]
    public float predictionErrorChance = 0.8f;  // 80% 확률으로 예측 오류 발생
    public float significantErrorThresholdZ = 4f; // 이 이상이면 로그 Í \xcd9c력

    [Header("Reaction Settings")]
    public float reactionSlowChance = 0.1f;  // 10% 확률으로 느리면
    public float slowSpeedMultiplier = 0.3f;

    private void Update()
    {
        if (ball == null || ballRb == null) return;

        Vector3 predictedPos = PredictBallLanding();
        Vector3 targetPos = new Vector3(
            transform.position.x,
            Mathf.Clamp(predictedPos.y, minY, maxY),
            predictedPos.z
        );

        float currentSpeed = moveSpeed;
        if (Random.value < reactionSlowChance)
        {
            currentSpeed *= slowSpeedMultiplier;
            // Debug.Log("🐢 [Player Reaction] Slowed reaction this frame");
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);
    }

    private Vector3 PredictBallLanding()
    {
        float vz = ballRb.velocity.z;
        if (Mathf.Approximately(vz, 0f)) return ball.position;

        float relativeZ = ball.position.z - transform.position.z;
        float timeToReach = Mathf.Clamp(Mathf.Abs(relativeZ / vz), 0.5f, 3f);

        Vector3 predicted = ball.position + ballRb.velocity * timeToReach;

        if (Random.value < predictionErrorChance)
        {
            float offsetY = Random.Range(-predictionOffset, predictionOffset);
            float offsetZ = Random.Range(-predictionOffset * 4f, predictionOffset * 4f);

            predicted.y += offsetY;
            predicted.z += offsetZ;

            if (Mathf.Abs(offsetZ) >= significantErrorThresholdZ)
            {
                // Debug.Log($"⚠️ [Player Prediction Error] Major deviation applied: z += {offsetZ:F2}");
            }
        }

        return predicted;
    }
}
