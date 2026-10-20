using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class FireStarter : MonoBehaviour
{
    [Header("控制器设置")]
    public XRNode controllerNode = XRNode.RightHand;

    [Header("射线设置")]
    public Transform rayOrigin;
    public float rayDistance = 10f;
    public LayerMask hitLayers = ~0;

    [Header("调试")]
    public bool drawDebugRay = true;
    public bool enableKeyboardTest = false;

    [Header("输入阈值")]
    [Range(0.1f, 1f)]
    public float triggerThreshold = 0.7f;

    private InputDevice controllerDevice;
    private bool currentPressed = false;
    private bool lastPressed = false;

    // 防止设备启动时误判按下
    private bool hasSeenRelease = false;

    private void OnEnable()
    {
        InitializeDevice();
    }

    private void Update()
    {
        if (!controllerDevice.isValid)
        {
            InitializeDevice();
        }

        UpdateTriggerState();

        // 只有先检测到“松开”，后面才允许点火
        if (!hasSeenRelease)
        {
            if (!currentPressed)
            {
                hasSeenRelease = true;
                Debug.Log("FireStarter: 已检测到初始松开状态，允许点火");
            }

            lastPressed = currentPressed;
            return;
        }

        // 按下瞬间触发
        if (currentPressed && !lastPressed)
        {
            TryIgnite();
        }

        lastPressed = currentPressed;

        if (enableKeyboardTest && Input.GetKeyDown(KeyCode.F))
        {
            TryIgnite();
        }

        if (drawDebugRay && rayOrigin != null)
        {
            Debug.DrawRay(rayOrigin.position, rayOrigin.forward * rayDistance, Color.red);
        }
    }

    private void InitializeDevice()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(controllerNode, devices);

        if (devices.Count > 0)
        {
            controllerDevice = devices[0];
            Debug.Log("已连接控制器: " + controllerDevice.name);
        }
    }

    private void UpdateTriggerState()
    {
        currentPressed = false;

        if (!controllerDevice.isValid) return;

        if (controllerDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButtonPressed))
        {
            currentPressed = triggerButtonPressed;
            return;
        }

        if (controllerDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            currentPressed = triggerValue >= triggerThreshold;
        }
    }

    private void TryIgnite()
    {
        if (rayOrigin == null)
        {
            Debug.LogWarning("FireStarter: rayOrigin 没有设置");
            return;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, hitLayers))
        {
            Flammable flammable = hit.collider.GetComponent<Flammable>();

            if (flammable == null)
            {
                flammable = hit.collider.GetComponentInParent<Flammable>();
            }

            if (flammable != null && !flammable.isOnFire)
            {
                flammable.Ignite();
                Debug.Log("点燃物体: " + flammable.gameObject.name);
            }
            else
            {
                Debug.Log("击中了物体，但没有 Flammable: " + hit.collider.name);
            }
        }
    }
}