using UnityEngine;

public class PlayerKeyboard : MonoBehaviour
{
    public enum PlayerControlType { Player1, Player2 }
    public PlayerControlType controlType;

    [Header("Movement Settings")]
    public float moveSpeed = 20f;
    public float minZ = -50f;
    public float maxZ = 50f;

    [Header("Ball Tracking")]
    public Transform ball;
    public float followSpeed = 5f;  // 공을 따라가는 속도
    public float minY = 0.5f;
    public float maxY = 7f;

    private void Update()
    {
        float inputZ = 0f;

        // 좌우 수동 조작
        if (controlType == PlayerControlType.Player1)
        {
            if (Input.GetKey(KeyCode.A)) inputZ = 1f;
            if (Input.GetKey(KeyCode.D)) inputZ = -1f;
        }
        else if (controlType == PlayerControlType.Player2)
        {
            if (Input.GetKey(KeyCode.W)) inputZ = -1f;
            if (Input.GetKey(KeyCode.S)) inputZ = 1f;
        }

        Vector3 newPosition = transform.position;

        // Z축 (수동 입력)
        newPosition.z = Mathf.Clamp(newPosition.z + inputZ * moveSpeed * Time.deltaTime, minZ, maxZ);

        // Y축 (공 따라가기)
        if (ball != null)
        {
            float targetY = Mathf.Clamp(ball.position.y, minY, maxY);
            newPosition.y = Mathf.Lerp(transform.position.y, targetY, followSpeed * Time.deltaTime);
        }

        transform.position = newPosition;
    }
}
