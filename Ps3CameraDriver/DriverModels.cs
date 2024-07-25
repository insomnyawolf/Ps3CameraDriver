﻿namespace Ps3CameraDriver;

public struct FrameConfiguration
{
    public VideoResolution Resolution;
    public ColorFormat ColorFormat;
    public int FramesPerSecond;

    public VideoSize VideoSize;
    public byte BytesPerPixel;
    public int PixelCount;
    public int FrameBufferSize;
    /// <summary>
    /// The stride is the number of bytes from one row of pixels in memory to the next row of pixels in memory.
    /// </summary>
    public int ImageStride;

    //InternalFrameConfigurationCache.VideoSize

    public static FrameConfiguration Default => new FrameConfiguration()
    {
        ColorFormat = ColorFormat.RGB,
        Resolution = VideoResolution.VGA,
        FramesPerSecond = 60,
    };

    public static FrameConfiguration LowResLowFramerate => new FrameConfiguration()
    {
        ColorFormat = ColorFormat.RGB,
        Resolution = VideoResolution.QVGA,
        FramesPerSecond = 30,
    };

    public void Initialize()
    {
        BytesPerPixel = GetBytesPerPixel(ColorFormat);
        ImageStride = BytesPerPixel * VideoSize.Width;
        PixelCount = VideoSize.GetPixelCount();
        FrameBufferSize = GetBufferSize();
    }

    private int GetBufferSize()
    {
        var size = PixelCount * BytesPerPixel;

        return size;
    }

    private byte GetBytesPerPixel(ColorFormat colorFormat)
    {
        //if (colorFormat == ColorFormat.Bayer)
        //{
        //    return 1;
        //}

        //if (colorFormat == ColorFormat.BGR)
        //{
        //    return 3;
        //}

        if (colorFormat == ColorFormat.RGB)
        {
            return 3;
        }

        //if (colorFormat == ColorFormat.BGRA)
        //{
        //    return 4;
        //}

        //if (colorFormat == ColorFormat.RGBA)
        //{
        //    return 4;
        //}

        //if (colorFormat == ColorFormat.Gray)
        //{
        //    return 1;
        //}

        throw new NotImplementedException();
    }
}

public class InternalFrameConfiguration
{
    public VideoResolution VideoResolution { get; init; }
    public VideoSize VideoSize { get; init; }
    public IReadOnlyList<NormalizedFrameConfig> NormalizedFrameConfig { get; init; } = null!;
    public IReadOnlyList<Command> SensorStart { get; init; } = null!;
    public IReadOnlyList<Command> BridgeStart { get; init; } = null!;

    // _normalize_framerate()
    public NormalizedFrameConfig GetNormalizedFrameConfig(int framesPerSecond)
    {
        var candidates = NormalizedFrameConfig;

        NormalizedFrameConfig config = candidates[0];

        for (int i = 1; i < candidates.Count; i++)
        {
            var current = candidates[i];

            if (current.fps < framesPerSecond)
            {
                break;
            }

            config = current;
        }

        return config;
    }
}

public readonly struct Command
{
    public readonly byte Register;
    public readonly byte Value;

    public Command(byte Register, byte Value)
    {
        this.Register = Register;
        this.Value = Value;
    }
}

public struct NormalizedFrameConfig
{
    private const RegisterOV534 Field1Address = (RegisterOV534)0x11;
    private const RegisterOV534 Field2Address = (RegisterOV534)0x0d;

    public int fps;

    // Maybe color configs?
    public byte Field1;
    public byte Field2;
    public byte Field3;

    public void WriteTo(Ps3CamDriver ps3CamDriver)
    {
        //sccb_reg_write(0x11, rate.r11);
        ps3CamDriver.SerialCameraControlBusRegisterWrite(Field1Address, Field1);
        //sccb_reg_write(0x0d, rate.r0d);
        ps3CamDriver.SerialCameraControlBusRegisterWrite(Field2Address, Field2);
        //ov534_reg_write(0xe5, rate.re5);
        ps3CamDriver.HardwareRegisterWrite(OperationsOV534.Unknown0xe5, Field3);
    }
};

public struct VideoSize
{
    public int Width;
    public int Height;

    public int GetPixelCount()
    {
        var res = Width * Height;

        return res;
    }
};
