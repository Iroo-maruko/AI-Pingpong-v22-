using UnityEngine;

public class AIPlayerController : MonoBehaviour
{
    public Transform ball;
    public float moveSpeed = 3f;

    private void Update()
    {
        if (ball == null) return;

        Vector3 target = new Vector3(transform.position.x, ball.position.y, ball.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }
}
