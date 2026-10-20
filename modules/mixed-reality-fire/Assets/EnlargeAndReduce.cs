using UnityEngine;

public class EnlargeAndReduce : MonoBehaviour
{
    public GameObject targetObject;
    public float scaleSpeed = 1f;
    public float smoothTime = 0.3f;  // 平滑过渡时间
    public float minScale = 0.1f;
    public float maxScale = 5f;

    private float targetScale = 1f;
    private float currentScaleVelocity = 0f;

    void Start()
    {
        if (targetObject != null)
        {
            targetScale = targetObject.transform.localScale.x;
        }
    }

    void Update()
    {
        if (targetObject == null) return;

        // 加号键放大（需要同时按Shift键）
        if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus) ||
            (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Equals)))
        {
            targetScale += scaleSpeed * Time.deltaTime;
        }

        // 减号键缩小
        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            targetScale -= scaleSpeed * Time.deltaTime;
        }

        // 或者使用R/F键
        if (Input.GetKey(KeyCode.R))
        {
            targetScale += scaleSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.F))
        {
            targetScale -= scaleSpeed * Time.deltaTime;
        }

        // 限制缩放范围
        targetScale = Mathf.Clamp(targetScale, minScale, maxScale);

        // 平滑过渡到目标缩放
        float currentScale = Mathf.SmoothDamp(
            targetObject.transform.localScale.x,
            targetScale,
            ref currentScaleVelocity,
            smoothTime
        );

        targetObject.transform.localScale = Vector3.one * currentScale;
    }
}