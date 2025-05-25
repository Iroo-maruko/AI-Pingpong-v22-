using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(Rigidbody))]
public class AIAgent : Agent
{
    public Transform ball;
    public Rigidbody ballRb;
    public Transform targetCourtPosition;

    public float moveSpeed = 40f;
    public float hitForce = 15f;
    public float upwardForce = 8f;
    public float hitDistance = 2.5f;

    private Rigidbody rb;
    public float hitCooldown = 0.5f;
    private float lastHitTime = -1f;

    public Vector3 minPosition = new Vector3(-80f, 2f, -40f);
    public Vector3 maxPosition = new Vector3(-50f, 16f, 50f);

    private float episodeTimer = 0f;
    public float maxEpisodeTime = 10f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.None;
    }

    void Update()
    {
        if (!isActiveAndEnabled) return;

        episodeTimer += Time.deltaTime;
        if (episodeTimer > maxEpisodeTime)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        episodeTimer = 0f;

        if (ball != null && ballRb != null)
        {
            ball.position = new Vector3(0f, 1.2f, 0f);
            ballRb.velocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        BallController bc = ball?.GetComponent<BallController>();
        bc?.LaunchBall();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (ball == null || ballRb == null || targetCourtPosition == null) return;

        sensor.AddObservation(transform.localPosition);                                 // 3
        sensor.AddObservation(ball.localPosition);                                     // 3
        sensor.AddObservation(ballRb.velocity);                                  // 3
        sensor.AddObservation(targetCourtPosition.localPosition);                      // 3
        sensor.AddObservation((ball.position - transform.position).normalized);        // 3
        sensor.AddObservation(Vector3.Dot((ball.position - transform.position).normalized, transform.forward)); // 1
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var act = actions.ContinuousActions;

        // 이동 처리
        Vector3 move = new Vector3(act[0], act[1], act[2]) * moveSpeed * Time.deltaTime;
        Vector3 newPosition = transform.localPosition + move;
        newPosition = new Vector3(
            Mathf.Clamp(newPosition.x, minPosition.x, maxPosition.x),
            Mathf.Clamp(newPosition.y, minPosition.y, maxPosition.y),
            Mathf.Clamp(newPosition.z, minPosition.z, maxPosition.z)
        );
        transform.localPosition = newPosition;

        // 회전 처리
        Vector3 rot = new Vector3(act[3], act[4], act[5]);
        transform.Rotate(rot * 100f * Time.deltaTime);

        // 거리 기반 보상
        float distanceToBall = Vector3.Distance(transform.position, ball.position);
        AddReward(-0.001f * distanceToBall);

        // 타격 자동화
        if (distanceToBall < hitDistance && Time.time - lastHitTime > hitCooldown)
        {
            float hitPower = Mathf.Clamp01(act[6] + 0.3f);

            Vector3 direction = (targetCourtPosition.position - ball.position).normalized;
            direction.y = Mathf.Clamp(direction.y, 0.1f, 0.35f);
            float alignment = Vector3.Dot(direction, transform.forward);

            ballRb.velocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;

            Vector3 hitForceVec = (direction + Vector3.up * upwardForce * 0.3f) * hitPower * hitForce;
            ballRb.AddForce(hitForceVec, ForceMode.Impulse);

            AddReward(+1.0f); // 기본 명중 보상

            if (alignment > 0)
                AddReward(0.3f * alignment);
            else
                AddReward(-0.3f * Mathf.Abs(alignment));

            Vector3 predictedLanding = ball.position + ballRb.velocity.normalized * 1.5f;
            float targetDistance = Vector3.Distance(predictedLanding, targetCourtPosition.position);
            AddReward(Mathf.Clamp01(1f - targetDistance / 10f)); // 착지 위치 보상

            lastHitTime = Time.time;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var a = actionsOut.ContinuousActions;
        a[0] = Input.GetAxis("Horizontal");
        a[1] = 0f;
        a[2] = Input.GetAxis("Vertical");
        a[3] = 0f; a[4] = 0f; a[5] = 0f;
        a[6] = 1f;
    }

    public void MissedBallPenalty()
    {
        if (!isActiveAndEnabled) return;
        AddReward(-1.5f);
        EndEpisode();
    }

    public void SuccessfulPoint()
    {
        if (!isActiveAndEnabled) return;
        AddReward(+2.0f);
        EndEpisode();
    }
}
