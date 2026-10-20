using UnityEngine;

public class IJKLPlayer : MonoBehaviour
{
    public float speed = 5f;
    public string ignoreTag = "Ground";
    public AudioClip collisionSound;
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
        // IJKL 移动控制
        float moveX = 0f;
        float moveZ = 0f;
        if (Input.GetKey(KeyCode.J)) moveX = -1f;
        if (Input.GetKey(KeyCode.L)) moveX = 1f;
        if (Input.GetKey(KeyCode.I)) moveZ = 1f;
        if (Input.GetKey(KeyCode.K)) moveZ = -1f;

        Vector3 movement = new Vector3(moveX, 0f, moveZ).normalized;

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
            renderer.material.color = Color.yellow;
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