using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动速度")]
    public float speed = 5f; // 可调节移动速度

    void Update()
    {
        // 获取水平输入：左箭头（LeftArrow）和右箭头（RightArrow）
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        if (Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;

        // 获取垂直输入：上箭头（UpArrow）和下箭头（DownArrow）
        float vertical = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
            vertical = 1f;
        if (Input.GetKey(KeyCode.DownArrow))
            vertical = -1f;

        // 计算移动方向（基于物体的局部坐标系）
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        // 移动物体（世界坐标系移动，乘以速度和时间增量）
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }
}