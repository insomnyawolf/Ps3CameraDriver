using LibUsbDotNet.LibUsb;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    // SCCB https://www.waveshare.com/w/upload/1/14/OmniVision_Technologies_Seril_Camera_Control_Bus%28SCCB%29_Specification.pdf
    // ov534 Ps3Cam Hardware https://jim.sh/svn/jim/devl/playstation/ps3/eye/test/

    private readonly IUsbDevice UsbDevice;

    private InternalFrameConfiguration InternalFrameConfigurationCache;
    private FrameConfiguration FrameConfigurationCache;
    private NormalizedFrameConfig NormalizedFrameConfigurationCache;

    private bool IsInitialized;
    private bool IsStreaming;

    public Ps3CamDriver(IUsbDevice IUsbDevice)
    {
        UsbDevice = IUsbDevice;
    }

    public void Init(FrameConfiguration frameConfiguration)
    {
        UsbDevice.Open();

        var res = UsbDevice.ClaimInterface(0);

        UpdateCameraConfiguration(frameConfiguration);

        BridgeReset();

        Thread.Sleep(10);

        SensorInitializeAddress();

        SensorReset();

        Thread.Sleep(10);

        // SensorProbe();

        Initialize();
    }

    public void UpdateCameraConfiguration(FrameConfiguration frameConfiguration)
    {
        FrameConfigurationCache = frameConfiguration;
        InternalFrameConfigurationCache = FrameConfigurations[frameConfiguration.Resolution];
        NormalizedFrameConfigurationCache = InternalFrameConfigurationCache.GetNormalizedFrameConfig(frameConfiguration.FramesPerSecond);
    }

    // ov534_set_frame_rate
    // validate frame rate and (if not dry run) set it
    public void ApplyNewFrameConfig()
    {
        NormalizedFrameConfigurationCache.WriteTo(this);
    }

    public void Initialize()
    {
        HardwareRegisterWriteArray(Ov534_RegistrerInitData);

        SetLed(true);

        SerialCameraControlBusWriteArray(Ov772x_RegistrerInitData);

        HardwareRegisterWrite(OperationsOV534.Bridge2, 0x09);

        SetLed(false);

        IsInitialized = true;
    }

    public void Start()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (IsStreaming)
        {
            return;
        }

        HardwareRegisterWriteArray(InternalFrameConfigurationCache.BridgeStart);

        SerialCameraControlBusWriteArray(InternalFrameConfigurationCache.SensorStart);

        ApplyNewFrameConfig();

#warning image config here

        SetLed(true);

        // Start stream
        HardwareRegisterWrite(OperationsOV534.Bridge2, 0x00);

        var size = GetSize();

        var bufferLength = size.GetBufferLength();

        IsStreaming = true;
    }

    public void SensorProbe()
    {
        var data1 = SerialCameraControlBusRegisterRead((RegisterOV534)0x0a);

        var data2 = SerialCameraControlBusRegisterRead((RegisterOV534)0x0a);

        var sensorId = data2 << 8;

        var data3 = SerialCameraControlBusRegisterRead((RegisterOV534)0x0b);

        var data4 = SerialCameraControlBusRegisterRead((RegisterOV534)0x0b);

        var sensorId2 = sensorId | data4;

        Console.WriteLine($"Sensor Id => {sensorId2}");
    }

    public void BridgeReset()
    {
        HardwareRegisterWrite(OperationsOV534.Bridge1, 0x3a);
        HardwareRegisterWrite(OperationsOV534.Bridge2, 0x08);
    }

    public void SensorInitializeAddress()
    {
        HardwareRegisterWrite(OperationsOV534.REG_ADDRESS, 0x42);
    }

    public void SensorReset()
    {
        SerialCameraControlBusRegisterWrite((RegisterOV534)0x12, 0x80);
    }

    public VideoSize GetSize()
    {
        return InternalFrameConfigurationCache.VideoSize;
    }
}