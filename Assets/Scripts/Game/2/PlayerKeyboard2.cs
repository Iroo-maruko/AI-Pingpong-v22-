using UnityEngine;

public class PlayerKeyboardSingle : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 20f;
    public float minZ = -50f;
    public float maxZ = 50f;

    public bool useArrowKeys = false; // 체크하면 화살표키, 해제하면 WASD

    private void Update()
    {
        float input = 0f;

        if (useArrowKeys)
        {
            if (Input.GetKey(KeyCode.LeftArrow)) input = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) input = 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.A)) input = -1f;
            if (Input.GetKey(KeyCode.D)) input = 1f;
        }

        float moveZ = input * moveSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position;
        newPosition.z = Mathf.Clamp(newPosition.z + moveZ, minZ, maxZ);
        transform.position = newPosition;
    }
}
