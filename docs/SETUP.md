# 环境与导入说明

## Unity 工程恢复

原技术文档写的是 Unity 2022，导出包中同时出现 XR Interaction Toolkit 3.4.0 示例目录；二者不能直接视为已验证的兼容组合。原项目精确编辑器版本、PICO SDK 版本、包锁定、XR 插件设置、输入设置和构建设置均未提供。请从原开发机恢复这些文件后进行正式构建；如新建工程，先以对应 SDK 官方支持的编辑器版本做兼容性验证。

导入时保留 `.meta` 与 GUID。每个 `modules/<功能>/Assets` 是独立快照，不要同时导入四份。不应把仓库根目录作为完整 Unity 项目打开。

| 快照 | 包含的项目场景 | 主要依赖或注意点 |
| --- | --- | --- |
| mixed-reality-fire | `Assets/Scenes/SampleScene.unity` | PICO Unity Integration SDK；透视能力依设备与 SDK 配置 |
| smoke-posture-guidance | `Assets/Scenes/SampleScene.unity` | PICO SDK、TextMesh Pro、Unity UI |
| posture-tracking-feedback | 未提供项目场景 | PICO SDK、TextMesh Pro、Unity UI；需要手动布置 UI 与组件 |
| virtual-fire-demo | 仅有 SDK/TMP 示例场景，未提供项目自身场景 | XR Interaction Toolkit 示例目录为 3.4.0；串口读取组件需 `System.IO.Ports` 与匹配 API 配置 |

资源还包括原导出包附带的 Unity Standard Assets、示例脚本和其他依赖。导出包不保证所有材质、预制体和 SDK 引用都已闭合。导入后逐项处理缺失脚本/材质引用，使用 Console 验证，不能仅凭本仓库的静态检查判定构建可用。

## ESP32 固件

1. Arduino IDE 安装 Espressif ESP32 板支持，选择与实际开发板相符的板型。
2. 安装 `PulseSensor Playground`、Adafruit `DHT sensor library` 和其 `Adafruit Unified Sensor` 依赖。原交付未说明精确版本，需实机验证 ESP32 ADC 与库的采样/中断兼容性。
3. 打开 `firmware/esp32_biofeedback/esp32_biofeedback.ino`，将同目录 `config.example.h` 复制为 `config.h`，填写局域网 SSID、密码和头显 IP。
4. DHT22 数据脚接 GPIO4，脉搏模拟输出接 GPIO32，按实际模块规格供电并共地。脉搏阈值初始为 2000，需根据实测信号校准。
5. 烧录后查看 115200 波特率串口。固件沿用原文档逻辑：DHT22 每 2 秒更新，约每 50ms 发送一次缓存值；Wi-Fi 连接和采样逻辑尚需硬件联调。温湿度读取失败会保留旧值，不能把缓存值当作新读数。

`config.h` 已加入忽略规则，不要提交真实 Wi-Fi 密码。

## UDP 联调

| 用途 | 默认端口 | 负载 |
| --- | --- | --- |
| ESP32 → Unity 心率 UI | 8080 | `BPM,temperature,humidity`，例如 `78,26.50,48.00` |
| Kinect 采集端 → Unity 姿态接收 | 5005 | `KINECT_ESCAPE_POSE_V1` JSON |
| PICO → PC 火源距离 | 6006 | `PICO_FIRE_DISTANCE_V1` JSON |
| PC → PICO 评分 UI | 7007 | `FIRE_ESCAPE_SCORE_V1` JSON |

头显、PC、ESP32 处于同一可互通局域网。按实际地址修改 Inspector 中的 `pcIp` 和固件的 `PICO_IP`。避免访客网络客户端隔离；通过操作系统正常防火墙规则允许所需 UDP 端口。

**已有场景/预制体会覆盖脚本默认值**：将心率接收组件 Inspector 中的 `listenPort` 明确设为 `8080`，不要只依赖本次修改的源码默认值。Kinect 接收继续使用 5005。

追踪模块中，`UserPositionProvider`、火源/玩家 Transform 与评估/评分 UI 应按组件 Inspector 引用逐项绑定。姿态采集和评分发送的外部进程未提供；仓库中的接收器不会自动启动这些服务。

仅需测试心率时，在带 Canvas 的 Unity 场景中添加 `UDPReceiver_BPMOnly` 组件，绑定现有文本 UI 或启用自动 UI，运行后用 Python 工具发送到 PC/头显 IP 的 8080 端口。停止发送 3 秒后，组件应显示等待/断开状态。

## 正式演示前

恢复并固定 Unity 版本、依赖清单和项目设置；完成编辑器编译、Android/PICO 构建、传感器校准与多人网络联调。请按黑客松规则说明原有作品基础与赛期新增内容。
