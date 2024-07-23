namespace Ps3CameraDriver;

public enum CheckStatusResponses : byte
{
    Ok = 0x00,
    Retry = 0x03,
    Error = 0x04,
};

public enum RegisterOV534 : byte
{
    BlueChannelGain = 0x01,
    RedChannelGain = 0x02,
    GreenChannelGain = 0x03,
    BlcBlueChannelTarget = 0x42,
    BlcRedChannelTarget = 0x43,
    BlcGrenChannelTarget = 0x44,
    /// <summary>
    /// Uncertain
    /// </summary>
    Settings = 0x13,
    AutoWhiteBalance = 0x63,
    AutoGain = 0x64,
    UnknownFrameBufferRelated = 0x0C,
    Exposure1 = 0x08,
    Exposure2 = 0x10,
    Sharpness1 = 0x91,
    Sharpness2 = 0x8E,
    Contrast = 0x9C,
    Brightness = 0x9B,
};

public enum OperationsOV534 : byte
{
    /// <summary>
    /// SensorAddress
    /// </summary>
    REG_ADDRESS = 0xf1,
    REG_SUBADDR = 0xf2,
    REG_WRITE = 0xf3,
    OP_WRITE_2 = 0x33,
    OP_WRITE_3 = 0x37,
    REG_READ = 0xf4,
    OP_READ_2 = 0xf9,
    REG_OPERATION = 0xf5,
    REG_STATUS = 0xf6,
    /// <summary>
    /// Uncertain
    /// </summary>
    Led1 = 0x21,
    /// <summary>
    /// Uncertain
    /// </summary>
    Led2 = 0x23,
    /// <summary>
    /// Uncertain
    /// </summary>
    Bridge1 = 0xe7,
    /// <summary>
    /// Uncertain
    /// </summary>
    Bridge2 = 0xe0,
    /// <summary>
    /// Unknown
    /// </summary>
    Unknown0xe5 = 0xe5,
};

public enum VideoResolution : byte
{
    /// <summary>
    /// 320x240
    /// </summary>
    QVGA,
    /// <summary>
    /// 640x480
    /// </summary>
    VGA,
}

public enum ColorFormat : int
{
    /// <summary>
    /// Output in Bayer. Destination buffer must be width * height bytes
    /// </summary>
    Bayer,
    /// <summary>
    /// Output in BGR. Destination buffer must be width * height * 3 bytes
    /// </summary>
    BGR,
    /// <summary>
    /// Output in RGB. Destination buffer must be width * height * 3 bytes
    /// </summary>
    RGB,
    /// <summary>
    /// Output in BGRA. Destination buffer must be width * height * 4 bytes
    /// </summary>
    BGRA,
    /// <summary>
    /// Output in RGBA. Destination buffer must be width * height * 4 bytes
    /// </summary>
    RGBA,
    /// <summary>
    /// Output in Grayscale. Destination buffer must be width * height bytes
    /// </summary>
    Gray,
};
