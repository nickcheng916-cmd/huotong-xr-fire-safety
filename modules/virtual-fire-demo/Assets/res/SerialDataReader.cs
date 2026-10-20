using UnityEngine;
using System.IO.Ports; // 必须将 Api Compatibility Level 改为 .NET Framework 才能使用此命名空间
using TMPro;           // 引入 TextMeshPro 命名空间用于 UI 显示

public class SerialDataReader : MonoBehaviour
{
    [Header("串口配置")]
    public string portName = "COM6";  // 已经为你改好了 B 同学电脑上的 COM6
    public int baudRate = 115200;     // 必须与 Arduino 中 Serial.begin(115200) 保持一致

    private SerialPort serialPort;

    [Header("VR UI 文本引用")]
    public TextMeshProUGUI bpmText;       // 用于显示心率
    public TextMeshProUGUI tempText;      // 用于显示温度
    public TextMeshProUGUI humidityText;  // 用于显示湿度

    void Start()
    {
        // 初始化串口对象
        serialPort = new SerialPort(portName, baudRate);

        // 设置读取超时时间（毫秒）。这个非常重要！
        // 如果不设置，当 Arduino 没发数据时，Unity 会卡死等待
        serialPort.ReadTimeout = 50;

        try
        {
            serialPort.Open();
            Debug.Log("串口打开成功！当前连接端口: " + portName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("串口打开失败，请检查端口号或 Arduino 是否被其他程序(如串口监视器)占用。错误信息: " + e.Message);
        }
    }

    void Update()
    {
        // 确保串口已打开
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                // 读取 Arduino 传来的一行数据 (直到遇到换行符)
                string dataString = serialPort.ReadLine();

                // 按逗号拆分数据：[0]是BPM, [1]是温度, [2]是湿度
                string[] dataValues = dataString.Split(',');

                // 确保我们接收到了完整的 3 个数据
                if (dataValues.Length == 3)
                {
                    // 将数据更新到 VR 世界的 UI 上
                    if (bpmText != null) bpmText.text = "心率: " + dataValues[0] + " BPM";
                    if (tempText != null) tempText.text = "温度: " + dataValues[1] + " °C";
                    if (humidityText != null) humidityText.text = "湿度: " + dataValues[2] + " %";
                }
            }
            catch (System.TimeoutException)
            {
                // 读取超时是正常现象（说明这几毫秒内 Arduino 没发新数据），直接忽略即可，防止控制台刷屏报错
            }
            catch (System.Exception e)
            {
                // 捕捉其他异常，方便排错
                Debug.LogWarning("读取串口数据时发生异常: " + e.Message);
            }
        }
    }

    void OnDestroy()
    {
        // 【关键安全措施】
        // 当关闭游戏运行或切换场景时，务必关闭串口。
        // 否则后台会一直占用 COM6，导致你下次运行 Unity 或打开 Arduino 串口监视器时报错“拒绝访问”
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("串口已安全关闭");
        }
    }
}