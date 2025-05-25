using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform ball;
    public Rigidbody ballRb;
    public float moveSpeed = 40f;
    public float predictionOffset = 0.3f;
    public float minY = 0.5f;
    public float maxY = 7f;

    private void Update()
    {
        if (ball == null || ballRb == null) return;

        Vector3 predictedPos = PredictBallLanding();
        Vector3 targetPos = new Vector3(
            transform.position.x,
            Mathf.Clamp(predictedPos.y, minY, maxY),
            predictedPos.z // z 제한 없이 자유 이동
        );

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    private Vector3 PredictBallLanding()
    {
        float vz = ballRb.velocity.z;
        if (Mathf.Approximately(vz, 0f)) return ball.position;

        float relativeZ = ball.position.z - transform.position.z;
        float timeToReach = Mathf.Clamp(Mathf.Abs(relativeZ / vz), 0f, 1.5f);

        Vector3 predicted = ball.position + ballRb.velocity * timeToReach;

        predicted.y += Random.Range(-predictionOffset, predictionOffset);
        predicted.z += Random.Range(-predictionOffset, predictionOffset);
        return predicted;
    }
}
