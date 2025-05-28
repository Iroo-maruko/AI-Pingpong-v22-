using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    private Rigidbody rb;
    public Transform resetPoint;
    public float initialForce = 19f;
    public float launchAngleRange = 30f;
    public float upwardLaunchOffset = 0.3f;

    private bool isResetting = false;

    public string lastHitter = "None";
    public string lastBounceTable = "None";

    public static System.Action<string> OnPaddleHit;
    public static System.Action<string> OnTableBounce;
    public static System.Action<string> OnOutOfBounds;
    public static System.Action<string> OnServeStarted;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        transform.position = resetPoint.position;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();
    }

    private void Update()
    {
        if (isResetting) return;

        Vector3 pos = transform.position;
        if (pos.y < -3f || Mathf.Abs(pos.x) > 65f || Mathf.Abs(pos.z) > 50f || pos.y > 150f)
        {
            OnOutOfBounds?.Invoke(lastBounceTable);
            StartCoroutine(ResetPosition());
        }
    }

    public void LaunchBall(string by)
    {
        rb.WakeUp();

        bool toLeft = by == "Player";
        lastHitter = Opponent(by);

        float angle = Random.Range(-launchAngleRange, launchAngleRange);
        Vector3 dir = Quaternion.Euler(0, angle, 0) * (toLeft ? Vector3.right : Vector3.left);
        dir = (dir + Vector3.up * upwardLaunchOffset).normalized;

        rb.velocity = dir * initialForce;

        OnServeStarted?.Invoke(by);
        Debug.Log($"🚀 Ball launched by {by} to {(toLeft ? "right" : "left")} with velocity {rb.velocity}");
    }

    public IEnumerator ResetPosition()
    {
        isResetting = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();

        transform.position = resetPoint.position;

        yield return new WaitForSeconds(0.8f);

        rb.WakeUp();
        isResetting = false;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (isResetting) return;

        if (col.collider.CompareTag("Paddle"))
        {
            string hitter = col.collider.name.Contains("Player") ? "Player" : "AI";
            lastHitter = hitter;
            OnPaddleHit?.Invoke(hitter);
            Debug.Log($"🎯 공이 {hitter}의 패들에 맞음");
        }
        else if (col.collider.CompareTag("PlayerTable") || col.collider.CompareTag("AITable"))
        {
            string tableTag = col.collider.tag;
            lastBounceTable = tableTag;
            OnTableBounce?.Invoke(tableTag);
            Debug.Log($"🟩 공이 {tableTag}에 바운스됨");
        }
    }

    private string Opponent(string p) => p == "Player" ? "AI" : "Player";
}
