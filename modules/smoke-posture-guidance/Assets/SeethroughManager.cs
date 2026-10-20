using UnityEngine;
// 1. 引入PICO SDK的命名空间
using Unity.XR.PXR;

public class SeethroughManager : MonoBehaviour
{
    void Start()
    {
        // 2. 在游戏开始时开启视频透视
        PXR_Manager.EnableVideoSeeThrough = true;
    }

    // （可选）当应用从后台恢复时，重新开启透视
    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) // 应用恢复运行
        {
            PXR_Manager.EnableVideoSeeThrough = true;
        }
    }
}