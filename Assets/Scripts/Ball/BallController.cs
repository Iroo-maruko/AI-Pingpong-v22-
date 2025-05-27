using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    private Rigidbody rb;
    private string lastHitter = "None";
    private string serveBy = "None";
    private string lastBounceTable = "None";
    private bool isResetting = false;
    private bool isServe = true;
    private bool aiHasHit = false;
    private bool pointProcessed = false;
    private bool hasLaunched = false;

    [Header("References")]
    public AIAgent aiAgent;

    [Header("Launch Settings")]
    public Transform resetPoint;
    public float initialForce = 19f;
    public float launchAngleRange = 30f;
    public float upwardLaunchOffset = 0.3f;

    [Header("Reset Conditions")]
    public float stopThreshold = 5f;
    public float stopCheckDuration = 0.7f;
    public float outOfBoundsY = -3f;
    public float outOfBoundsX = 65f;
    public float outOfBoundsZ = 50f;
    public float maxAllowedHeight = 150f;

    private float slowTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (resetPoint != null)
            transform.position = resetPoint.position;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();
    }

    private void Update()
    {
        if (isResetting || pointProcessed) return;

        float speed = rb.velocity.magnitude;
        if (speed < stopThreshold && !isServe)
        {
            slowTimer += Time.deltaTime;
            if (slowTimer >= stopCheckDuration && !pointProcessed)
            {
                pointProcessed = true;
                StartCoroutine(HandlePointOut(lastBounceTable));
            }
        }
        else
        {
            slowTimer = 0f;
        }

        Vector3 pos = transform.position;
        if (!pointProcessed && (
            pos.y < outOfBoundsY ||
            pos.y > maxAllowedHeight ||
            Mathf.Abs(pos.x) > outOfBoundsX ||
            Mathf.Abs(pos.z) > outOfBoundsZ))
        {
            pointProcessed = true;
            StartCoroutine(HandlePointOut(lastBounceTable));
        }
    }

    private IEnumerator HandlePointOut(string lastBounce)
    {
        GameManager.Instance.OnBallOutOfBounds(lastBounce);
        yield return new WaitForSeconds(0.5f);
        ResetBall(GameManager.Instance.centerPoint.position);
    }

    public void LaunchBall()
    {
        if (hasLaunched) return;
        hasLaunched = true;

        isServe = true;
        aiHasHit = false;
        pointProcessed = false;
        lastHitter = "None";
        lastBounceTable = "None";

        bool toLeft = Random.value < 0.5f;
        serveBy = toLeft ? "Player" : "AI";

        float angle = Random.Range(-launchAngleRange, launchAngleRange);
        Vector3 horizontalDir = Quaternion.Euler(0, angle, 0) * (toLeft ? Vector3.right : Vector3.left);
        Vector3 finalDir = (horizontalDir + Vector3.up * upwardLaunchOffset).normalized;

        rb.velocity = finalDir * initialForce;
    }

    public void ResetBall(Vector3 position)
    {
        StartCoroutine(ResetRoutine(position));
    }

    private IEnumerator ResetRoutine(Vector3 position)
    {
        isResetting = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();

        yield return new WaitForFixedUpdate();
        transform.position = position;
        rb.WakeUp();

        yield return new WaitForSeconds(0.15f);
        hasLaunched = false;
        LaunchBall();

        slowTimer = 0f;
        isResetting = false;
    }

    private void RegisterTableBounce(string tableTag)
    {
        lastBounceTable = tableTag;
        GameManager.Instance.OnBallBounce(tableTag, isServe);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isResetting || pointProcessed) return;

        string tag = collision.gameObject.tag;

        if (tag == "Paddle")
        {
            string hitter = collision.gameObject.name.Contains("Player") ? "Player" : "AI";
            RegisterHit(hitter);
            GameManager.Instance.OnBallHit();
        }
        else if (tag == "PlayerTable" || tag == "AITable")
        {
            isServe = false;
            RegisterTableBounce(tag);
        }
        else if (tag == "Ground")
        {
            pointProcessed = true;
            StartCoroutine(HandlePointOut(lastBounceTable));
        }
    }

    public void RegisterHit(string hitter)
    {
        lastHitter = hitter;
        isServe = false;

        if (hitter == "AI") aiHasHit = true;
        GameManager.Instance?.ResetBounceCount();
    }

    public void ForceEndServe() => isServe = false;
    public string GetLastHitter() => lastHitter;
    public string GetLastBounceTable() => lastBounceTable;
    public string GetServeBy() => serveBy;
    public Vector3 GetPosition() => transform.position;
    public Vector3 GetVelocity() => rb.velocity;
    public bool IsResetting() => isResetting;
    public bool IsServe() => isServe;
    public bool AIHasHit() => aiHasHit;
    public void SetAIHasHit(bool value) => aiHasHit = value;

    public void SetLastBounceTable(string tag) => lastBounceTable = tag;
}
