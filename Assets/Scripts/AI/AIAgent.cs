using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(Rigidbody))]
public class AIAgent : Agent
{
    public Transform ball;
    public Rigidbody ballRb;

    public float moveSpeed = 40f;
    public float hitDistance = 2.5f;

    private Rigidbody rb;
    public float hitCooldown = 0.5f;
    private float lastHitTime = -1f;

    public Vector3 minPosition = new Vector3(-80f, 2f, -40f);
    public Vector3 maxPosition = new Vector3(-50f, 16f, 50f);

    private float episodeTimer = 0f;
    public float maxEpisodeTime = 999999f;

    // [0]=HitFlag, [1]=Power, [2]=DirX, [3]=DirZ
    public float[] currentHitAction = new float[4];

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
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
            ballRb.WakeUp();
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        BallController bc = ball?.GetComponent<BallController>();
        bc?.LaunchBall(GameManager.Instance.serveBy); // ✅ 올바른 호출
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (ball == null || ballRb == null) return;

        sensor.AddObservation(transform.localPosition);                          // 3
        sensor.AddObservation(ball.localPosition);                              // 3
        sensor.AddObservation(ballRb.velocity);                                 // 3
        sensor.AddObservation((ball.position - transform.position).normalized); // 3
        sensor.AddObservation(Vector3.Dot((ball.position - transform.position).normalized, transform.forward)); // 1
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var act = actions.ContinuousActions;

        MoveAgent(act);

        float distanceToBall = Vector3.Distance(transform.position, ball.position);
        AddReward(-0.001f * distanceToBall);

        currentHitAction[0] = act[3];
        currentHitAction[1] = Mathf.Clamp01(act[4]);
        currentHitAction[2] = act[5];
        currentHitAction[3] = act[6];
    }

    private void MoveAgent(ActionSegment<float> act)
    {
        Vector3 move = new Vector3(act[0], act[1], act[2]) * moveSpeed * Time.deltaTime;
        Vector3 newPosition = transform.localPosition + move;
        newPosition = new Vector3(
            Mathf.Clamp(newPosition.x, minPosition.x, maxPosition.x),
            Mathf.Clamp(newPosition.y, minPosition.y, maxPosition.y),
            Mathf.Clamp(newPosition.z, minPosition.z, maxPosition.z)
        );
        transform.localPosition = newPosition;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var a = actionsOut.ContinuousActions;
        a[0] = Input.GetAxis("Horizontal");
        a[1] = 0f;
        a[2] = Input.GetAxis("Vertical");

        a[3] = 1f;
        a[4] = 1f;
        a[5] = 1f;
        a[6] = -1f;
    }
}
