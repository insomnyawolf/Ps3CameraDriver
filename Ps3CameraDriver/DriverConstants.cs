using LibUsbDotNet;
using Ps3CameraDriver.Models;
using Ps3CameraDriver.Protocol;
using VirtualCameraCommon;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    // SCCB https://www.waveshare.com/w/upload/1/14/OmniVision_Technologies_Seril_Camera_Control_Bus%28SCCB%29_Specification.pdf
    // ov534 Ps3Cam Hardware https://jim.sh/svn/jim/devl/playstation/ps3/eye/test/
    // OV7725 Datasheet https://pdf1.alldatasheet.com/datasheet-pdf/view/312422/OMNIVISION/OV7725.html

    public const ushort Timeout = 500;
    public const ushort BufferSize = 1024;
    public const int StatusCheckMaxRetry = 5;
    public const int MaxFramesInBuffer = 5;
    //public const int MaxFramesInBuffer = 50;

    const byte BaseRequestType = (byte)RequestType.Vendor | (byte)RequestRecipient.Device;
    const byte OutgoingRequestType = (byte)EndpointDirection.Out | BaseRequestType;
    const byte IncomingRequestType = (byte)EndpointDirection.In | BaseRequestType;

    private static readonly byte[] UsbBuffer = new byte[64];

    public IReadOnlyList<Command> Ov534_RegisterInitData = new List<Command>()
    {
        new Command(0xe7, 0x3a),
        // select OV772x sensor
        new Command((byte)OperationsOV534.REG_ADDRESS, 0x42),
        new Command(0x92, 0x01),
        new Command(0x93, 0x18),
        new Command(0x94, 0x10),
        new Command(0x95, 0x10),
        new Command(0xE2, 0x00),
        new Command(0xE7, 0x3E),
        new Command(0x96, 0x00),
        new Command(0x97, 0x20),
        new Command(0x97, 0x20),
        new Command(0x97, 0x20),
        new Command(0x97, 0x0A),
        new Command(0x97, 0x3F),
        new Command(0x97, 0x4A),
        new Command(0x97, 0x20),
        new Command(0x97, 0x15),
        new Command(0x97, 0x0B),
        new Command(0x8E, 0x40),
        new Command(0x1F, 0x81),
        new Command(0xC0, 0x50),
        new Command(0xC1, 0x3C),
        new Command(0xC2, 0x01),
        new Command(0xC3, 0x01),
        new Command(0x50, 0x89),
        new Command(0x88, 0x08),
        new Command(0x8D, 0x00),
        new Command(0x8E, 0x00),
        //video data start (V_FMT)
        new Command(0x1C, 0x00),
        //RAW8 mode
        new Command(0x1D, 0x00),
        //payload size 0x0200 * 4 = 2048 bytes
        new Command(0x1D, 0x02),
        //payload size
        new Command(0x1D, 0x00),
        //frame size = 0x012C00 * 4 = 307200 bytes (640 * 480 @ 8bpp)
        new Command(0x1D, 0x01),
        //frame size
        new Command(0x1D, 0x2C),
        //frame size
        new Command(0x1D, 0x00),
        //video data start (V_CNTL0)
        new Command(0x1C, 0x0A),
        //turn on UVC header
        new Command(0x1D, 0x08),
        new Command(0x1D, 0x0E),
        new Command(0x34, 0x05),
        new Command(0xE3, 0x04),
        new Command(0x89, 0x00),
        new Command(0x76, 0x00),
        new Command(0xE7, 0x2E),
        new Command(0x31, 0xF9),
        new Command(0x25, 0x42),
        new Command(0x21, 0xF0),
        new Command(0xE5, 0x04),
    };

    public IReadOnlyList<Command> Ov772x_RegisterInitData = new List<Command>()
    {
        // reset
        new Command(0x12, 0x80),
        new Command(0x3D, 0x00),
        new Command(0x12, 0x01),
        // Processed Bayer RAW (8bit) 
        new Command(0x11, 0x01),
        new Command(0x14, 0x40),
        new Command(0x15, 0x00),
        new Command(0x63, 0xAA),
        // AWB
        // W/B defect auto correction[0-1] Gamma[2] Interpolation[3]
        new Command(0x64, 0xDF),
        new Command(0x66, 0x00),
        new Command(0x67, 0x02),
        new Command(0x17, 0x26),
        new Command(0x18, 0xA0),
        new Command(0x19, 0x07),
        new Command(0x1A, 0xF0),
        new Command(0x29, 0xA0),
        new Command(0x2A, 0x00),
        new Command(0x2C, 0xF0),
        new Command(0x20, 0x10),
        new Command(0x4E, 0x0F),
        new Command(0x3E, 0xF3),
        new Command(0x0D, 0x41),
        new Command(0x32, 0x00),
        new Command(0x13, 0xF0),
        // COM8  - jfrancois 0xf0	orig x0f7
        new Command(0x22, 0x7F),
        new Command(0x23, 0x03),
        new Command(0x24, 0x40),
        new Command(0x25, 0x30),
        new Command(0x26, 0xA1),
        new Command(0x2A, 0x00),
        new Command(0x2B, 0x00),
        new Command(0x13, 0xF7),
        new Command(0x0C, 0xC0),
        new Command(0x11, 0x00),
        new Command(0x0D, 0x41),
        new Command(0x8E, 0x00),
	    // De-noise threshold - jfrancois 0x00 - orig 0x04
	    new Command(0xAC, 0xBF),
    };

    public static readonly IReadOnlyList<CameraConfigurations> CameraConfigurationsList = new List<CameraConfigurations>()
    {
        new CameraConfigurations()
        {
            VideoSize = VideoSize.QVGA,
            SensorConfiguration = new SensorConfiguration[]
            {
                new SensorConfiguration { FramesPerSecond = 290, Field1 = 0x00, Field2 = 0xc1, Field3 = 0x04 },
                // 205 FPS or above: video is partly corrupt
                new SensorConfiguration { FramesPerSecond = 205, Field1 = 0x01, Field2 = 0xc1, Field3 = 0x02 },
                // 187 FPS or below: video is valid
                new SensorConfiguration { FramesPerSecond = 187, Field1 = 0x01, Field2 = 0x81, Field3 = 0x02 },
                new SensorConfiguration { FramesPerSecond = 150, Field1 = 0x00, Field2 = 0x41, Field3 = 0x04 },
                new SensorConfiguration { FramesPerSecond = 137, Field1 = 0x02, Field2 = 0xc1, Field3 = 0x02 },
                new SensorConfiguration { FramesPerSecond = 125, Field1 = 0x01, Field2 = 0x41, Field3 = 0x02 },
                new SensorConfiguration { FramesPerSecond = 100, Field1 = 0x02, Field2 = 0xc1, Field3 = 0x04 },
                new SensorConfiguration { FramesPerSecond = 090, Field1 = 0x03, Field2 = 0x81, Field3 = 0x02 },
                new SensorConfiguration { FramesPerSecond = 075, Field1 = 0x04, Field2 = 0x81, Field3 = 0x02 },
                new SensorConfiguration { FramesPerSecond = 060, Field1 = 0x04, Field2 = 0xc1, Field3 = 0x04 },
                new SensorConfiguration { FramesPerSecond = 050, Field1 = 0x04, Field2 = 0x41, Field3 = 0x02 },
                new SensorConfiguration { FramesPerSecond = 040, Field1 = 0x06, Field2 = 0x81, Field3 = 0x03 },
                new SensorConfiguration { FramesPerSecond = 037, Field1 = 0x03, Field2 = 0x41, Field3 = 0x04 },
                new SensorConfiguration { FramesPerSecond = 030, Field1 = 0x04, Field2 = 0x41, Field3 = 0x04 },
            },
            BridgeStart = new List<Command>()
            {
                new Command(0x1c, 0x00),
                new Command(0x1d, 0x00),
                new Command(0x1d, 0x02),
                new Command(0x1d, 0x00),
                // frame size = 0x004B00 * 4 = 76800 bytes (320 * 240 @ 8bpp)
                new Command(0x1d, 0x00),
                new Command(0x1d, 0x4b),
                // frame size
                new Command(0x1d, 0x00),
                // frame size
                new Command(0xc0, 0x28),
                new Command(0xc1, 0x1e),
            },
            SensorStart = new List<Command>()
            {
                new Command(0x12, 0x41),
                new Command(0x17, 0x3f),
                new Command(0x18, 0x50),
                new Command(0x19, 0x03),
                new Command(0x1a, 0x78),
                new Command(0x29, 0x50),
                new Command(0x2c, 0x78),
                new Command(0x65, 0x2f),
            },
        },
        new CameraConfigurations()
        {
            VideoSize = VideoSize.VGA,
            SensorConfiguration = new SensorConfiguration[]
            {
                // 83 FPS: video is partly corrupt
                new SensorConfiguration { FramesPerSecond = 83, Field1 = 0x01, Field2 = 0xc1, Field3 = 0x02 },
                // 75 FPS or below: video is valid
                new SensorConfiguration { FramesPerSecond = 75, Field1 = 0x01, Field2 = 0x81, Field3 = 0x02 },
                new SensorConfiguration { FramesPerSecond = 60, Field1 = 0x00, Field2 = 0x41, Field3 = 0x04 },
                new SensorConfiguration { FramesPerSecond = 50, Field1 = 0x01, Field2 = 0x41, Field3 = 0x02 },
                new SensorConfiguration { FramesPerSecond = 40, Field1 = 0x02, Field2 = 0xc1, Field3 = 0x04 },
                new SensorConfiguration { FramesPerSecond = 30, Field1 = 0x04, Field2 = 0x81, Field3 = 0x02 },
                new SensorConfiguration { FramesPerSecond = 15, Field1 = 0x03, Field2 = 0x41, Field3 = 0x04 },
            },
            BridgeStart = new List<Command>()
            {
                new Command(0x1c, 0x00),
                new Command(0x1d, 0x00),
                new Command(0x1d, 0x02),
                new Command(0x1d, 0x00),
                // frame size = 0x012C00 * 4 = 307200 bytes (640 * 480 @ 8bpp)
                new Command(0x1d, 0x01),
                new Command(0x1d, 0x2C),
                // frame size
                new Command(0x1d, 0x00),
                // frame size
                new Command(0xc0, 0x50),
                new Command(0xc1, 0x3c),
            },
            SensorStart = new List<Command>()
            {
                new Command(0x12, 0x01),
                new Command(0x17, 0x26),
                new Command(0x18, 0xa0),
                new Command(0x19, 0x07),
                new Command(0x1a, 0xf0),
                new Command(0x29, 0xa0),
                new Command(0x2c, 0xf0),
                new Command(0x65, 0x20),
            },
        },
    };

    public static readonly IReadOnlyDictionary<VideoSize, CameraConfigurations> CameraConfigurations = CameraConfigurationsList.ToDictionary(i => i.VideoSize);
}