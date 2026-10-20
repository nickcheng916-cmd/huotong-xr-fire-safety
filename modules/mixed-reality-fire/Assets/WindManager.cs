using UnityEngine;

public class WindManager : MonoBehaviour
{
    // 单例+静态参数，火焰脚本安全读取
    public static WindManager Instance { get; private set; }
    public static Vector2 CurrentWindDir;
    public static float CurrentWindStrength;

    [Header("风基础设置")]
    public float maxWindStrength = 10f;
    public float windChangeSpeed = 8f;

    [Header("运行时调试（只读）")]
    [SerializeField] private Vector2 currentWindDirDebug;
    [SerializeField] private float currentWindStrengthDebug;

    void Awake()
    {
        // 单例初始化，确保场景里只有一个风管理器
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // ====================== Z/X/C/V 控制风向 ======================
        Vector2 inputDir = Vector2.zero;
        if (Input.GetKey(KeyCode.Z)) inputDir.x = -1; // 左风吹，火焰右偏
        if (Input.GetKey(KeyCode.X)) inputDir.x = 1;  // 右风吹，火焰左偏
        if (Input.GetKey(KeyCode.C)) inputDir.y = -1; // 下风吹，火焰上偏
        if (Input.GetKey(KeyCode.V)) inputDir.y = 1;  // 上风吹，火焰下偏
        CurrentWindDir = inputDir.normalized;

        // ====================== B/N 控制风力大小 ======================
        if (Input.GetKey(KeyCode.N))
        {
            CurrentWindStrength += windChangeSpeed * Time.deltaTime;
            Debug.Log("【按键生效】N键：增大风力");
        }
        if (Input.GetKey(KeyCode.B))
        {
            CurrentWindStrength -= windChangeSpeed * Time.deltaTime;
            Debug.Log("【按键生效】B键：减小风力");
        }

        // 限制风力在0~最大值之间，不会出现负数
        CurrentWindStrength = Mathf.Clamp(CurrentWindStrength, 0, maxWindStrength);

        // 同步调试面板数值
        currentWindDirDebug = CurrentWindDir;
        currentWindStrengthDebug = CurrentWindStrength;
    }

    // 场景可视化风向，方便调试
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 windWorldDir = new Vector3(CurrentWindDir.x, CurrentWindDir.y, 0) * CurrentWindStrength * 0.1f;
            Gizmos.DrawLine(transform.position, transform.position + windWorldDir);
            Gizmos.DrawSphere(transform.position + windWorldDir, 0.2f);
        }
    }
}