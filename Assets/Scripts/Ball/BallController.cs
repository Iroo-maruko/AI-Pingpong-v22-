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
    private bool hasOutEventTriggered = false;
    private bool hasServedBounceOccurred = false;

    [Header("References")]
    public AIAgent aiAgent;

    [Header("Launch Settings")]
    public Transform resetPoint;
    public float initialForce = 26f;
    public float launchAngleRange = 20f;

    [Header("Reset Conditions")]
    public float stopThreshold = 5f;
    public float stopCheckDuration = 0.7f;
    public float outOfBoundsY = -3f;
    public float outOfBoundsX = 65f;
    public float outOfBoundsZ = 50f;
    public float maxAllowedHeight = 50f;

    private float slowTimer = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (resetPoint != null)
            transform.position = resetPoint.position;

        LaunchBall();
    }

    private void Update()
    {
        if (isResetting) return;

        Vector3 pos = transform.position;

        bool outOfBounds = pos.y < outOfBoundsY ||
                           pos.y > maxAllowedHeight ||
                           Mathf.Abs(pos.x) > outOfBoundsX ||
                           Mathf.Abs(pos.z) > outOfBoundsZ;

        if (!hasOutEventTriggered && outOfBounds)
        {
            hasOutEventTriggered = true;
            Debug.Log($"⛔️ Ball went out of bounds. (x: {pos.x}, y: {pos.y}, z: {pos.z})");
            GameManager.Instance.OnBallOutOfBounds(serveBy, lastBounceTable);
            HandleRewards();
            return;
        }

        float speed = rb.velocity.magnitude;
        if (speed < stopThreshold && !isServe && !hasOutEventTriggered)
        {
            slowTimer += Time.deltaTime;
            if (slowTimer >= stopCheckDuration)
            {
                hasOutEventTriggered = true;
                Debug.Log("⚠️ Ball is too slow. Resetting...");
                GameManager.Instance.OnBallOutOfBounds(serveBy, lastBounceTable);
                HandleRewards();
                return;
            }
        }
        else
        {
            slowTimer = 0f;
        }
    }

    public void LaunchBall()
    {
        isServe = true;
        hasOutEventTriggered = false;
        hasServedBounceOccurred = false;

        bool toLeft = Random.value < 0.5f;
        serveBy = toLeft ? "Player" : "AI";

        float angle = Random.Range(-launchAngleRange, launchAngleRange);
        Vector3 dir = Quaternion.Euler(0, angle, 0) * (toLeft ? Vector3.right : Vector3.left);
        rb.velocity = dir.normalized * initialForce;
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

        yield return new WaitForSeconds(0.1f);

        LaunchBall();
        slowTimer = 0f;
        isResetting = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isResetting) return;

        string tag = collision.gameObject.tag;

        if (tag == "Paddle")
        {
            string hitter = collision.gameObject.name.Contains("Player") ? "Player" : "AI";
            RegisterHit(hitter);
            GameManager.Instance.OnBallHit();
        }
        else if (tag == "PlayerTable" || tag == "AITable")
        {
            lastBounceTable = tag;

            if (isServe && !hasServedBounceOccurred)
            {
                Debug.Log($"✔️ First serve bounce ({tag})");
                GameManager.Instance.OnBallBounce(true);
                isServe = false;
                hasServedBounceOccurred = true;
            }
            else
            {
                // 🔥 서브 이후 자신의 테이블에 바운스되면 실점
                if (!isServe &&
                    ((lastHitter == "Player" && tag == "PlayerTable") ||
                     (lastHitter == "AI" && tag == "AITable")))
                {
                    Debug.Log($"❌ {lastHitter} hit but landed on own table → point lost");
                    GameManager.Instance.OnBallOutOfBounds(serveBy, lastBounceTable);
                    HandleRewards();
                }
                else
                {
                    Debug.Log($"✅ {lastHitter} successfully bounced on {tag}");
                    GameManager.Instance.OnBallBounce(false);
                }
            }
        }
        else if (tag == "Net")
        {
            Debug.Log("🪢 Ball hit the net.");
            GameManager.Instance.OnBallHitNet();
            HandleRewards();
        }
        else if (tag == "Ground" && !isServe && !hasOutEventTriggered)
        {
            hasOutEventTriggered = true;
            Debug.Log("⛔️ Ball hit the ground.");
            GameManager.Instance.OnBallOutOfBounds(serveBy, lastBounceTable);
            HandleRewards();
        }
    }

    private void HandleRewards()
    {
        aiAgent?.MissedBallPenalty();
    }

    public void RegisterHit(string hitter)
    {
        lastHitter = hitter;
        Debug.Log($"[BallController] lastHitBy set to: {lastHitter}");
        GameManager.Instance?.ResetBounceCount();  // 💡 GameManager.cs에 이 메서드 추가 필요
    }

    public string GetLastHitter() => lastHitter;
    public string GetLastBounceTable() => lastBounceTable;
    public string GetServeBy() => serveBy;
    public Vector3 GetPosition() => transform.position;
    public Vector3 GetVelocity() => rb.velocity;
    public bool IsResetting() => isResetting;
    public bool IsServe() => isServe;
    public void ForceEndServe()
    {
        isServe = false;
        hasServedBounceOccurred = true;
    }
}
