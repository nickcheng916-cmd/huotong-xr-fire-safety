using UnityEngine;

public class FlameFollower : MonoBehaviour
{
    [Header("检测设置")]
    public float detectionRadius = 5f;          // 自动检测半径（若未指定目标）
    public LayerMask targetLayer = -1;           // 目标层级（默认所有层，建议设为 Player）
    public Transform target;                     // 可手动指定目标（优先级高于自动检测）

    [Header("移动设置")]
    public float followSpeed = 3f;
    public bool usePhysics = false;              // 是否使用 Rigidbody 移动（避免穿透）
    public float maxDistance = 10f;              // 超过此距离则停止跟随

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (usePhysics && rb == null)
        {
            Debug.LogWarning("usePhysics 为 true 但物体没有 Rigidbody，将改用 Transform 移动");
            usePhysics = false;
        }
    }

    void Update()
    {
        // 如果没有手动指定目标，尝试自动搜索
        if (target == null)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);
            if (hits.Length > 0)
            {
                target = hits[0].transform;
                Debug.Log($"找到目标: {target.name}");
            }
            else
            {
                // 无目标时保持静止
                return;
            }
        }

        // 检查目标是否超出跟随范围
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > maxDistance)
        {
            target = null;   // 超出范围，放弃跟随
            return;
        }

        // 移动物体
        MoveTowards(target.position);
    }

    void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 moveStep = direction * followSpeed * Time.deltaTime;

        if (usePhysics && rb != null)
        {
            // 物理移动：使用 velocity（保持动量）或 MovePosition（更精确）
            rb.MovePosition(transform.position + moveStep);
        }
        else
        {
            transform.position += moveStep;
        }
    }

    // 可选：通过触发器手动设置目标
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            target = other.transform;
            Debug.Log($"触发器触发，目标设置为: {other.name}");
        }
    }

    // 可选：离开触发器时清除目标
    void OnTriggerExit(Collider other)
    {
        if (target != null && other.transform == target)
        {
            target = null;
            Debug.Log("目标离开触发器，停止跟随");
        }
    }

    // 可视化调试（Scene 视图）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}