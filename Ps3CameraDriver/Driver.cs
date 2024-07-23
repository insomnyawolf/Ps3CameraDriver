using LibUsbDotNet.LibUsb;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    // SCCB https://www.waveshare.com/w/upload/1/14/OmniVision_Technologies_Seril_Camera_Control_Bus%28SCCB%29_Specification.pdf
    // ov534 Ps3Cam Hardware https://jim.sh/svn/jim/devl/playstation/ps3/eye/test/

    private readonly IUsbDevice UsbDevice;

    private VideoResolution VideoResolution;
    private VideoFormat VideoFormat;
    private int FramesPerSecond;

    public Ps3CamDriver(IUsbDevice IUsbDevice)
    {
        UsbDevice = IUsbDevice;
    }

    public void Init(VideoResolution resolution, int framesPerSecond = 60, VideoFormat videoFormat = VideoFormat.BGR)
    {
        UsbDevice.Open();

        var res = UsbDevice.ClaimInterface(0);

        UpdateCameraConfiguration(resolution, framesPerSecond, videoFormat);

        BridgeReset();

        Thread.Sleep(10);

        SensorInitializeAddress();

        SensorReset();

        Thread.Sleep(10);

        // SensorProbe();

        Initialize();
    }

    public void UpdateCameraConfiguration(VideoResolution resolution, int framesPerSecond = 60, VideoFormat videoFormat = VideoFormat.BGR)
    {
        var settings = CameraConfigurations[resolution];

        VideoResolution = settings.VideoResolution;

        VideoFormat = videoFormat;

        var normalizedFrameConfig = settings.GetNormalizedFrameConfig(framesPerSecond);

        FramesPerSecond = normalizedFrameConfig.fps;

        ApplyNewFrameConfig(normalizedFrameConfig);
    }

    // ov534_set_frame_rate
    // validate frame rate and (if not dry run) set it
    public void ApplyNewFrameConfig(NormalizedFrameConfig normalizedFrameConfig)
    {
        normalizedFrameConfig.WriteTo(this);
    }

    public void Initialize()
    {

    }

    public void SensorProbe()
    {
        var data1 = SerialCameraControlBusRegisterRead(0x0a);

        var data2 = SerialCameraControlBusRegisterRead(0x0a);

        var sensorId = data2 << 8;

        var data3 = SerialCameraControlBusRegisterRead(0x0b);

        var data4 = SerialCameraControlBusRegisterRead(0x0b);

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
        SerialCameraControlBusRegisterWrite(0x12, 0x80);
    }

    private bool LedStatus;

    // Just because it helps testing, it works \:D/
    public void ToggleLed()
    {
        LedStatus = !LedStatus;
        SetLed(LedStatus);
    }

    // ov534_set_led();
    public void SetLed(bool on)
    {
        unchecked
        {
            const byte LedMask = 0x80;
            const short LedMaskInverted = ~LedMask;

            var data1 = HardwareRegisterRead(OperationsOV534.Led1);

            data1 |= LedMask;

            HardwareRegisterWrite(OperationsOV534.Led1, data1);


            var data2 = HardwareRegisterRead(OperationsOV534.Led2);

            if (on)
            {
                data2 |= LedMask;

                HardwareRegisterWrite(OperationsOV534.Led2, data2);

                return;
            }

            // !on

            data2 &= (byte)LedMaskInverted;

            HardwareRegisterWrite(OperationsOV534.Led2, data2);

            var data3 = HardwareRegisterRead(OperationsOV534.Led1);

            data3 &= (byte)LedMaskInverted;

            HardwareRegisterWrite(OperationsOV534.Led1, data3);
        }
    }
}