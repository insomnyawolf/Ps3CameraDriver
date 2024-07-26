namespace Ps3CameraDriver.Protocol;

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


