namespace Ps3CameraDriver;

public enum RegisterOV534 : byte
{
    Gain = 0x00,
    GainBlueChannel = 0x01,
    GainRedChannel = 0x02,
    GainGreenChannel = 0x03,
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
    Saturation1 = 0xa7,
    Saturation2 = 0xa8,
    Contrast = 0x9C,
    Brightness = 0x9B,
};