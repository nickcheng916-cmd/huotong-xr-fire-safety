using UnityEngine;

public class WASDPlayer : MonoBehaviour
{
    [Header("移动速度")]
    public float speed = 5f;

    [Header("碰撞设置")]
    public string ignoreTag = "Ground";      // 忽略碰撞的标签
    public AudioClip collisionSound;         // 碰撞音效
    [Range(0f, 1f)] public float volume = 1f;

    private Rigidbody rb;
    private Renderer renderer;
    private Color originalColor;
    private int nonGroundCollisions = 0;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        renderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (rb == null)
            Debug.LogError("Rigidbody component missing!");
        else
            rb.freezeRotation = true;

        if (renderer != null)
            originalColor = renderer.material.color;
    }

    void Update()
    {
        // WASD 移动控制
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;
        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;

        Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

        if (rb != null)
            rb.velocity = movement * speed;
        else
            transform.Translate(movement * speed * Time.deltaTime, Space.World);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(ignoreTag))
            return;

        Debug.Log($"{gameObject.name} collided with {collision.gameObject.name}");

        if (collisionSound != null)
            audioSource.PlayOneShot(collisionSound, volume);

        nonGroundCollisions++;
        if (nonGroundCollisions == 1 && renderer != null)
            renderer.material.color = Color.red;    // 改为红色
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(ignoreTag))
            return;

        nonGroundCollisions--;
        if (nonGroundCollisions == 0 && renderer != null)
            renderer.material.color = originalColor;
    }
}